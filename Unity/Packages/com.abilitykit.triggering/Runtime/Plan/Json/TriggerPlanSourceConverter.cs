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
                            WriteTrigger(writer, trigger, source.actions);
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

        private void WriteTrigger(JsonTextWriter writer, TriggerSourceTriggerJson trigger, Dictionary<string, ActionSourceDefJson> actionSchemas)
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

            writer.WriteEndObject();
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
            var value = cond["value"]?.Value<double>() ?? 0;
            var argFieldId = string.IsNullOrEmpty(argName) ? 0 : StableStringId.Get("payload:" + argName);

            writer.WriteStartObject();
            writer.WritePropertyName("Kind");
            writer.WriteValue("CompareNumeric");
            writer.WritePropertyName("CompareOp");
            writer.WriteValue(op);
            writer.WritePropertyName("Left");
            writer.WriteStartObject();
            writer.WritePropertyName("Kind");
            writer.WriteValue("PayloadField");
            writer.WritePropertyName("FieldId");
            writer.WriteValue(argFieldId);
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
                if (consumed.Contains(prop.Name)) continue;

                result.Add(new ActionArgSource(prop.Name, prop.Value));
            }

            return result;
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
        }
    }
}
