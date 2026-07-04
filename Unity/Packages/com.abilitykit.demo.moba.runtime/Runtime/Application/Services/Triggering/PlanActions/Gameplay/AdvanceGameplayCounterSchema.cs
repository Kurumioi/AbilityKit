using System;
using System.Collections.Generic;
using AbilityKit.Ability.World.DI;
using AbilityKit.Demo.Moba.Systems;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan;

namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    public sealed class AdvanceGameplayCounterSchema : MobaPlanActionSchemaBase<AdvanceGameplayCounterArgs>
    {
        public static readonly AdvanceGameplayCounterSchema Instance = new AdvanceGameplayCounterSchema();

        protected override string ActionName => TriggeringConstants.Actions.AdvanceGameplayCounter;

        public override AdvanceGameplayCounterArgs ParseArgs(Dictionary<string, ActionArgValue> namedArgs, ExecCtx<IWorldResolver> ctx)
        {
            var keyId = ReadInt(namedArgs, ctx, 0, "key_id", "keyid", "id");
            var scopePayloadFieldId = ReadInt(namedArgs, ctx, 0, "scope_payload_field_id", "scopePayloadFieldId", "scope_field_id", "field_id");
            var threshold = ReadFloat(namedArgs, ctx, 0f, "threshold", "limit", "count");
            var delta = ReadFloat(namedArgs, ctx, 1f, "delta", "step", "amount");
            var resetValue = ReadFloat(namedArgs, ctx, 0f, "reset_value", "resetValue", "reset");
            var triggerId = ReadInt(namedArgs, ctx, 0, "trigger_id", "triggerId", "on_threshold_trigger_id", "onThresholdTriggerId");

            return new AdvanceGameplayCounterArgs(keyId, scopePayloadFieldId, threshold, delta, resetValue, triggerId);
        }

        public override bool TryValidateArgs(ReadOnlySpan<KeyValuePair<string, ActionArgValue>> args, out string error)
        {
            if (!RequireAny(args, "key_id", out error, "key_id", "keyid", "id")) return false;
            if (!RequireAny(args, "scope_payload_field_id", out error, "scope_payload_field_id", "scopePayloadFieldId", "scope_field_id", "field_id")) return false;
            if (!RequireAny(args, "threshold", out error, "threshold", "limit", "count")) return false;
            if (!RequireAny(args, "trigger_id", out error, "trigger_id", "triggerId", "on_threshold_trigger_id", "onThresholdTriggerId")) return false;
            return true;
        }
    }
}
