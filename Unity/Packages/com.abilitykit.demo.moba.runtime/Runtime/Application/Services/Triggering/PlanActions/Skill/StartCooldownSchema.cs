using System;
using System.Collections.Generic;
using AbilityKit.Ability.World.DI;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan;

namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    public sealed class StartCooldownSchema : MobaPlanActionSchemaBase<StartCooldownArgs>
    {
        public static readonly StartCooldownSchema Instance = new StartCooldownSchema();

        protected override string ActionName => Systems.TriggeringConstants.Actions.StartCooldown;

        public override StartCooldownArgs ParseArgs(Dictionary<string, ActionArgValue> namedArgs, ExecCtx<IWorldResolver> ctx)
        {
            var skillId = ReadInt(namedArgs, ctx, 0, "skill_id", "skillid");
            var skillSlot = ReadInt(namedArgs, ctx, 0, "skill_slot", "skillslot", "slot");
            var cooldownMs = ReadInt(namedArgs, ctx, 0, "cooldown_ms", "cooldownms", "duration_ms", "durationms");

            if (skillId <= 0)
            {
                skillId = ReadSkillPayloadInt(ctx, SkillRulePayloadFields.SkillId);
            }

            if (skillSlot <= 0)
            {
                skillSlot = ReadSkillPayloadInt(ctx, SkillRulePayloadFields.SkillSlot);
            }

            return new StartCooldownArgs(skillId, skillSlot, cooldownMs);
        }

        public override bool TryValidateArgs(ReadOnlySpan<KeyValuePair<string, ActionArgValue>> args, out string error)
        {
            if (!RequireAny(args, "cooldown_ms", out error, "cooldown_ms", "cooldownms", "duration_ms", "durationms")) return false;
            error = null;
            return true;
        }

        private static int ReadSkillPayloadInt(ExecCtx<IWorldResolver> ctx, string fieldName)
        {
            var fieldId = SkillRulePayloadFields.FieldId(fieldName);
            return TryReadCurrentPayloadNumber(ctx, fieldId, out var value) ? (int)Math.Round(value) : 0;
        }
    }
}
