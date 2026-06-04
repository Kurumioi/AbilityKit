using System;
using System.Collections.Generic;
using AbilityKit.Core.Common.Log;
using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Demo.Moba.Systems;
using AbilityKit.Triggering.Eventing;
using AbilityKit.Triggering.Runtime.Config.Plans;
using AbilityKit.Triggering.Runtime.Plan;
using AbilityKit.Triggering.Runtime.Plan.Json;

namespace AbilityKit.Demo.Moba.Gameplay.Triggering
{
    public static class MobaGameplayTriggerValidator
    {
        private static readonly HashSet<int> KnownPayloadFieldIds = new HashSet<int>
        {
            StableStringId.Get("payload:" + GameplayTriggerEvents.FrameIndexField),
            StableStringId.Get("payload:" + GameplayTriggerEvents.ElapsedSecondsField),
            StableStringId.Get("payload:" + GameplayTriggerEvents.DeltaSecondsField),
            StableStringId.Get("payload:" + GameplayTriggerEvents.WinTeamIdField),
            MobaBattlePayloadFields.FieldId(MobaBattlePayloadFields.AttackerActorId),
            MobaBattlePayloadFields.FieldId(MobaBattlePayloadFields.TargetActorId),
            MobaBattlePayloadFields.FieldId(MobaBattlePayloadFields.DamageValue),
            MobaBattlePayloadFields.FieldId(MobaBattlePayloadFields.TargetHp),
            MobaBattlePayloadFields.FieldId(MobaBattlePayloadFields.TargetMaxHp),
            MobaBattlePayloadFields.FieldId(MobaBattlePayloadFields.DamageType),
            MobaBattlePayloadFields.FieldId(MobaBattlePayloadFields.CritType),
            MobaBattlePayloadFields.FieldId(MobaBattlePayloadFields.ReasonKind),
            MobaBattlePayloadFields.FieldId(MobaBattlePayloadFields.ReasonParam),
            MobaBattlePayloadFields.FieldId(MobaBattlePayloadFields.UnitActorId),
            MobaBattlePayloadFields.FieldId(MobaBattlePayloadFields.KillerActorId),
        };

        public static bool Validate(TriggerPlanJsonDatabase db, MobaEventSubscriptionRegistry eventRegistry)
        {
            var errors = 0;
            var warnings = 0;

            if (db?.Records == null || db.Records.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < db.Records.Count; i++)
            {
                var record = db.Records[i];
                if (!IsGameplayRecord(record))
                {
                    continue;
                }

                ValidateRecord(record, eventRegistry, $"trigger[{record.TriggerId}:{record.EventName}]", ref errors, ref warnings);
            }

            return Report(errors, warnings);
        }

        public static bool ValidateGameplay(GameplayMO gameplay, TriggerPlanJsonDatabase db, MobaEventSubscriptionRegistry eventRegistry)
        {
            var errors = 0;
            var warnings = 0;

            if (gameplay == null)
            {
                AddError("gameplay", "gameplay config is null", ref errors);
                return Report(errors, warnings);
            }

            if (db == null)
            {
                AddError($"gameplay[{gameplay.Id}]", "trigger database is null", ref errors);
                return Report(errors, warnings);
            }

            var triggerIds = gameplay.TriggerIds;
            if (triggerIds == null || triggerIds.Count == 0)
            {
                AddWarning($"gameplay[{gameplay.Id}]", "gameplay has no trigger ids", ref warnings);
                return Report(errors, warnings);
            }

            for (int i = 0; i < triggerIds.Count; i++)
            {
                var triggerId = triggerIds[i];
                var path = $"gameplay[{gameplay.Id}].triggerIds[{i}]={triggerId}";
                if (!_TryGetRecord(db, triggerId, out var record))
                {
                    AddError(path, "trigger id not found in trigger database", ref errors);
                    continue;
                }

                ValidateRecord(record, eventRegistry, path, ref errors, ref warnings);
            }

            return Report(errors, warnings);
        }

        private static bool _TryGetRecord(TriggerPlanJsonDatabase db, int triggerId, out TriggerPlanJsonDatabase.Record record)
        {
            return db.TryGetRecordByTriggerId(triggerId, out record);
        }

        private static void ValidateRecord(
            TriggerPlanJsonDatabase.Record record,
            MobaEventSubscriptionRegistry eventRegistry,
            string path,
            ref int errors,
            ref int warnings)
        {
            ValidateEvent(record, eventRegistry, path, ref errors, ref warnings);
            ValidatePlan(record.Plan, path, ref errors, ref warnings);
        }

        private static bool Report(int errors, int warnings)
        {
            if (errors > 0 || warnings > 0)
            {
                Log.Warning($"[MobaGameplayTriggerValidator] validation completed. errors={errors}, warnings={warnings}");
            }
            else
            {
                Log.Info("[MobaGameplayTriggerValidator] validation completed. gameplay trigger configs are valid");
            }

            return errors == 0;
        }

        private static bool IsGameplayRecord(TriggerPlanJsonDatabase.Record record)
        {
            return !string.IsNullOrEmpty(record.EventName)
                   && record.EventName.StartsWith("gameplay.", StringComparison.OrdinalIgnoreCase);
        }

        private static void ValidateEvent(
            TriggerPlanJsonDatabase.Record record,
            MobaEventSubscriptionRegistry eventRegistry,
            string path,
            ref int errors,
            ref int warnings)
        {
            if (record.EventId == 0)
            {
                AddError(path, "event id is empty", ref errors);
            }

            if (record.Scope != TriggerPlanScope.Global)
            {
                AddWarning(path, $"gameplay trigger scope is {record.Scope}; lifecycle gameplay events are normally global", ref warnings);
            }

            if (eventRegistry == null || !eventRegistry.TryGetArgsType(record.EventName, out var argsType) || argsType == null)
            {
                AddError(path, $"event '{record.EventName}' is not registered in MobaEventSubscriptionRegistry", ref errors);
                return;
            }

        }

        private static void ValidatePlan(TriggerPlan<object> plan, string path, ref int errors, ref int warnings)
        {
            if (plan.Actions == null || plan.Actions.Length == 0)
            {
                AddError(path, "gameplay trigger has no actions", ref errors);
            }

            ValidatePayloadRef(plan.PredicateArg0, path + ".predicate.arg0", ref errors);
            ValidatePayloadRef(plan.PredicateArg1, path + ".predicate.arg1", ref errors);
            ValidatePredicateExpr(plan.PredicateExpr, path + ".predicate", ref errors);
            ValidateActions(plan.Actions, path, ref errors, ref warnings);
        }

        private static void ValidatePredicateExpr(PredicateExprPlan expr, string path, ref int errors)
        {
            if (expr.Nodes == null)
            {
                return;
            }

            for (int i = 0; i < expr.Nodes.Length; i++)
            {
                var node = expr.Nodes[i];
                if (node.Kind != EBoolExprNodeKind.CompareNumeric)
                {
                    continue;
                }

                ValidatePayloadRef(node.Left, $"{path}.nodes[{i}].left", ref errors);
                ValidatePayloadRef(node.Right, $"{path}.nodes[{i}].right", ref errors);
            }
        }

        private static void ValidateActions(ActionCallPlan[] actions, string path, ref int errors, ref int warnings)
        {
            if (actions == null)
            {
                return;
            }

            for (int i = 0; i < actions.Length; i++)
            {
                var action = actions[i];
                var actionPath = $"{path}.actions[{i}]";

                if (!ActionSchemaRegistry.TryGet(action.Id, out var schema) || schema == null)
                {
                    AddError(actionPath, $"action schema is not registered. actionId={action.Id.Value}", ref errors);
                }
                else
                {
                    var args = ToArgsArray(action);
                    var span = new ReadOnlySpan<KeyValuePair<string, ActionArgValue>>(args);
                    if (!schema.TryValidateArgs(span, out var error))
                    {
                        AddError(actionPath, $"action args invalid. actionId={action.Id.Value}, error={error}", ref errors);
                    }
                }

                ValidatePayloadRef(action.Arg0, actionPath + ".arg0", ref errors);
                ValidatePayloadRef(action.Arg1, actionPath + ".arg1", ref errors);
                if (action.Args != null)
                {
                    foreach (var pair in action.Args)
                    {
                        ValidatePayloadRef(pair.Value.Ref, actionPath + "." + pair.Key, ref errors);
                    }
                }
            }
        }

        private static KeyValuePair<string, ActionArgValue>[] ToArgsArray(ActionCallPlan action)
        {
            if (action.Args != null && action.Args.Count > 0)
            {
                var args = new KeyValuePair<string, ActionArgValue>[action.Args.Count];
                var index = 0;
                foreach (var pair in action.Args)
                {
                    args[index++] = pair;
                }

                return args;
            }

            if (action.Arity <= 0)
            {
                return Array.Empty<KeyValuePair<string, ActionArgValue>>();
            }

            var positional = new List<KeyValuePair<string, ActionArgValue>>(action.Arity);
            positional.Add(new KeyValuePair<string, ActionArgValue>("arg0", ActionArgValue.Of(action.Arg0, "arg0")));
            if (action.Arity > 1)
            {
                positional.Add(new KeyValuePair<string, ActionArgValue>("arg1", ActionArgValue.Of(action.Arg1, "arg1")));
            }

            return positional.ToArray();
        }

        private static void ValidatePayloadRef(NumericValueRef valueRef, string path, ref int errors)
        {
            if (valueRef.Kind != ENumericValueRefKind.PayloadField)
            {
                return;
            }

            if (valueRef.FieldId == 0 || !KnownPayloadFieldIds.Contains(valueRef.FieldId))
            {
                AddError(path, $"unknown gameplay/battle payload field id={valueRef.FieldId}", ref errors);
            }
        }

        private static void AddError(string path, string message, ref int errors)
        {
            errors++;
            Log.Error($"[MobaGameplayTriggerValidator] {path}: {message}");
        }

        private static void AddWarning(string path, string message, ref int warnings)
        {
            warnings++;
            Log.Warning($"[MobaGameplayTriggerValidator] {path}: {message}");
        }
    }
}
