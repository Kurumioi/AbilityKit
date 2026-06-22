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
    /// 瑙﹀彂鍣ㄨ鍒掓簮鏍煎紡杞崲鍣?    /// 灏嗕汉绫诲彲璇荤殑婧愭牸寮?JSON 杞崲涓鸿繍琛屾椂鏍煎紡 JSON
    /// </summary>
    public sealed class TriggerPlanSourceConverter
    {
        private readonly TriggerPlanSourceConditionWriter _conditionWriter = new TriggerPlanSourceConditionWriter();
        /// <summary>
        /// 灏嗘簮鏍煎紡 JSON 瀛楃涓茶浆鎹负杩愯鏃舵牸寮?JSON 瀛楃涓?        /// </summary>
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
            _conditionWriter.WritePredicate(writer, ResolveConditionList(trigger.conditions, trigger.conditionRefs, trigger.condition_refs, conditionCatalog, new HashSet<string>(StringComparer.OrdinalIgnoreCase)), WriteParamValue);

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
                    mode = TriggerPlanSourceJsonUtility.ReadString(obj, "mode", "type") ?? mode;
                    maxExecutions = TriggerPlanSourceJsonUtility.ReadInt(obj, maxExecutions, "max_executions", "maxExecutions", "count", "times");
                    cooldownMs = TriggerPlanSourceJsonUtility.ReadFloat(obj, cooldownMs, "cooldown_ms", "cooldownMs", "cooldown", "interval_ms", "intervalMs");
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
                    _conditionWriter.WritePredicate(writer, conditionNodes, WriteParamValue);
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
                    _conditionWriter.WritePredicate(writer, untilConditions, WriteParamValue);
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
            if (!TriggerPlanSourceJsonUtility.IsKind(type, "all", "and", "any", "or", "not"))
            {
                return normalized;
            }

            var inlineItems = TriggerPlanSourceConditionWriter.ReadConditionItems(normalized);
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
            return TriggerPlanSourceJsonUtility.IsKind(type, "seq", "sequence");
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
                    TriggerPlanSourceValueWriter.WriteConstValue(writer, value.Value<double>());
                    break;

                case JTokenType.String:
                    WriteStringValue(writer, value.ToString());
                    break;

                case JTokenType.Boolean:
                    TriggerPlanSourceValueWriter.WriteConstValue(writer, value.Value<bool>() ? 1.0 : 0.0);
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
                TriggerPlanSourceValueWriter.WriteConstValue(writer, 0);
                return;
            }

            var kind = TriggerPlanSourceJsonUtility.ReadString(value, "kind", "type");
            var payload = TriggerPlanSourceJsonUtility.ReadString(value, "payload", "payload_field", "payloadField", "field");
            if (!string.IsNullOrEmpty(payload) || TriggerPlanSourceJsonUtility.IsKind(kind, "payload", "payload_field", "payloadField"))
            {
                TriggerPlanSourceValueWriter.WritePayloadFieldValue(writer, payload ?? TriggerPlanSourceJsonUtility.ReadString(value, "name", "key"));
                return;
            }

            var constToken = value["const"] ?? value["value"] ?? value["constValue"] ?? value["ConstValue"];
            if (constToken != null && constToken.Type != JTokenType.Object && constToken.Type != JTokenType.Array)
            {
                WriteParamValue(writer, constToken);
                return;
            }

            var varDomain = TriggerPlanSourceJsonUtility.ReadString(value, "var_domain", "varDomain", "domain", "domainId", "DomainId");
            var varKey = TriggerPlanSourceJsonUtility.ReadString(value, "var_key", "varKey", "key", "Key");
            if (!string.IsNullOrEmpty(varDomain) && !string.IsNullOrEmpty(varKey) || TriggerPlanSourceJsonUtility.IsKind(kind, "var", "variable"))
            {
                TriggerPlanSourceValueWriter.WriteVarValue(writer, string.IsNullOrEmpty(varDomain) ? "trigger" : varDomain, varKey);
                return;
            }

            var expr = TriggerPlanSourceJsonUtility.ReadString(value, "expr", "expression", "exprText", "ExprText");
            if (!string.IsNullOrEmpty(expr) || TriggerPlanSourceJsonUtility.IsKind(kind, "expr", "expression"))
            {
                TriggerPlanSourceValueWriter.WriteExprValue(writer, expr ?? string.Empty);
                return;
            }

            throw new InvalidOperationException("Unsupported action parameter value object. Expected const/value, payload, var or expr.");
        }

        private void WriteStringValue(JsonTextWriter writer, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                TriggerPlanSourceValueWriter.WriteConstValue(writer, 0);
                return;
            }

            if (value.StartsWith("payload:", StringComparison.OrdinalIgnoreCase))
            {
                TriggerPlanSourceValueWriter.WritePayloadFieldValue(writer, value.Substring("payload:".Length));
                return;
            }

            if (value.StartsWith("@"))
            {
                TriggerPlanSourceValueWriter.WritePayloadFieldValue(writer, value.Substring(1));
                return;
            }

            if (value.StartsWith("$"))
            {
                TriggerPlanSourceValueWriter.WriteVarValue(writer, "trigger", value.TrimStart('$'));
                return;
            }

            if (value.StartsWith("="))
            {
                TriggerPlanSourceValueWriter.WriteExprValue(writer, value.Substring(1).Trim());
                return;
            }

            if (double.TryParse(value, out var numValue))
            {
                TriggerPlanSourceValueWriter.WriteConstValue(writer, numValue);
                return;
            }

            if (value.StartsWith("%"))
            {
                TriggerPlanSourceValueWriter.WriteVarValue(writer, "trigger", value.TrimStart('%'));
                return;
            }

            throw new InvalidOperationException($"Unsupported action parameter string value: {value}");
        }

    }
}
