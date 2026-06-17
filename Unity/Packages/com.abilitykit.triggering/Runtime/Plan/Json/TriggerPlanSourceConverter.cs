using System;
using System.Collections.Generic;
using System.IO;
using AbilityKit.Triggering.Eventing;
using AbilityKit.Triggering.Runtime.Config.Plans;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AbilityKit.Triggering.Runtime.Plan.Json
{
    /// <summary>
    /// 触发器计划源格式转换器
    /// 将人类可读的源格式 JSON 转换为运行时格式 JSON
    /// </summary>
    public sealed class TriggerPlanSourceConverter
    {
        /// <summary>
        /// 将源格式 JSON 字符串转换为运行时格式 JSON 字符串
        /// </summary>
        public string ConvertSourceToRuntimeJson(string sourceJson)
        {
            if (string.IsNullOrEmpty(sourceJson))
            {
                return "{\"Triggers\":[],\"Strings\":{}}";
            }

            var source = ParseSource(sourceJson);
            return ConvertSource(source);
        }

        private static TriggerPlanSourceJson ParseSource(string sourceJson)
        {
            var root = JObject.Parse(sourceJson);
            if (root["triggers"] != null || root["actions"] is JObject || root["conditions"] is JObject || root["version"] != null || root["metadata"] != null)
            {
                var source = root.ToObject<TriggerPlanSourceJson>();
                if (source?.triggers != null && source.triggers.Count > 0)
                {
                    return source;
                }
            }

            var singleTrigger = root.ToObject<TriggerSourceTriggerJson>();
            return new TriggerPlanSourceJson
            {
                triggers = singleTrigger != null && singleTrigger.id > 0
                    ? new List<TriggerSourceTriggerJson> { singleTrigger }
                    : new List<TriggerSourceTriggerJson>()
            };
        }

        private string ConvertSource(TriggerPlanSourceJson source)
        {
            var behaviorCatalog = BuildBehaviorCatalog(source?.behaviors);
            var conditionCatalog = BuildFragmentCatalog(source?.conditionGroups, source?.condition_groups);
            var actionCatalog = BuildFragmentCatalog(source?.actionGroups, source?.action_groups);
            using (var sw = new StringWriter())
            {
                using (var writer = new JsonTextWriter(sw))
                {
                    writer.Formatting = Formatting.Indented;
                    writer.WriteStartObject();
                    writer.WritePropertyName("Triggers");
                    writer.WriteStartArray();

                    if (source?.triggers != null)
                    {
                        foreach (var trigger in source.triggers)
                        {
                            WriteTrigger(writer, trigger, source.actions, conditionCatalog, actionCatalog, behaviorCatalog);
                        }
                    }

                    writer.WriteEndArray();
                    writer.WritePropertyName("Strings");
                    writer.WriteStartObject();
                    writer.WriteEndObject();
                    writer.WriteEndObject();
                }
                return sw.ToString();
            }
        }

        private void WriteTrigger(
            JsonTextWriter writer,
            TriggerSourceTriggerJson trigger,
            Dictionary<string, ActionSourceDefJson> actionSchemas,
            Dictionary<string, JToken> conditionCatalog,
            Dictionary<string, JToken> actionCatalog,
            Dictionary<string, JObject> behaviorCatalog)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("TriggerId");
            writer.WriteValue(trigger.id);

            if (!string.IsNullOrEmpty(trigger.@event))
            {
                writer.WritePropertyName("EventName");
                writer.WriteValue(trigger.@event);
                writer.WritePropertyName("EventId");
                writer.WriteValue(StableStringId.Get("event:" + trigger.@event));
            }
            else
            {
                writer.WritePropertyName("EventId");
                writer.WriteValue(0);
            }

            writer.WritePropertyName("AllowExternal");
            writer.WriteValue(trigger.allowExternal);

            writer.WritePropertyName("Phase");
            writer.WriteValue(ParsePhase(trigger.phase));

            writer.WritePropertyName("Priority");
            writer.WriteValue(trigger.priority);

            writer.WritePropertyName("Scope");
            writer.WriteValue((int)ParseScope(trigger.scope));

            writer.WritePropertyName("Predicate");
            WritePredicate(writer, ResolveConditionList(trigger.conditions, trigger.conditionRefs, trigger.condition_refs, conditionCatalog, new HashSet<string>(StringComparer.OrdinalIgnoreCase)));

            WriteExecutionControl(writer, trigger);

            writer.WritePropertyName("Actions");
            writer.WriteStartArray();
            var actions = ResolveActionList(trigger.actions, trigger.actionRefs, trigger.action_refs, actionCatalog, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            if (actions != null)
            {
                foreach (var action in actions)
                {
                    WriteAction(writer, action, actionSchemas, actionCatalog);
                }
            }
            writer.WriteEndArray();

            var root = GetExecutionRootSource(trigger);
            if (root != null)
            {
                writer.WritePropertyName("ExecutionRoot");
                WriteExecutionNode(writer, root, actionSchemas, conditionCatalog, actionCatalog, behaviorCatalog, $"trigger:{trigger.id}", "inline", trigger.id.ToString(), new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            }
 
            writer.WriteEndObject();
        }

        private static void WriteExecutionControl(JsonTextWriter writer, TriggerSourceTriggerJson trigger)
        {
            var executionToken = trigger.execution ?? trigger.executionControl ?? trigger.execution_control;
            var mode = trigger.once ? "once" : trigger.repeat ? "repeat" : null;
            var maxExecutions = trigger.max_executions > 0 ? trigger.max_executions : trigger.maxExecutions;
            var cooldownMs = trigger.cooldown_ms > 0f ? trigger.cooldown_ms : trigger.cooldownMs;

            if (executionToken != null && executionToken.Type != JTokenType.Null)
            {
                if (executionToken.Type == JTokenType.String)
                {
                    mode = executionToken.Value<string>();
                }
                else if (executionToken is JObject obj)
                {
                    mode = ReadString(obj, "mode", "type") ?? mode;
                    maxExecutions = ReadInt(obj, maxExecutions, "max_executions", "maxExecutions", "count", "times");
                    cooldownMs = ReadFloat(obj, cooldownMs, "cooldown_ms", "cooldownMs", "cooldown", "interval_ms", "intervalMs");
                }
            }

            if (string.IsNullOrEmpty(mode) && cooldownMs > 0f)
            {
                mode = "cooldown";
            }

            if (string.IsNullOrEmpty(mode))
            {
                return;
            }

            writer.WritePropertyName("ExecutionControl");
            writer.WriteStartObject();
            writer.WritePropertyName("Mode");
            writer.WriteValue(mode);
            if (maxExecutions > 0)
            {
                writer.WritePropertyName("MaxExecutions");
                writer.WriteValue(maxExecutions);
            }
            if (cooldownMs > 0f)
            {
                writer.WritePropertyName("CooldownMs");
                writer.WriteValue(cooldownMs);
            }
            writer.WriteEndObject();
        }

        private static string ReadString(JObject obj, params string[] aliases)
        {
            if (obj == null || aliases == null) return null;
            for (int i = 0; i < aliases.Length; i++)
            {
                if (obj.TryGetValue(aliases[i], StringComparison.OrdinalIgnoreCase, out var token))
                {
                    return token?.ToString();
                }
            }
            return null;
        }

        private static int ReadInt(JObject obj, int defaultValue, params string[] aliases)
        {
            if (obj == null || aliases == null) return defaultValue;
            for (int i = 0; i < aliases.Length; i++)
            {
                if (obj.TryGetValue(aliases[i], StringComparison.OrdinalIgnoreCase, out var token))
                {
                    return token.Value<int?>() ?? defaultValue;
                }
            }
            return defaultValue;
        }

        private static float ReadFloat(JObject obj, float defaultValue, params string[] aliases)
        {
            if (obj == null || aliases == null) return defaultValue;
            for (int i = 0; i < aliases.Length; i++)
            {
                if (obj.TryGetValue(aliases[i], StringComparison.OrdinalIgnoreCase, out var token))
                {
                    return token.Value<float?>() ?? defaultValue;
                }
            }
            return defaultValue;
        }

        private static JObject GetExecutionRootSource(TriggerSourceTriggerJson trigger)
        {
            if (trigger == null) return null;
            if (trigger.behavior != null) return trigger.behavior;

            if (trigger.executables == null || trigger.executables.Count == 0)
                return null;

            return new JObject
            {
                ["type"] = "sequence",
                ["children"] = new JArray(trigger.executables)
            };
        }

        private static int ParsePhase(string phase)
        {
            if (string.IsNullOrEmpty(phase)) return 0;

            return phase.Trim().ToLowerInvariant() switch
            {
                "immediate" => 0,
                "delayed" => 1,
                "precondition" => 2,
                "postcondition" => 3,
                _ => throw new InvalidOperationException($"Unsupported trigger phase: {phase}")
            };
        }

        private static TriggerPlanScope ParseScope(string scope)
        {
            if (string.IsNullOrEmpty(scope)) return TriggerPlanScope.Global;
            switch (scope.Trim().ToLowerInvariant())
            {
                case "owner":
                case "ownerbound":
                case "owner_bound":
                case "owner-bound":
                    return TriggerPlanScope.OwnerBound;
                case "global":
                    return TriggerPlanScope.Global;
                default:
                    throw new InvalidOperationException($"Unsupported trigger scope: {scope}");
            }
        }

        private void WritePredicate(JsonTextWriter writer, List<JObject> conditions)
        {
            writer.WriteStartObject();

            if (conditions == null || conditions.Count == 0)
            {
                writer.WritePropertyName("Kind");
                writer.WriteValue("none");
                writer.WritePropertyName("Nodes");
                writer.WriteNull();
            }
            else
            {
                writer.WritePropertyName("Kind");
                writer.WriteValue("expr");
                writer.WritePropertyName("Nodes");
                writer.WriteStartArray();

                WriteConditionList(writer, conditions, "And");

                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }

        private void WriteConditionList(JsonTextWriter writer, IReadOnlyList<JObject> conditions, string logicalOp)
        {
            if (conditions == null || conditions.Count == 0)
            {
                throw new InvalidOperationException($"Condition group '{logicalOp}' must contain at least one condition.");
            }

            for (var i = 0; i < conditions.Count; i++)
            {
                WriteCondition(writer, conditions[i]);
                if (i > 0)
                {
                    WriteBoolOperator(writer, logicalOp);
                }
            }
        }

        private void WriteCondition(JsonTextWriter writer, JObject cond)
        {
            if (cond == null)
            {
                throw new InvalidOperationException("Condition item cannot be null.");
            }

            var type = cond["type"]?.ToString();
            if (string.IsNullOrEmpty(type))
            {
                throw new InvalidOperationException("Condition type is required.");
            }

            switch (type)
            {
                case "all":
                case "and":
                    WriteConditionList(writer, ReadConditionItems(cond), "And");
                    break;

                case "any":
                case "or":
                    WriteConditionList(writer, ReadConditionItems(cond), "Or");
                    break;

                case "not":
                    WriteConditionList(writer, ReadConditionItems(cond), "And");
                    WriteBoolOperator(writer, "Not");
                    break;

                case "compare":
                case "compare_numeric":
                    WriteCompareNode(writer, cond, cond["op"]?.ToString() ?? cond["compare_op"]?.ToString() ?? cond["compareOp"]?.ToString());
                    break;

                case "arg_eq":
                    WriteCompareNode(writer, cond, "Equal");
                    break;

                case "arg_gt":
                    WriteCompareNode(writer, cond, "GreaterThan");
                    break;

                case "arg_gte":
                    WriteCompareNode(writer, cond, "GreaterThanOrEqual");
                    break;

                case "arg_lt":
                    WriteCompareNode(writer, cond, "LessThan");
                    break;

                case "arg_lte":
                    WriteCompareNode(writer, cond, "LessThanOrEqual");
                    break;

                case "arg_neq":
                    WriteCompareNode(writer, cond, "NotEqual");
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported condition type: {type}");
            }
        }

        private static List<JObject> ReadConditionItems(JObject cond)
        {
            var items = new List<JObject>();
            var inner = cond["items"] ?? cond["item"] ?? cond["conditions"] ?? cond["condition"];
            if (inner is JArray arr)
            {
                foreach (var item in arr)
                {
                    if (item is JObject obj)
                    {
                        items.Add(obj);
                    }
                }
            }
            else if (inner is JObject obj)
            {
                items.Add(obj);
            }

            return items;
        }

        private static void WriteBoolConst(JsonTextWriter writer, bool value)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("Kind");
            writer.WriteValue("Const");
            writer.WritePropertyName("ConstValue");
            writer.WriteValue(value);
            writer.WriteEndObject();
        }

        private static void WriteBoolOperator(JsonTextWriter writer, string kind)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("Kind");
            writer.WriteValue(kind);
            writer.WriteEndObject();
        }

        private void WriteCompareNode(JsonTextWriter writer, JObject cond, string op)
        {
            var argName = cond["arg_name"]?.ToString();
            var leftVarDomain = (cond["left_var_domain"] ?? cond["var_domain"])?.ToString();
            var leftVarKey = (cond["left_var_key"] ?? cond["var_key"])?.ToString();

            writer.WriteStartObject();
            writer.WritePropertyName("Kind");
            writer.WriteValue("CompareNumeric");
            writer.WritePropertyName("CompareOp");
            writer.WriteValue(NormalizeCompareOp(op));
            writer.WritePropertyName("Left");
            if (cond["left"] != null)
            {
                WriteParamValue(writer, cond["left"]);
            }
            else if (!string.IsNullOrEmpty(leftVarDomain) && !string.IsNullOrEmpty(leftVarKey))
            {
                WriteVarValue(writer, leftVarDomain, leftVarKey);
            }
            else
            {
                WritePayloadFieldValue(writer, argName);
            }

            writer.WritePropertyName("Right");
            WriteParamValue(writer, cond["right"] ?? cond["value"]);
            writer.WriteEndObject();
        }

        private void WriteExecutionNode(
            JsonTextWriter writer,
            JObject node,
            Dictionary<string, ActionSourceDefJson> actionSchemas,
            Dictionary<string, JToken> conditionCatalog,
            Dictionary<string, JToken> actionCatalog,
            Dictionary<string, JObject> behaviorCatalog,
            string sourcePath,
            string sourceKind,
            string sourceId,
            HashSet<string> behaviorResolving)
        {
            var resolvedBehaviorId = default(string);
            node = ResolveBehaviorReference(node, behaviorCatalog, behaviorResolving, ref sourceKind, ref sourceId, ref sourcePath, out resolvedBehaviorId);

            try
            {
                writer.WriteStartObject();

                if (node == null)
                {
                    writer.WritePropertyName("Kind");
                    writer.WriteValue("Sequence");
                    writer.WritePropertyName("Children");
                    writer.WriteStartArray();
                    writer.WriteEndArray();
                    writer.WriteEndObject();
                    return;
                }

                var type = (node["kind"] ?? node["type"])?.ToString();
                var normalizedKind = NormalizeExecutionKind(type, node);

                writer.WritePropertyName("Kind");
                writer.WriteValue(normalizedKind);
                WriteNodeSource(writer, sourceKind, sourceId, sourcePath);

                if (TryGetNodeCondition(node, conditionCatalog, out var conditionNodes))
                {
                    writer.WritePropertyName("Condition");
                    WritePredicate(writer, conditionNodes);
                }

                var weight = node["weight"]?.Value<float?>();
                if (weight.HasValue)
                {
                    writer.WritePropertyName("Weight");
                    writer.WriteValue(weight.Value);
                }

                WriteExecutionNodeOptions(writer, node, normalizedKind, conditionCatalog);

                if (string.Equals(normalizedKind, "Action", StringComparison.OrdinalIgnoreCase))
                {
                    var action = node["action"] as JObject ?? node;
                    writer.WritePropertyName("Action");
                    WriteAction(writer, ResolveAction(action, actionCatalog, new HashSet<string>(StringComparer.OrdinalIgnoreCase)) ?? action, actionSchemas, actionCatalog);
                    writer.WriteEndObject();
                    return;
                }

                var children = GetNodeChildren(node, behaviorCatalog);
                writer.WritePropertyName("Children");
                writer.WriteStartArray();
                if (children != null)
                {
                    var index = 0;
                    foreach (var child in children)
                    {
                        if (child is JObject childObj)
                        {
                            WriteExecutionNode(writer, childObj, actionSchemas, conditionCatalog, actionCatalog, behaviorCatalog, $"{sourcePath}/children[{index}]", sourceKind, sourceId, behaviorResolving);
                        }
                        index++;
                    }
                }
                writer.WriteEndArray();

                if (string.Equals(normalizedKind, "If", StringComparison.OrdinalIgnoreCase))
                {
                    var elseChildren = GetNodeElseChildren(node);
                    if (elseChildren != null)
                    {
                        writer.WritePropertyName("ElseChildren");
                        writer.WriteStartArray();
                        var index = 0;
                        foreach (var child in elseChildren)
                        {
                            if (child is JObject childObj)
                            {
                                WriteExecutionNode(writer, childObj, actionSchemas, conditionCatalog, actionCatalog, behaviorCatalog, $"{sourcePath}/else[{index}]", sourceKind, sourceId, behaviorResolving);
                            }
                            index++;
                        }
                        writer.WriteEndArray();
                    }
                }

                writer.WriteEndObject();
            }
            finally
            {
                EndResolveBehaviorReference(resolvedBehaviorId, behaviorResolving);
            }
        }

        private static string NormalizeExecutionKind(string type, JObject node)
        {
            if (node != null && node["action"] is JObject)
                return "Action";

            if (string.IsNullOrEmpty(type))
                return HasCompositeChildren(node) ? "Sequence" : "Action";

            switch (type.Trim().ToLowerInvariant())
            {
                case "sequence":
                case "seq":
                    return "Sequence";
                case "selector":
                case "select":
                    return "Selector";
                case "random":
                case "random_selector":
                case "randomselector":
                    return "Random";
                case "if":
                case "ifelse":
                case "if_else":
                    return "If";
                case "parallel":
                case "all":
                    return "Parallel";
                case "repeat":
                case "loop":
                    return "Repeat";
                case "until":
                case "repeat_until":
                case "repeatuntil":
                    return "Until";
                case "invert":
                case "not":
                    return "Invert";
                case "succeed":
                case "success":
                case "always_success":
                case "alwayssuccess":
                    return "Succeed";
                case "fail":
                case "failure":
                case "always_fail":
                case "alwaysfail":
                    return "Fail";
                case "action":
                    return "Action";
                default:
                    throw new InvalidOperationException($"Unsupported execution node kind: {type}");
            }
        }

        private static bool HasCompositeChildren(JObject node)
        {
            return node != null && (node["children"] is JArray || node["items"] is JArray || node["then"] is JArray || node["child"] is JObject || HasBehaviorReferenceList(node));
        }

        private static JArray GetNodeChildren(JObject node, Dictionary<string, JObject> behaviorCatalog)
        {
            if (node == null) return null;

            var merged = new JArray();
            AddBehaviorReferenceChildren(merged, ReadStringList(node, "behaviorRefs"), behaviorCatalog);
            AddBehaviorReferenceChildren(merged, ReadStringList(node, "behavior_refs"), behaviorCatalog);

            var inline = GetInlineNodeChildren(node);
            if (inline != null)
            {
                foreach (var child in inline)
                {
                    merged.Add(child);
                }
            }

            return merged.Count > 0 ? merged : null;
        }

        private static JArray GetInlineNodeChildren(JObject node)
        {
            if (node == null) return null;
            if (node["children"] is JArray children) return children;
            if (node["items"] is JArray items) return items;
            if (node["then"] is JArray thenChildren) return thenChildren;
            if (node["then"] is JObject thenOne) return new JArray(thenOne);
            if (node["child"] is JObject childOne) return new JArray(childOne);
            return null;
        }

        private static bool HasBehaviorReferenceList(JObject node)
        {
            return node != null && (node["behaviorRefs"] is JArray || node["behavior_refs"] is JArray || node["behaviorRefs"] is JValue || node["behavior_refs"] is JValue);
        }

        private static void AddBehaviorReferenceChildren(JArray children, List<string> refs, Dictionary<string, JObject> behaviorCatalog)
        {
            if (children == null || refs == null) return;
            for (int i = 0; i < refs.Count; i++)
            {
                var refId = refs[i];
                if (string.IsNullOrEmpty(refId)) continue;
                if (behaviorCatalog == null || !behaviorCatalog.ContainsKey(refId))
                {
                    throw new InvalidOperationException($"Behavior reference not found: {refId}");
                }

                children.Add(new JObject { ["behaviorRef"] = refId });
            }
        }

        private static JArray GetNodeElseChildren(JObject node)
        {
            if (node == null) return null;
            if (node["elseChildren"] is JArray elseChildren) return elseChildren;
            if (node["else"] is JArray elseItems) return elseItems;
            if (node["else"] is JObject elseOne) return new JArray(elseOne);
            return null;
        }

        private void WriteExecutionNodeOptions(JsonTextWriter writer, JObject node, string normalizedKind, Dictionary<string, JToken> conditionCatalog)
        {
            if (string.Equals(normalizedKind, "Repeat", StringComparison.OrdinalIgnoreCase))
            {
                writer.WritePropertyName("Count");
                writer.WriteValue(ReadPositiveInt(node, "count", "times", "repeatCount", "repeat_count"));
            }

            if (string.Equals(normalizedKind, "Until", StringComparison.OrdinalIgnoreCase))
            {
                writer.WritePropertyName("MaxIterations");
                writer.WriteValue(ReadPositiveInt(node, "maxIterations", "max_iterations", "limit", "count"));

                if (TryGetUntilCondition(node, conditionCatalog, out var untilConditions))
                {
                    writer.WritePropertyName("UntilCondition");
                    WritePredicate(writer, untilConditions);
                }
            }

            if (string.Equals(normalizedKind, "Fail", StringComparison.OrdinalIgnoreCase))
            {
                var reason = node["reason"]?.ToString();
                if (!string.IsNullOrEmpty(reason))
                {
                    writer.WritePropertyName("Reason");
                    writer.WriteValue(reason);
                }
            }
        }

        private static int ReadPositiveInt(JObject node, params string[] aliases)
        {
            if (node == null || aliases == null) return 1;
            for (int i = 0; i < aliases.Length; i++)
            {
                if (node.TryGetValue(aliases[i], StringComparison.OrdinalIgnoreCase, out var token))
                {
                    var value = token.Value<int?>() ?? 1;
                    return value > 0 ? value : 1;
                }
            }

            return 1;
        }

        private static bool TryGetUntilCondition(JObject node, Dictionary<string, JToken> conditionCatalog, out List<JObject> conditions)
        {
            conditions = null;
            if (node == null) return false;

            var token = node["until"] ?? node["untilCondition"] ?? node["until_condition"];
            if (!TryBuildConditionList(token, out conditions))
            {
                return false;
            }

            conditions = ResolveConditionList(conditions, null, null, conditionCatalog, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            return conditions.Count > 0;
        }

        private static bool TryGetNodeCondition(JObject node, Dictionary<string, JToken> conditionCatalog, out List<JObject> conditions)
        {
            conditions = null;
            if (node == null) return false;

            var token = node["condition"] ?? node["conditions"] ?? node["when"];
            if (!TryBuildConditionList(token, out conditions))
            {
                return false;
            }

            conditions = ResolveConditionList(conditions, null, null, conditionCatalog, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            return conditions.Count > 0;
        }

        private static bool TryBuildConditionList(JToken token, out List<JObject> conditions)
        {
            conditions = null;
            if (token == null || token.Type == JTokenType.Null)
                return false;

            conditions = new List<JObject>();
            if (token is JArray arr)
            {
                foreach (var item in arr)
                {
                    if (item is JObject obj)
                    {
                        conditions.Add(obj);
                    }
                }
            }
            else if (token is JObject obj)
            {
                conditions.Add(obj);
            }

            return conditions.Count > 0;
        }

        private static void WriteNodeSource(JsonTextWriter writer, string sourceKind, string sourceId, string sourcePath)
        {
            if (!string.IsNullOrEmpty(sourceKind))
            {
                writer.WritePropertyName("SourceKind");
                writer.WriteValue(sourceKind);
            }

            if (!string.IsNullOrEmpty(sourceId))
            {
                writer.WritePropertyName("SourceId");
                writer.WriteValue(sourceId);
            }

            if (!string.IsNullOrEmpty(sourcePath))
            {
                writer.WritePropertyName("SourcePath");
                writer.WriteValue(sourcePath);
            }
        }

        private static Dictionary<string, JObject> BuildBehaviorCatalog(Dictionary<string, BehaviorSourceDefJson> behaviors)
        {
            var catalog = new Dictionary<string, JObject>(StringComparer.OrdinalIgnoreCase);
            if (behaviors == null) return catalog;

            foreach (var kvp in behaviors)
            {
                var id = kvp.Key;
                var behavior = kvp.Value;
                var root = behavior?.behavior ?? behavior?.root;
                if (root == null) continue;

                catalog[id] = root;
                if (!string.IsNullOrEmpty(behavior.id))
                {
                    catalog[behavior.id] = root;
                }
            }

            return catalog;
        }

        private static Dictionary<string, JToken> BuildFragmentCatalog(params Dictionary<string, JToken>[] sources)
        {
            var catalog = new Dictionary<string, JToken>(StringComparer.OrdinalIgnoreCase);
            if (sources == null) return catalog;

            for (int i = 0; i < sources.Length; i++)
            {
                var source = sources[i];
                if (source == null) continue;

                foreach (var kvp in source)
                {
                    if (string.IsNullOrEmpty(kvp.Key) || kvp.Value == null) continue;
                    catalog[kvp.Key] = kvp.Value;
                }
            }

            return catalog;
        }

        private static List<JObject> ResolveConditionList(
            List<JObject> inlineConditions,
            List<string> conditionRefs,
            List<string> condition_refs,
            Dictionary<string, JToken> catalog,
            HashSet<string> resolving)
        {
            var result = new List<JObject>();
            AddReferencedConditions(result, conditionRefs, catalog, resolving);
            AddReferencedConditions(result, condition_refs, catalog, resolving);

            if (inlineConditions != null)
            {
                for (int i = 0; i < inlineConditions.Count; i++)
                {
                    AddCondition(result, inlineConditions[i], catalog, resolving);
                }
            }

            return result;
        }

        private static void AddReferencedConditions(List<JObject> result, List<string> refs, Dictionary<string, JToken> catalog, HashSet<string> resolving)
        {
            if (refs == null) return;
            for (int i = 0; i < refs.Count; i++)
            {
                AddConditionRef(result, refs[i], catalog, resolving);
            }
        }

        private static void AddCondition(List<JObject> result, JObject condition, Dictionary<string, JToken> catalog, HashSet<string> resolving)
        {
            if (condition == null) return;

            var refId = GetFragmentReferenceId(condition, "conditionRef", "conditionId", "conditionGroup", "conditionGroupId");
            if (!string.IsNullOrEmpty(refId))
            {
                AddConditionRef(result, refId, catalog, resolving);
                return;
            }

            result.Add(NormalizeConditionNode(condition, catalog, resolving));
        }

        private static JObject NormalizeConditionNode(JObject condition, Dictionary<string, JToken> catalog, HashSet<string> resolving)
        {
            var normalized = (JObject)condition.DeepClone();
            var type = normalized["type"]?.ToString();
            if (!IsKind(type, "all", "and", "any", "or", "not"))
            {
                return normalized;
            }

            var inlineItems = ReadConditionItems(normalized);
            var conditionRefs = ReadStringList(normalized, "conditionRefs");
            var conditionRefsSnake = ReadStringList(normalized, "condition_refs");
            var mergedItems = ResolveConditionList(inlineItems, conditionRefs, conditionRefsSnake, catalog, resolving);

            RemoveProperties(normalized, "conditionRefs", "condition_refs", "conditions", "condition", "item");
            normalized["items"] = new JArray(mergedItems);
            return normalized;
        }

        private static void AddConditionRef(List<JObject> result, string refId, Dictionary<string, JToken> catalog, HashSet<string> resolving)
        {
            var fragment = BeginResolveFragment(refId, catalog, resolving, "condition group");
            if (fragment == null) return;

            try
            {
                if (fragment is JArray arr)
                {
                    foreach (var item in arr)
                    {
                        if (item is JObject obj)
                        {
                            AddCondition(result, obj, catalog, resolving);
                        }
                    }
                    return;
                }

                if (fragment is JObject single)
                {
                    if (TryReadObjectList(single, out var items, "conditions", "items"))
                    {
                        for (int i = 0; i < items.Count; i++)
                        {
                            AddCondition(result, items[i], catalog, resolving);
                        }
                        return;
                    }

                    AddCondition(result, single, catalog, resolving);
                }
            }
            finally
            {
                resolving.Remove(refId);
            }
        }

        private static List<JObject> ResolveActionList(
            List<JObject> inlineActions,
            List<string> actionRefs,
            List<string> action_refs,
            Dictionary<string, JToken> catalog,
            HashSet<string> resolving)
        {
            var result = new List<JObject>();
            AddReferencedActions(result, actionRefs, catalog, resolving);
            AddReferencedActions(result, action_refs, catalog, resolving);

            if (inlineActions != null)
            {
                for (int i = 0; i < inlineActions.Count; i++)
                {
                    AddAction(result, inlineActions[i], catalog, resolving);
                }
            }

            return result;
        }

        private static void AddReferencedActions(List<JObject> result, List<string> refs, Dictionary<string, JToken> catalog, HashSet<string> resolving)
        {
            if (refs == null) return;
            for (int i = 0; i < refs.Count; i++)
            {
                AddActionRef(result, refs[i], catalog, resolving);
            }
        }

        private static void AddAction(List<JObject> result, JObject action, Dictionary<string, JToken> catalog, HashSet<string> resolving)
        {
            if (action == null) return;

            var resolved = ResolveAction(action, catalog, resolving);
            if (resolved == null) return;

            if (IsCompositeAction(resolved))
            {
                var inlineItems = ReadActionItems(resolved);
                var actionRefs = ReadStringList(resolved, "actionRefs");
                var actionRefsSnake = ReadStringList(resolved, "action_refs");
                result.AddRange(ResolveActionList(inlineItems, actionRefs, actionRefsSnake, catalog, resolving));
                return;
            }

            result.Add(resolved);
        }

        private static JObject ResolveAction(JObject action, Dictionary<string, JToken> catalog, HashSet<string> resolving)
        {
            if (action == null) return null;

            var refId = GetFragmentReferenceId(action, "actionRef", "actionId", "actionGroup", "actionGroupId");
            if (string.IsNullOrEmpty(refId))
            {
                return NormalizeActionNode(action, catalog, resolving);
            }

            var fragment = BeginResolveFragment(refId, catalog, resolving, "action group");
            try
            {
                if (fragment is JObject obj)
                {
                    if (TryReadObjectList(obj, out var items, "actions", "items"))
                    {
                        if (items.Count != 1)
                        {
                            throw new InvalidOperationException($"Action reference must resolve to exactly one action in this context: {refId}");
                        }

                        return ResolveAction(items[0], catalog, resolving);
                    }

                    return (JObject)obj.DeepClone();
                }

                if (fragment is JArray arr && arr.Count == 1 && arr[0] is JObject single)
                {
                    return ResolveAction(single, catalog, resolving);
                }
            }
            finally
            {
                resolving.Remove(refId);
            }

            throw new InvalidOperationException($"Action reference must resolve to one action: {refId}");
        }

        private static void AddActionRef(List<JObject> result, string refId, Dictionary<string, JToken> catalog, HashSet<string> resolving)
        {
            var fragment = BeginResolveFragment(refId, catalog, resolving, "action group");
            if (fragment == null) return;

            try
            {
                if (fragment is JArray arr)
                {
                    foreach (var item in arr)
                    {
                        if (item is JObject obj)
                        {
                            AddAction(result, obj, catalog, resolving);
                        }
                    }
                    return;
                }

                if (fragment is JObject single)
                {
                    if (TryReadObjectList(single, out var items, "actions", "items"))
                    {
                        for (int i = 0; i < items.Count; i++)
                        {
                            AddAction(result, items[i], catalog, resolving);
                        }
                        return;
                    }

                    AddAction(result, single, catalog, resolving);
                }
            }
            finally
            {
                resolving.Remove(refId);
            }
        }

        private static JObject NormalizeActionNode(JObject action, Dictionary<string, JToken> catalog, HashSet<string> resolving)
        {
            var normalized = (JObject)action.DeepClone();
            if (!IsCompositeAction(normalized))
            {
                return normalized;
            }

            var inlineItems = ReadActionItems(normalized);
            var actionRefs = ReadStringList(normalized, "actionRefs");
            var actionRefsSnake = ReadStringList(normalized, "action_refs");
            var mergedItems = ResolveActionList(inlineItems, actionRefs, actionRefsSnake, catalog, resolving);

            RemoveProperties(normalized, "actionRefs", "action_refs", "actions");
            normalized["items"] = new JArray(mergedItems);
            return normalized;
        }

        private static bool IsCompositeAction(JObject action)
        {
            var type = action?["type"]?.ToString();
            return IsKind(type, "seq", "sequence");
        }

        private static List<JObject> ReadActionItems(JObject action)
        {
            var items = new List<JObject>();
            if (TryReadObjectList(action, out var fromList, "items", "actions"))
            {
                items.AddRange(fromList);
            }

            return items;
        }

        private static List<string> ReadStringList(JObject obj, params string[] aliases)
        {
            var result = new List<string>();
            if (obj == null || aliases == null) return result;

            for (int i = 0; i < aliases.Length; i++)
            {
                if (!obj.TryGetValue(aliases[i], StringComparison.OrdinalIgnoreCase, out var token)) continue;

                if (token is JArray arr)
                {
                    foreach (var item in arr)
                    {
                        var value = item?.ToString();
                        if (!string.IsNullOrEmpty(value))
                        {
                            result.Add(value);
                        }
                    }
                }
                else
                {
                    var value = token?.ToString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        result.Add(value);
                    }
                }
            }

            return result;
        }

        private static void RemoveProperties(JObject obj, params string[] names)
        {
            if (obj == null || names == null) return;
            for (int i = 0; i < names.Length; i++)
            {
                obj.Remove(names[i]);
            }
        }

        private static JToken BeginResolveFragment(string refId, Dictionary<string, JToken> catalog, HashSet<string> resolving, string kind)
        {
            if (string.IsNullOrEmpty(refId)) return null;
            if (catalog == null || !catalog.TryGetValue(refId, out var fragment) || fragment == null)
            {
                throw new InvalidOperationException($"{kind} reference not found: {refId}");
            }

            if (!resolving.Add(refId))
            {
                throw new InvalidOperationException($"Cyclic {kind} reference detected: {refId}");
            }

            return fragment.DeepClone();
        }

        private static string GetFragmentReferenceId(JObject obj, params string[] aliases)
        {
            if (obj == null || aliases == null) return null;
            for (int i = 0; i < aliases.Length; i++)
            {
                if (obj.TryGetValue(aliases[i], StringComparison.OrdinalIgnoreCase, out var token))
                {
                    return token?.ToString();
                }
            }

            return obj["ref"]?.ToString();
        }

        private static bool TryReadObjectList(JObject obj, out List<JObject> items, params string[] aliases)
        {
            items = null;
            if (obj == null || aliases == null) return false;

            for (int i = 0; i < aliases.Length; i++)
            {
                if (!obj.TryGetValue(aliases[i], StringComparison.OrdinalIgnoreCase, out var token)) continue;

                items = new List<JObject>();
                if (token is JArray arr)
                {
                    foreach (var item in arr)
                    {
                        if (item is JObject child)
                        {
                            items.Add(child);
                        }
                    }
                }
                else if (token is JObject child)
                {
                    items.Add(child);
                }

                return items.Count > 0;
            }

            return false;
        }

        private static JObject ResolveBehaviorReference(
            JObject node,
            Dictionary<string, JObject> behaviorCatalog,
            HashSet<string> behaviorResolving,
            ref string sourceKind,
            ref string sourceId,
            ref string sourcePath,
            out string resolvedBehaviorId)
        {
            resolvedBehaviorId = null;
            var behaviorId = GetBehaviorReferenceId(node);
            if (string.IsNullOrEmpty(behaviorId))
                return node;

            if (behaviorCatalog == null || !behaviorCatalog.TryGetValue(behaviorId, out var root) || root == null)
            {
                throw new InvalidOperationException($"Behavior reference not found: {behaviorId}");
            }

            if (behaviorResolving != null && !behaviorResolving.Add(behaviorId))
            {
                throw new InvalidOperationException($"Cyclic behavior reference detected: {behaviorId}");
            }

            resolvedBehaviorId = behaviorId;
            sourceKind = "behavior";
            sourceId = behaviorId;
            sourcePath = $"behavior:{behaviorId}";
            return (JObject)root.DeepClone();
        }

        private static void EndResolveBehaviorReference(string behaviorId, HashSet<string> behaviorResolving)
        {
            if (!string.IsNullOrEmpty(behaviorId) && behaviorResolving != null)
            {
                behaviorResolving.Remove(behaviorId);
            }
        }

        private static string GetBehaviorReferenceId(JObject node)
        {
            if (node == null) return null;
            return node["behaviorRef"]?.ToString()
                ?? node["behaviorId"]?.ToString()
                ?? node["behavior"]?.ToString()
                ?? node["ref"]?.ToString();
        }

        private void WriteAction(JsonTextWriter writer, JObject action, Dictionary<string, ActionSourceDefJson> actionSchemas, Dictionary<string, JToken> actionCatalog)
        {
            action = ResolveAction(action, actionCatalog, new HashSet<string>(StringComparer.OrdinalIgnoreCase)) ?? action;
            var type = action["type"]?.ToString();
            if (string.IsNullOrEmpty(type)) return;

            var actionId = StableStringId.Get("action:" + type);
            var orderedArgs = BuildOrderedActionArgs(action, type, actionSchemas);

            writer.WriteStartObject();
            writer.WritePropertyName("ActionId");
            writer.WriteValue(actionId);

            var positionalCount = Math.Min(orderedArgs.Count, 2);
            writer.WritePropertyName("Arity");
            writer.WriteValue(positionalCount);

            if (positionalCount > 0)
            {
                writer.WritePropertyName("Arg0");
                WriteParamValue(writer, orderedArgs[0].Value);
            }

            if (positionalCount > 1)
            {
                writer.WritePropertyName("Arg1");
                WriteParamValue(writer, orderedArgs[1].Value);
            }

            if (orderedArgs.Count > 0)
            {
                writer.WritePropertyName("Args");
                writer.WriteStartObject();
                for (int i = 0; i < orderedArgs.Count; i++)
                {
                    writer.WritePropertyName(orderedArgs[i].Name);
                    WriteParamValue(writer, orderedArgs[i].Value);
                }
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        private List<ActionArgSource> BuildOrderedActionArgs(JObject action, string type, Dictionary<string, ActionSourceDefJson> actionSchemas)
        {
            var result = new List<ActionArgSource>();
            var consumed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var schema = FindActionSchema(type, actionSchemas);

            if (schema?.@params != null)
            {
                foreach (var param in schema.@params)
                {
                    if (param == null || string.IsNullOrEmpty(param.name)) continue;

                    if (action.TryGetValue(param.name, StringComparison.OrdinalIgnoreCase, out var value))
                    {
                        result.Add(new ActionArgSource(param.name, value));
                        consumed.Add(param.name);
                        continue;
                    }

                    if (!param.required && param.defaultValue != null)
                    {
                        result.Add(new ActionArgSource(param.name, JToken.FromObject(param.defaultValue)));
                        consumed.Add(param.name);
                        continue;
                    }

                    if (param.required)
                    {
                        throw new InvalidOperationException($"Required action parameter is missing. action={type} param={param.name}");
                    }
                }
            }

            foreach (var prop in action.Properties())
            {
                if (string.Equals(prop.Name, "type", StringComparison.OrdinalIgnoreCase)) continue;
                if (IsExecutionNodeControlProperty(prop.Name)) continue;
                if (consumed.Contains(prop.Name)) continue;
 
                result.Add(new ActionArgSource(prop.Name, prop.Value));
            }

            return result;
        }

        private static bool IsExecutionNodeControlProperty(string name)
        {
            return string.Equals(name, "kind", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "action", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "children", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "behaviorRefs", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "behavior_refs", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "items", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "child", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "then", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "else", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "elseChildren", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "condition", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "conditions", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "when", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "until", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "untilCondition", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "until_condition", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "count", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "times", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "repeatCount", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "repeat_count", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "maxIterations", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "max_iterations", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "limit", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "reason", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "behaviorRef", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "behaviorId", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "behavior", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "ref", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "weight", StringComparison.OrdinalIgnoreCase);
        }

        private static ActionSourceDefJson FindActionSchema(string type, Dictionary<string, ActionSourceDefJson> actionSchemas)
        {
            if (string.IsNullOrEmpty(type) || actionSchemas == null)
            {
                return null;
            }

            if (actionSchemas.TryGetValue(type, out var schema))
            {
                return schema;
            }

            foreach (var kvp in actionSchemas)
            {
                if (string.Equals(kvp.Value?.type, type, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value;
                }
            }

            return null;
        }

        private void WriteParamValue(JsonTextWriter writer, JToken value)
        {
            if (value == null || value.Type == JTokenType.Null)
            {
                throw new InvalidOperationException("Action parameter value cannot be null.");
            }

            switch (value.Type)
            {
                case JTokenType.Integer:
                case JTokenType.Float:
                    WriteConstValue(writer, value.Value<double>());
                    break;

                case JTokenType.String:
                    WriteStringValue(writer, value.ToString());
                    break;

                case JTokenType.Boolean:
                    WriteConstValue(writer, value.Value<bool>() ? 1.0 : 0.0);
                    break;

                case JTokenType.Object:
                    WriteObjectValue(writer, (JObject)value);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported action parameter token type: {value.Type}");
            }
        }

        private void WriteObjectValue(JsonTextWriter writer, JObject value)
        {
            if (value == null)
            {
                WriteConstValue(writer, 0);
                return;
            }

            var kind = ReadString(value, "kind", "type");
            var payload = ReadString(value, "payload", "payload_field", "payloadField", "field");
            if (!string.IsNullOrEmpty(payload) || IsKind(kind, "payload", "payload_field", "payloadField"))
            {
                WritePayloadFieldValue(writer, payload ?? ReadString(value, "name", "key"));
                return;
            }

            var constToken = value["const"] ?? value["value"] ?? value["constValue"] ?? value["ConstValue"];
            if (constToken != null && constToken.Type != JTokenType.Object && constToken.Type != JTokenType.Array)
            {
                WriteParamValue(writer, constToken);
                return;
            }

            var varDomain = ReadString(value, "var_domain", "varDomain", "domain", "domainId", "DomainId");
            var varKey = ReadString(value, "var_key", "varKey", "key", "Key");
            if (!string.IsNullOrEmpty(varDomain) && !string.IsNullOrEmpty(varKey) || IsKind(kind, "var", "variable"))
            {
                WriteVarValue(writer, string.IsNullOrEmpty(varDomain) ? "trigger" : varDomain, varKey);
                return;
            }

            var expr = ReadString(value, "expr", "expression", "exprText", "ExprText");
            if (!string.IsNullOrEmpty(expr) || IsKind(kind, "expr", "expression"))
            {
                WriteExprValue(writer, expr ?? string.Empty);
                return;
            }

            throw new InvalidOperationException("Unsupported action parameter value object. Expected const/value, payload, var or expr.");
        }

        private void WriteStringValue(JsonTextWriter writer, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                WriteConstValue(writer, 0);
                return;
            }

            if (value.StartsWith("payload:", StringComparison.OrdinalIgnoreCase))
            {
                WritePayloadFieldValue(writer, value.Substring("payload:".Length));
                return;
            }

            if (value.StartsWith("@"))
            {
                WritePayloadFieldValue(writer, value.Substring(1));
                return;
            }

            if (value.StartsWith("$"))
            {
                WriteVarValue(writer, "trigger", value.TrimStart('$'));
                return;
            }

            if (value.StartsWith("="))
            {
                WriteExprValue(writer, value.Substring(1).Trim());
                return;
            }

            if (double.TryParse(value, out var numValue))
            {
                WriteConstValue(writer, numValue);
                return;
            }

            if (value.StartsWith("%"))
            {
                WriteVarValue(writer, "trigger", value.TrimStart('%'));
                return;
            }

            throw new InvalidOperationException($"Unsupported action parameter string value: {value}");
        }

        private static bool IsKind(string kind, params string[] values)
        {
            if (string.IsNullOrEmpty(kind) || values == null) return false;
            for (var i = 0; i < values.Length; i++)
            {
                if (string.Equals(kind, values[i], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string NormalizeCompareOp(string op)
        {
            if (string.IsNullOrEmpty(op)) return "Equal";
            switch (op.Trim().ToLowerInvariant())
            {
                case "eq":
                case "=":
                case "==":
                case "equal":
                    return "Equal";
                case "ne":
                case "neq":
                case "!=":
                case "not_equal":
                case "not-equal":
                case "notequal":
                    return "NotEqual";
                case "gt":
                case ">":
                case "greater_than":
                case "greater-than":
                case "greaterthan":
                    return "GreaterThan";
                case "ge":
                case "gte":
                case ">=":
                case "greater_than_or_equal":
                case "greater-than-or-equal":
                case "greaterthanorequal":
                    return "GreaterThanOrEqual";
                case "lt":
                case "<":
                case "less_than":
                case "less-than":
                case "lessthan":
                    return "LessThan";
                case "le":
                case "lte":
                case "<=":
                case "less_than_or_equal":
                case "less-than-or-equal":
                case "lessthanorequal":
                    return "LessThanOrEqual";
                default:
                    return op;
            }
        }

        private static void WriteConstValue(JsonTextWriter writer, double value)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("Kind");
            writer.WriteValue("Const");
            writer.WritePropertyName("ConstValue");
            writer.WriteValue(value);
            writer.WriteEndObject();
        }

        private static void WritePayloadFieldValue(JsonTextWriter writer, string payloadField)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("Kind");
            writer.WriteValue("PayloadField");
            writer.WritePropertyName("FieldId");
            writer.WriteValue(string.IsNullOrEmpty(payloadField) ? 0 : StableStringId.Get("payload:" + payloadField));
            writer.WriteEndObject();
        }

        private static void WriteVarValue(JsonTextWriter writer, string domain, string key)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("Kind");
            writer.WriteValue("Var");
            writer.WritePropertyName("DomainId");
            writer.WriteValue(domain ?? string.Empty);
            writer.WritePropertyName("Key");
            writer.WriteValue(key ?? string.Empty);
            writer.WriteEndObject();
        }

        private static void WriteExprValue(JsonTextWriter writer, string expr)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("Kind");
            writer.WriteValue("Expr");
            writer.WritePropertyName("ExprText");
            writer.WriteValue(expr ?? string.Empty);
            writer.WriteEndObject();
        }

        /// <summary>
        /// 源格式 JSON 结构
        /// </summary>
#pragma warning disable 0649 // DTO fields are populated by JSON deserialization.
        private class TriggerPlanSourceJson
        {
            public string version;
            public TriggerSourceMetadataJson metadata;
            public List<TriggerSourceVariableJson> variables;
            public Dictionary<string, ActionSourceDefJson> actions;
            public Dictionary<string, ConditionSourceDefJson> conditions;
            public Dictionary<string, BehaviorSourceDefJson> behaviors;
            public Dictionary<string, JToken> conditionGroups;
            public Dictionary<string, JToken> condition_groups;
            public Dictionary<string, JToken> actionGroups;
            public Dictionary<string, JToken> action_groups;
            public List<TriggerSourceTriggerJson> triggers;
        }

        private class TriggerSourceMetadataJson
        {
            public string author;
            public string created_at;
            public string last_modified;
            public string description;
        }

        private class TriggerSourceVariableJson
        {
            public string name;
            public string description;
        }

        private class ActionSourceDefJson
        {
            public string type;
            public string displayName;
            public string description;
            public string category;
            public bool isComposite;
            public List<ActionSourceParamJson> @params;
        }

        private class ActionSourceParamJson
        {
            public string name;
            public string type;
            public bool required;
            public object defaultValue;
        }

        private class ConditionSourceDefJson
        {
            public string type;
            public string displayName;
            public string description;
            public string category;
            public bool isComposite;
            public List<ConditionSourceParamJson> @params;
        }

        private class ConditionSourceParamJson
        {
            public string name;
            public string type;
            public bool required;
            public object defaultValue;
        }

        private class BehaviorSourceDefJson
        {
            public string id;
            public string displayName;
            public string description;
            public JObject behavior;
            public JObject root;
        }
    
        private readonly struct ActionArgSource
        {
            public readonly string Name;
            public readonly JToken Value;

            public ActionArgSource(string name, JToken value)
            {
                Name = name;
                Value = value;
            }
        }

        private class TriggerSourceTriggerJson
        {
            public int id;
            public string name;
            public string @event;
            public int priority;
            public string phase;
            public string scope;
            public bool enabled;
            public bool allowExternal;
            public string comment;
            public List<JObject> conditions;
            public List<JObject> actions;
            public List<string> conditionRefs;
            public List<string> condition_refs;
            public List<string> actionRefs;
            public List<string> action_refs;
            public JObject behavior;
            public List<JObject> executables;
            public JToken execution;
            public JToken executionControl;
            public JToken execution_control;
            public bool once;
            public bool repeat;
            public int maxExecutions;
            public int max_executions;
            public float cooldownMs;
            public float cooldown_ms;
        }
#pragma warning restore 0649
    }
}
