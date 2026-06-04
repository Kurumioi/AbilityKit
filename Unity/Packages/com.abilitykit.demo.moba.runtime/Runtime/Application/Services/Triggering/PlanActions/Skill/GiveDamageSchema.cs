using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba;
using AbilityKit.Ability.World.DI;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan;
using AbilityKit.Demo.Moba.Systems;


namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    /// <summary>
    /// give_damage Action �?Schema 鐎规矮绠?
    /// 鐎圭偟骞?IActionSchema閿涘本褰佹笟娑樺棘閺佹媽袙閺嬫劕鎷版宀冪槈闁槒绶?
    /// </summary>
    public sealed class GiveDamageSchema : MobaPlanActionSchemaBase<GiveDamageArgs>
    {
        public static readonly GiveDamageSchema Instance = new GiveDamageSchema();

        protected override string ActionName => TriggeringConstants.Actions.GiveDamage;

        public override GiveDamageArgs ParseArgs(Dictionary<string, ActionArgValue> namedArgs, ExecCtx<IWorldResolver> ctx)
        {
            var damageValue = ReadFloat(namedArgs, ctx, 0f, "damage_value", "value", "damagevalue");
            var reasonParam = ReadInt(namedArgs, ctx, 0, "reason_param", "reasonparam");
            var damageType = ReadEnum(namedArgs, ctx, DamageType.Physical, "damage_type", "damagetype");
            var queryTemplateId = ReadInt(namedArgs, ctx, 0,
                "query_template_id", "querytemplateid", "target_query_id", "targetqueryid", "target_query_template_id", "targetquerytemplateid");

            return new GiveDamageArgs(damageValue, reasonParam, damageType, queryTemplateId);
        }

        public override bool TryValidateArgs(ReadOnlySpan<KeyValuePair<string, ActionArgValue>> args, out string error)
        {
            return RequireAny(args, "damage_value", out error, "damage_value", "value", "damagevalue");
        }
    }
}
