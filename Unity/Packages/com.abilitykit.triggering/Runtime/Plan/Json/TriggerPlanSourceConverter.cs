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
                            WriteTrigger(writer, trigger, source.actions, behaviorCatalog);
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

        private void WriteTrigger(JsonTextWriter writer, TriggerSourceTriggerJson trigger, Dictionary<string, ActionSourceDefJson> actionSchemas, Dictionary<string, JObject> behaviorCatalog)
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
            WritePredicate(writer, trigger.conditions);

            WriteExecutionControl(writer, trigger);

            writer.WritePropertyName("Actions");
            writer.WriteStartArray();
            if (trigger.actions != null)
            {
                foreach (var action in trigger.actions)
                {
                    WriteAction(writer, action, actionSchemas);
                }
            }
            writer.WriteEndArray();

            var root = GetExecutionRootSource(trigger);
            if (root != null)
            {
                writer.WritePropertyName("ExecutionRoot");
                WriteExecutionNode(writer, root, actionSchemas, behaviorCatalog, $"trigger:{trigger.id}", "inline", trigger.id.ToString());
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
            return phase.ToLowerInvariant() switch
            {
                "immediate" => 0,
                "delayed" => 1,
                "precondition" => 2,
                "postcondition" => 3,
                _ => 0
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
                default:
                    return TriggerPlanScope.Global;
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

                foreach (var cond in conditions)
                {
                    WriteCondition(writer, cond);
                }

                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }

        private void WriteCondition(JsonTextWriter writer, JObject cond)
        {
            var type = cond["type"]?.ToString();
            if (string.IsNullOrEmpty(type))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("Kind");
                writer.WriteValue("Const");
                writer.WritePropertyName("ConstValue");
                writer.WriteValue(true);
                writer.WriteEndObject();
                return;
            }

            switch (type)
            {
                case "all":
                case "any":
                case "not":
                    var inner = cond["items"] ?? cond["item"];
                    if (inner is JArray arr)
                    {
                        foreach (var item in arr)
                        {
                            WriteCondition(writer, (JObject)item);
                        }
                    }
                    else if (inner is JObject obj)
                    {
                        WriteCondition(writer, obj);
                    }

                    if (type == "not")
                    {
                        writer.WriteStartObject();
                        writer.WritePropertyName("Kind");
                        writer.WriteValue("Not");
                        writer.WriteEndObject();
                    }
                    break;

                case "arg_eq":
                    WriteCompareNode(writer, cond, "Eq");
                    break;

                case "arg_gt":
                    WriteCompareNode(writer, cond, "Gt");
                    break;

                case "arg_gte":
                    WriteCompareNode(writer, cond, "Ge");
                    break;

                case "arg_lt":
                    WriteCompareNode(writer, cond, "Lt");
                    break;

                case "arg_lte":
                    WriteCompareNode(writer, cond, "Le");
                    break;

                case "arg_neq":
                    WriteCompareNode(writer, cond, "Ne");
                    break;

                default:
                    break;
            }
        }

        private void WriteCompareNode(JsonTextWriter writer, JObject cond, string op)
        {
            var argName = cond["arg_name"]?.ToString();
            var leftVarDomain = (cond["left_var_domain"] ?? cond["var_domain"])?.ToString();
            var leftVarKey = (cond["left_var_key"] ?? cond["var_key"])?.ToString();
            var value = cond["value"]?.Value<double>() ?? 0;
            var argFieldId = string.IsNullOrEmpty(argName) ? 0 : StableStringId.Get("payload:" + argName);

            writer.WriteStartObject();
            writer.WritePropertyName("Kind");
            writer.WriteValue("CompareNumeric");
            writer.WritePropertyName("CompareOp");
            writer.WriteValue(op);
            writer.WritePropertyName("Left");
            writer.WriteStartObject();
            if (!string.IsNullOrEmpty(leftVarDomain) && !string.IsNullOrEmpty(leftVarKey))
            {
                writer.WritePropertyName("Kind");
                writer.WriteValue("Var");
                writer.WritePropertyName("DomainId");
                writer.WriteValue(leftVarDomain);
                writer.WritePropertyName("Key");
                writer.WriteValue(leftVarKey);
            }
            else
            {
                writer.WritePropertyName("Kind");
                writer.WriteValue("PayloadField");
                writer.WritePropertyName("FieldId");
                writer.WriteValue(argFieldId);
            }
            writer.WriteEndObject();
            writer.WritePropertyName("Right");
            writer.WriteStartObject();
            writer.WritePropertyName("Kind");
            writer.WriteValue("Const");
            writer.WritePropertyName("ConstValue");
            writer.WriteValue(value);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        private void WriteExecutionNode(JsonTextWriter writer, JObject node, Dictionary<string, ActionSourceDefJson> actionSchemas, Dictionary<string, JObject> behaviorCatalog, string sourcePath, string sourceKind, string sourceId)
        {
            node = ResolveBehaviorReference(node, behaviorCatalog, ref sourceKind, ref sourceId, ref sourcePath);
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

            if (TryGetNodeCondition(node, out var conditionNodes))
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

            WriteExecutionNodeOptions(writer, node, normalizedKind);

            if (string.Equals(normalizedKind, "Action", StringComparison.OrdinalIgnoreCase))
            {
                var action = node["action"] as JObject ?? node;
                writer.WritePropertyName("Action");
                WriteAction(writer, action, actionSchemas);
                writer.WriteEndObject();
                return;
            }

            var children = GetNodeChildren(node);
            writer.WritePropertyName("Children");
            writer.WriteStartArray();
            if (children != null)
            {
                var index = 0;
                foreach (var child in children)
                {
                    if (child is JObject childObj)
                    {
                        WriteExecutionNode(writer, childObj, actionSchemas, behaviorCatalog, $"{sourcePath}/children[{index}]", sourceKind, sourceId);
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
                            WriteExecutionNode(writer, childObj, actionSchemas, behaviorCatalog, $"{sourcePath}/else[{index}]", sourceKind, sourceId);
                        }
                        index++;
                    }
                    writer.WriteEndArray();
                }
            }

            writer.WriteEndObject();
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
                    return HasCompositeChildren(node) ? "Sequence" : "Action";
            }
        }

        private static bool HasCompositeChildren(JObject node)
        {
            return node != null && (node["children"] is JArray || node["items"] is JArray || node["then"] is JArray || node["child"] is JObject);
        }

        private static JArray GetNodeChildren(JObject node)
        {
            if (node == null) return null;
            if (node["children"] is JArray children) return children;
            if (node["items"] is JArray items) return items;
            if (node["then"] is JArray thenChildren) return thenChildren;
            if (node["then"] is JObject thenOne) return new JArray(thenOne);
            if (node["child"] is JObject childOne) return new JArray(childOne);
            return null;
        }

        private static JArray GetNodeElseChildren(JObject node)
        {
            if (node == null) return null;
            if (node["elseChildren"] is JArray elseChildren) return elseChildren;
            if (node["else"] is JArray elseItems) return elseItems;
            if (node["else"] is JObject elseOne) return new JArray(elseOne);
            return null;
        }

        private void WriteExecutionNodeOptions(JsonTextWriter writer, JObject node, string normalizedKind)
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

                if (TryGetUntilCondition(node, out var untilConditions))
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

        private static bool TryGetUntilCondition(JObject node, out List<JObject> conditions)
        {
            conditions = null;
            if (node == null) return false;

            var token = node["until"] ?? node["untilCondition"] ?? node["until_condition"];
            return TryBuildConditionList(token, out conditions);
        }

        private static bool TryGetNodeCondition(JObject node, out List<JObject> conditions)
        {
            conditions = null;
            if (node == null) return false;

            var token = node["condition"] ?? node["conditions"] ?? node["when"];
            return TryBuildConditionList(token, out conditions);
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

        private static JObject ResolveBehaviorReference(JObject node, Dictionary<string, JObject> behaviorCatalog, ref string sourceKind, ref string sourceId, ref string sourcePath)
        {
            var behaviorId = GetBehaviorReferenceId(node);
            if (string.IsNullOrEmpty(behaviorId))
                return node;

            if (behaviorCatalog == null || !behaviorCatalog.TryGetValue(behaviorId, out var root) || root == null)
            {
                throw new InvalidOperationException($"Behavior reference not found: {behaviorId}");
            }

            sourceKind = "behavior";
            sourceId = behaviorId;
            sourcePath = $"behavior:{behaviorId}";
            return (JObject)root.DeepClone();
        }

        private static string GetBehaviorReferenceId(JObject node)
        {
            if (node == null) return null;
            return node["behaviorRef"]?.ToString()
                ?? node["behaviorId"]?.ToString()
                ?? node["behavior"]?.ToString()
                ?? node["ref"]?.ToString();
        }

        private void WriteAction(JsonTextWriter writer, JObject action, Dictionary<string, ActionSourceDefJson> actionSchemas)
        {
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
            if (value == null)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("Kind");
                writer.WriteValue("Const");
                writer.WritePropertyName("ConstValue");
                writer.WriteValue(0);
                writer.WriteEndObject();
                return;
            }

            switch (value.Type)
            {
                case JTokenType.Integer:
                case JTokenType.Float:
                    writer.WriteStartObject();
                    writer.WritePropertyName("Kind");
                    writer.WriteValue("Const");
                    writer.WritePropertyName("ConstValue");
                    writer.WriteValue(value.Value<double>());
                    writer.WriteEndObject();
                    break;

                case JTokenType.String:
                    var strValue = value.ToString();
                    WriteStringValue(writer, strValue);
                    break;

                case JTokenType.Boolean:
                    writer.WriteStartObject();
                    writer.WritePropertyName("Kind");
                    writer.WriteValue("Const");
                    writer.WritePropertyName("ConstValue");
                    writer.WriteValue(value.Value<bool>() ? 1.0 : 0.0);
                    writer.WriteEndObject();
                    break;

                default:
                    writer.WriteStartObject();
                    writer.WritePropertyName("Kind");
                    writer.WriteValue("Const");
                    writer.WritePropertyName("ConstValue");
                    writer.WriteValue(0);
                    writer.WriteEndObject();
                    break;
            }
        }

        private void WriteStringValue(JsonTextWriter writer, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("Kind");
                writer.WriteValue("Const");
                writer.WritePropertyName("ConstValue");
                writer.WriteValue(0);
                writer.WriteEndObject();
                return;
            }

            if (value.StartsWith("$"))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("Kind");
                writer.WriteValue("Var");
                writer.WritePropertyName("DomainId");
                writer.WriteValue("trigger");
                writer.WritePropertyName("Key");
                writer.WriteValue(value.TrimStart('$'));
                writer.WriteEndObject();
                return;
            }

            if (value.StartsWith("="))
            {
                var expr = value.Substring(1).Trim();
                writer.WriteStartObject();
                writer.WritePropertyName("Kind");
                writer.WriteValue("Expr");
                writer.WritePropertyName("ExprText");
                writer.WriteValue(expr);
                writer.WriteEndObject();
                return;
            }

            if (double.TryParse(value, out var numValue))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("Kind");
                writer.WriteValue("Const");
                writer.WritePropertyName("ConstValue");
                writer.WriteValue(numValue);
                writer.WriteEndObject();
                return;
            }

            if (value.StartsWith("%"))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("Kind");
                writer.WriteValue("Var");
                writer.WritePropertyName("DomainId");
                writer.WriteValue("trigger");
                writer.WritePropertyName("Key");
                writer.WriteValue(value.TrimStart('%'));
                writer.WriteEndObject();
                return;
            }

            writer.WriteStartObject();
            writer.WritePropertyName("Kind");
            writer.WriteValue("Const");
            writer.WritePropertyName("ConstValue");
            writer.WriteValue(0);
            writer.WriteEndObject();
        }

        /// <summary>
        /// 源格式 JSON 结构
        /// </summary>
        private class TriggerPlanSourceJson
        {
            public string version;
            public TriggerSourceMetadataJson metadata;
            public List<TriggerSourceVariableJson> variables;
            public Dictionary<string, ActionSourceDefJson> actions;
            public Dictionary<string, ConditionSourceDefJson> conditions;
            public Dictionary<string, BehaviorSourceDefJson> behaviors;
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
    }
}
