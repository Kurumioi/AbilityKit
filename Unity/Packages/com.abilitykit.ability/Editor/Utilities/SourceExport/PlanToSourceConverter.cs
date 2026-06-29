#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using AbilityKit.Ability.Config.Source;
using AbilityKit.Ability.Editor.Utilities;
using AbilityKit.Ability.Triggering.Runtime;
using AbilityKit.Triggering.Eventing;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime.Plan;
using Newtonsoft.Json;
using UnityEngine;

namespace AbilityKit.Ability.Editor.Utilities
{
    /// <summary>
    /// Plan JSON → Source JSON 转换器
    /// 从运行时 Plan 格式反向转换为可读的 Source 格式
    /// </summary>
    internal static class PlanToSourceConverter
    {
        /// <summary>
        /// 转换结果
        /// </summary>
        public class ConvertResult
        {
            public bool Success = true;
            public TriggerSourceConfig Source;
            public List<string> Errors = new List<string>();
            public List<string> Warnings = new List<string>();
            public Dictionary<int, string> IdToActionName = new Dictionary<int, string>();
            public Dictionary<int, string> IdToConditionName = new Dictionary<int, string>();
            public Dictionary<int, string> IdToEventName = new Dictionary<int, string>();
            public Dictionary<int, string> Strings = new Dictionary<int, string>();

            public void AddError(string msg) { Errors.Add(msg); Success = false; }
            public void AddWarning(string msg) { Warnings.Add(msg); }
        }

        /// <summary>
        /// 从 Plan JSON 字符串转换
        /// </summary>
        public static ConvertResult Convert(string planJson)
        {
            var result = new ConvertResult();

            if (string.IsNullOrWhiteSpace(planJson))
            {
                result.AddError("Plan JSON is empty");
                return result;
            }

            TriggerPlanDatabaseDto planDb;
            try
            {
                planDb = JsonConvert.DeserializeObject<TriggerPlanDatabaseDto>(planJson);
                if (planDb == null)
                {
                    result.AddError("Failed to parse Plan JSON");
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.AddError($"JSON parse error: {ex.Message}");
                return result;
            }

            return Convert(planDb);
        }

        /// <summary>
        /// 从 Plan 配置对象转换
        /// </summary>
        public static ConvertResult Convert(TriggerPlanDatabaseDto planDb)
        {
            var result = new ConvertResult();

            if (planDb == null)
            {
                result.AddError("Plan database is null");
                return result;
            }

            var source = new TriggerSourceConfig
            {
                Schema = "abilitykit-trigger-source-v1",
                Version = "1.0",
                Metadata = new SourceMetadata
                {
                    Author = "auto-generated",
                    Description = "从 Plan JSON 自动生成的源配置",
                    CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    LastModified = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                },
                Variables = new List<SourceVariable>
                {
                    new SourceVariable("$caster", "技能释放者"),
                    new SourceVariable("$target", "目标实体"),
                    new SourceVariable("$self", "触发者自身")
                },
                Actions = new Dictionary<string, ActionTypeDefinition>(),
                Conditions = new Dictionary<string, ConditionTypeDefinition>(),
                Triggers = new List<SourceTriggerConfig>()
            };

            // 复制字符串表
            if (planDb.Strings != null)
            {
                foreach (var kvp in planDb.Strings)
                {
                    result.Strings[kvp.Key] = kvp.Value;
                }
            }

            // 1. 构建 ID → 名称 映射（从所有触发器收集）
            BuildIdToNameMaps(planDb, result);

            // 2. 转换触发器
            if (planDb.Triggers != null)
            {
                foreach (var triggerDto in planDb.Triggers)
                {
                    var trigger = ConvertTrigger(triggerDto, result);
                    if (trigger != null)
                    {
                        source.Triggers.Add(trigger);
                    }
                }
            }

            // 3. 生成类型定义
            GenerateTypeDefinitions(source, result);

            result.Source = source;
            return result;
        }

        /// <summary>
        /// 构建 ID 到名称的映射表
        /// </summary>
        private static void BuildIdToNameMaps(TriggerPlanDatabaseDto planDb, ConvertResult result)
        {
            // 从 ActionId 反查动作名称
            // 注意：这里需要运行时注册的 ActionId
            foreach (var kvp in result.IdToActionName)
            {
                // 已经有了
            }

            // 从触发器收集 EventId
            if (planDb.Triggers != null)
            {
                foreach (var trigger in planDb.Triggers)
                {
                    if (trigger.EventId > 0)
                    {
                        var name = GetEventNameFromId(trigger.EventId);
                        result.IdToEventName[trigger.EventId] = name ?? trigger.EventName;
                    }

                    // 收集动作和条件 ID
                    if (trigger.Actions != null)
                    {
                        foreach (var action in trigger.Actions)
                        {
                            if (action.ActionId > 0 && !result.IdToActionName.ContainsKey(action.ActionId))
                            {
                                var name = GetActionNameFromId(action.ActionId);
                                result.IdToActionName[action.ActionId] = name ?? $"action_{action.ActionId}";
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 根据 ActionId 获取动作名称
        /// </summary>
        private static string GetActionNameFromId(int actionId)
        {
            // 遍历已知动作类型
            var knownTypes = new[]
            {
                ("debug_log", "action:debug_log"),
                ("shoot_projectile", "action:shoot_projectile"),
                ("give_damage", "action:give_damage"),
                ("add_buff", "action:add_buff"),
                ("effect_execute", "action:effect_execute"),
                ("play_presentation", "action:play_presentation"),
                ("spawn_summon", "action:spawn_summon"),
                ("seq", "action:seq"),
                ("set_var", "action:set_var"),
                ("log_attacker", "action:log_attacker")
            };

            foreach (var (name, fullName) in knownTypes)
            {
                if (StableStringId.Get(fullName) == actionId)
                {
                    return name;
                }
            }

            return null;
        }

        /// <summary>
        /// 根据 EventId 获取事件名称
        /// </summary>
        private static string GetEventNameFromId(int eventId)
        {
            return null; // 无法反向解析
        }

        /// <summary>
        /// 转换单个触发器
        /// </summary>
        private static SourceTriggerConfig ConvertTrigger(TriggerPlanDto triggerDto, ConvertResult result)
        {
            if (triggerDto == null) return null;

            var trigger = new SourceTriggerConfig
            {
                Id = triggerDto.TriggerId,
                Name = $"Trigger_{triggerDto.TriggerId}",
                Event = triggerDto.EventName,
                Priority = triggerDto.Priority,
                Phase = GetPhaseName(triggerDto.Phase),
                Enabled = true,
                AllowExternal = triggerDto.AllowExternal,
                Conditions = new List<SourceConditionConfig>(),
                Actions = new List<SourceActionConfig>()
            };

            // 转换条件
            if (triggerDto.Predicate != null)
            {
                ConvertPredicate(triggerDto.Predicate, trigger.Conditions, result);
            }

            // 转换动作
            if (triggerDto.Actions != null)
            {
                foreach (var actionDto in triggerDto.Actions)
                {
                    var action = ConvertAction(actionDto, result);
                    if (action != null)
                    {
                        trigger.Actions.Add(action);
                    }
                }
            }

            return trigger;
        }

        /// <summary>
        /// 转换谓词/条件
        /// </summary>
        private static void ConvertPredicate(
            PredicatePlanDto predicate,
            List<SourceConditionConfig> conditions,
            ConvertResult result)
        {
            if (predicate == null) return;

            if (predicate.Kind == "none" || predicate.Nodes == null || predicate.Nodes.Count == 0)
            {
                return;
            }

            var condition = ConvertBoolExprNodes(predicate.Nodes, result);
            if (condition == null)
            {
                return;
            }

            if (string.Equals(condition.Type, "all", StringComparison.OrdinalIgnoreCase) && condition.Items != null)
            {
                conditions.AddRange(condition.Items);
                return;
            }

            conditions.Add(condition);
        }

        private static SourceConditionConfig ConvertBoolExprNodes(List<BoolExprNodeDto> nodes, ConvertResult result)
        {
            var stack = new Stack<SourceConditionConfig>();
            foreach (var node in nodes)
            {
                if (node == null) continue;

                if (SourceConditionPlanMapping.IsKind(node.Kind, SourceConditionPlanMapping.BoolKindAnd) || SourceConditionPlanMapping.IsKind(node.Kind, SourceConditionPlanMapping.BoolKindOr))
                {
                    if (stack.Count < 2)
                    {
                        result.AddWarning($"Invalid bool expression: {node.Kind} stack underflow");
                        return null;
                    }

                    var right = stack.Pop();
                    var left = stack.Pop();
                    var type = SourceConditionPlanMapping.IsKind(node.Kind, SourceConditionPlanMapping.BoolKindAnd) ? "all" : "any";
                    stack.Push(MergeCompositeCondition(type, left, right));
                    continue;
                }

                if (SourceConditionPlanMapping.IsKind(node.Kind, SourceConditionPlanMapping.BoolKindNot))
                {
                    if (stack.Count < 1)
                    {
                        result.AddWarning("Invalid bool expression: Not stack underflow");
                        return null;
                    }

                    stack.Push(new SourceConditionConfig
                    {
                        Type = "not",
                        Item = stack.Pop()
                    });
                    continue;
                }

                var condition = ConvertBoolExprNode(node, result);
                if (condition != null)
                {
                    stack.Push(condition);
                }
            }

            if (stack.Count == 0)
            {
                return null;
            }

            if (stack.Count > 1)
            {
                var items = new List<SourceConditionConfig>();
                while (stack.Count > 0)
                {
                    items.Insert(0, stack.Pop());
                }

                result.AddWarning("Bool expression has multiple roots, wrapped with all condition");
                return new SourceConditionConfig { Type = "all", Items = items };
            }

            return stack.Pop();
        }

        private static SourceConditionConfig MergeCompositeCondition(string type, SourceConditionConfig left, SourceConditionConfig right)
        {
            var items = new List<SourceConditionConfig>();
            AddCompositeItem(items, type, left);
            AddCompositeItem(items, type, right);
            return new SourceConditionConfig { Type = type, Items = items };
        }

        private static void AddCompositeItem(List<SourceConditionConfig> items, string type, SourceConditionConfig condition)
        {
            if (condition == null) return;
            if (string.Equals(condition.Type, type, StringComparison.OrdinalIgnoreCase) && condition.Items != null)
            {
                items.AddRange(condition.Items);
                return;
            }

            items.Add(condition);
        }

        /// <summary>
        /// 转换布尔表达式节点
        /// </summary>
        private static SourceConditionConfig ConvertBoolExprNode(BoolExprNodeDto node, ConvertResult result)
        {
            if (node == null) return null;

            if (SourceConditionPlanMapping.IsKind(node.Kind, SourceConditionPlanMapping.BoolKindConst))
            {
                return new SourceConditionConfig { Type = node.ConstValue ? "always_true" : "always_false" };
            }

            if (SourceConditionPlanMapping.IsKind(node.Kind, SourceConditionPlanMapping.BoolKindCompareNumeric) || SourceConditionPlanMapping.IsKind(node.Kind, "compare"))
            {
                if (!SourceConditionPlanMapping.TryGetConditionTypeForCompareOp(node.CompareOp, out var conditionType))
                {
                    result.AddWarning($"Unknown compare op: {node.CompareOp}, fallback to arg_eq");
                    conditionType = "arg_eq";
                }

                var condition = new SourceConditionConfig
                {
                    Type = conditionType,
                    Args = new Dictionary<string, object>()
                };

                if (node.Left != null)
                {
                    condition.Args["arg_name"] = ExtractArgName(node.Left);
                }

                if (node.Right != null)
                {
                    condition.Args["value"] = ExtractValue(node.Right);
                }

                return condition;
            }

            result.AddWarning($"Unknown BoolExprNode Kind: {node.Kind}");
            return null;
        }

        /// <summary>
        /// 提取参数名
        /// </summary>
        private static string ExtractArgName(NumericValueRefDto ref_)
        {
            if (ref_ == null) return "arg1";

            switch (ref_.Kind)
            {
                case "PayloadField":
                    return GetFieldNameFromId(ref_.FieldId);
                case "Var":
                    return ref_.Key ?? "arg1";
                default:
                    return "arg1";
            }
        }

        /// <summary>
        /// 根据 FieldId 获取字段名
        /// </summary>
        private static string GetFieldNameFromId(int fieldId)
        {
            // 常见 payload 字段
            if (fieldId == StableStringId.Get("payload:caster")) return "caster";
            if (fieldId == StableStringId.Get("payload:target")) return "target";
            if (fieldId == StableStringId.Get("payload:self")) return "self";
            return "arg1";
        }

        /// <summary>
        /// 提取值
        /// </summary>
        private static object ExtractValue(NumericValueRefDto ref_)
        {
            if (ref_ == null) return 0;

            switch (ref_.Kind)
            {
                case "Const":
                    return ref_.ConstValue;
                case "Expr":
                    return ref_.ExprText;
                case "Var":
                    return "$" + (ref_.Key ?? "var");
                default:
                    return ref_.ConstValue;
            }
        }

        /// <summary>
        /// 转换动作
        /// </summary>
        private static SourceActionConfig ConvertAction(ActionCallPlanDto actionDto, ConvertResult result)
        {
            if (actionDto == null) return null;

            var action = new SourceActionConfig();

            // 获取动作名称
            var actionName = result.IdToActionName.TryGetValue(actionDto.ActionId, out var name)
                ? name
                : $"unknown_{actionDto.ActionId}";
            action.Type = actionName;

            // 转换参数
            action.Args = new Dictionary<string, object>();

            if (actionDto.Args != null)
            {
                foreach (var kvp in actionDto.Args)
                {
                    action.Args[kvp.Key] = ConvertValueRef(kvp.Value, result);
                }
            }

            // 特殊处理：根据动作类型调整参数
            NormalizeActionParams(action, actionDto, result);

            return action;
        }

        /// <summary>
        /// 转换值引用
        /// </summary>
        private static object ConvertValueRef(NumericValueRefDto ref_, ConvertResult result)
        {
            if (ref_ == null) return 0;

            switch (ref_.Kind)
            {
                case "Const":
                    return ref_.ConstValue;

                case "Var":
                    if (!string.IsNullOrEmpty(ref_.Key))
                    {
                        return "$" + ref_.Key;
                    }
                    return ref_.ConstValue;

                case "Expr":
                    return ref_.ExprText ?? "";

                case "Blackboard":
                    // TODO: 转换为黑板引用格式
                    return $"bb:{ref_.BoardId}:{ref_.KeyId}";

                case "PayloadField":
                    var fieldName = GetFieldNameFromId(ref_.FieldId);
                    return "$" + fieldName;

                default:
                    return ref_.ConstValue;
            }
        }

        /// <summary>
        /// 根据动作类型规范化参数
        /// </summary>
        private static void NormalizeActionParams(
            SourceActionConfig action,
            ActionCallPlanDto actionDto,
            ConvertResult result)
        {
            switch (action.Type)
            {
                case "debug_log":
                    // msg_id 需要转换为实际字符串
                    if (action.Args.TryGetValue("msg_id", out var msgIdObj))
                    {
                        var strId = System.Convert.ToInt32(msgIdObj);
                        if (result.Strings.TryGetValue(strId, out var msg))
                        {
                            action.Args["message"] = msg;
                        }
                        else
                        {
                            action.Args["message"] = $"<string:{strId}>";
                        }
                        action.Args.Remove("msg_id");
                    }

                    if (action.Args.TryGetValue("dump", out var dumpObj))
                    {
                        action.Args["dump_args"] = System.Convert.ToInt32(dumpObj) != 0;
                    }
                    break;

                case "shoot_projectile":
                    RenameArg(action.Args, "launcher_id", "launcher");
                    RenameArg(action.Args, "target_id", "target");
                    break;

                case "give_damage":
                    RenameArg(action.Args, "from_id", "from");
                    RenameArg(action.Args, "to_id", "to");
                    RenameArg(action.Args, "damage_amount", "amount");
                    break;

                case "add_buff":
                    RenameArg(action.Args, "target_actor_id", "target");
                    CollapsePrefixedArgs(action.Args, "buffids", "buffIds");
                    RenameArg(action.Args, "buff_id", "buffIds");
                    break;
            }
        }

        /// <summary>
        /// 重命名参数字段
        /// </summary>
        private static void RenameArg(Dictionary<string, object> args, string oldKey, string newKey)
        {
            if (args.TryGetValue(oldKey, out var value))
            {
                args[newKey] = value;
                args.Remove(oldKey);
            }
        }

        private static void CollapsePrefixedArgs(Dictionary<string, object> args, string prefix, string targetKey)
        {
            var values = new List<int>();
            var keysToRemove = new List<string>();
            foreach (var kv in args)
            {
                if (!kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (TryConvertToInt(kv.Value, out var value) && value > 0)
                {
                    values.Add(value);
                    keysToRemove.Add(kv.Key);
                }
            }

            if (values.Count == 0)
            {
                return;
            }

            args[targetKey] = values;
            for (int i = 0; i < keysToRemove.Count; i++)
            {
                args.Remove(keysToRemove[i]);
            }
        }

        private static bool TryConvertToInt(object value, out int intValue)
        {
            switch (value)
            {
                case null:
                    intValue = 0;
                    return false;
                case int i:
                    intValue = i;
                    return true;
                case long l:
                    intValue = (int)l;
                    return true;
                case double d:
                    intValue = (int)Math.Round(d);
                    return true;
                case float f:
                    intValue = (int)Math.Round(f);
                    return true;
                default:
                    try
                    {
                        intValue = System.Convert.ToInt32(value);
                        return true;
                    }
                    catch
                    {
                        intValue = 0;
                        return false;
                    }
            }
        }

        /// <summary>
        /// 获取阶段名称
        /// </summary>
        private static string GetPhaseName(int phase)
        {
            switch (phase)
            {
                case 0: return "immediate";
                case 1: return "early";
                case 2: return "late";
                default: return "immediate";
            }
        }

        /// <summary>
        /// 生成类型定义
        /// </summary>
        private static void GenerateTypeDefinitions(TriggerSourceConfig source, ConvertResult result)
        {
            // 动作类型定义
            source.Actions["seq"] = new ActionTypeDefinition
            {
                Type = "seq",
                DisplayName = "顺序执行",
                Description = "按顺序执行多个动作",
                Category = "流程",
                IsComposite = true,
                Params = new List<ParameterDefinition>
                {
                    new ParameterDefinition("items", "action[]", true)
                }
            };

            source.Actions["debug_log"] = new ActionTypeDefinition
            {
                Type = "debug_log",
                DisplayName = "调试日志",
                Description = "输出调试信息",
                Category = "调试",
                Params = new List<ParameterDefinition>
                {
                    new ParameterDefinition("message", "string", true),
                    new ParameterDefinition("dump_args", "bool", false) { DefaultValue = false }
                }
            };

            source.Actions["shoot_projectile"] = new ActionTypeDefinition
            {
                Type = "shoot_projectile",
                DisplayName = "发射弹道",
                Description = "发射一个弹道向目标移动",
                Category = "战斗",
                Params = new List<ParameterDefinition>
                {
                    new ParameterDefinition("launcher", "entity", true),
                    new ParameterDefinition("target", "entity", true),
                    new ParameterDefinition("projectile_id", "int", true),
                    new ParameterDefinition("speed", "float", false) { DefaultValue = 300.0 }
                }
            };

            source.Actions["give_damage"] = new ActionTypeDefinition
            {
                Type = "give_damage",
                DisplayName = "造成伤害",
                Description = "对目标造成伤害",
                Category = "战斗",
                Params = new List<ParameterDefinition>
                {
                    new ParameterDefinition("from", "entity", true),
                    new ParameterDefinition("to", "entity", true),
                    new ParameterDefinition("amount", "expr", true),
                    new ParameterDefinition("reason", "int", false) { DefaultValue = 0 }
                }
            };

            source.Actions["add_buff"] = new ActionTypeDefinition
            {
                Type = "add_buff",
                DisplayName = "添加Buff",
                Description = "为目标添加Buff效果",
                Category = "Buff",
                Params = new List<ParameterDefinition>
                {
                    new ParameterDefinition("target", "entity", false),
                    new ParameterDefinition("target_self", "bool", false) { DefaultValue = false },
                    new ParameterDefinition("buffIds", "int[]", true),
                    new ParameterDefinition("buff_id", "int", false),
                    new ParameterDefinition("duration", "float", false) { DefaultValue = -1.0 }
                }
            };

            // 条件类型定义
            source.Conditions["all"] = new ConditionTypeDefinition
            {
                Type = "all",
                DisplayName = "全部满足",
                Description = "所有子条件都必须满足",
                Category = "复合",
                IsComposite = true,
                Params = new List<ParameterDefinition>
                {
                    new ParameterDefinition("items", "condition[]", true)
                }
            };

            source.Conditions["any"] = new ConditionTypeDefinition
            {
                Type = "any",
                DisplayName = "任一满足",
                Description = "任一子条件满足即可",
                Category = "复合",
                IsComposite = true,
                Params = new List<ParameterDefinition>
                {
                    new ParameterDefinition("items", "condition[]", true)
                }
            };

            source.Conditions["not"] = new ConditionTypeDefinition
            {
                Type = "not",
                DisplayName = "取反",
                Description = "对子条件取反",
                Category = "复合",
                IsComposite = true,
                Params = new List<ParameterDefinition>
                {
                    new ParameterDefinition("item", "condition", true)
                }
            };

            source.Conditions["arg_eq"] = new ConditionTypeDefinition
            {
                Type = "arg_eq",
                DisplayName = "参数等于",
                Description = "检查参数值是否等于指定值",
                Category = "参数",
                Params = new List<ParameterDefinition>
                {
                    new ParameterDefinition("arg_name", "string", true),
                    new ParameterDefinition("value", "number", true)
                }
            };

            source.Conditions["arg_gt"] = new ConditionTypeDefinition
            {
                Type = "arg_gt",
                DisplayName = "参数大于",
                Description = "检查参数值是否大于指定值",
                Category = "参数",
                Params = new List<ParameterDefinition>
                {
                    new ParameterDefinition("arg_name", "string", true),
                    new ParameterDefinition("value", "number", true)
                }
            };
        }
    }
}
#endif
