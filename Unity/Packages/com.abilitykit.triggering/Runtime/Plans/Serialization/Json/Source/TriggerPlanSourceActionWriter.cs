using System;
using System.Collections.Generic;
using AbilityKit.Triggering.Eventing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AbilityKit.Triggering.Runtime.Plan.Json
{
    /// <summary>
    /// Writes source-format action definitions and action argument value tokens into runtime JSON.
    /// </summary>
    internal sealed class TriggerPlanSourceActionWriter
    {
        public void WriteAction(
            JsonTextWriter writer,
            JObject action,
            Dictionary<string, ActionSourceDefJson> actionSchemas,
            Dictionary<string, JToken> actionCatalog,
            Func<JObject, Dictionary<string, JToken>, HashSet<string>, JObject> resolveAction)
        {
            if (resolveAction == null) throw new ArgumentNullException(nameof(resolveAction));

            action = resolveAction(action, actionCatalog, new HashSet<string>(StringComparer.OrdinalIgnoreCase)) ?? action;
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

        public void WriteParamValue(JsonTextWriter writer, JToken value)
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

        private static List<ActionArgSource> BuildOrderedActionArgs(JObject action, string type, Dictionary<string, ActionSourceDefJson> actionSchemas)
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
