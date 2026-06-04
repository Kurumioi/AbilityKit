using System;
using System.Collections.Generic;
using AbilityKit.Ability.World.DI;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan;
using AbilityKit.Demo.Moba.Systems;


namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    /// <summary>
    /// take_damage Action �?Schema 瀹氫�?
    /// </summary>
    public sealed class TakeDamageSchema : MobaPlanActionSchemaBase<TakeDamageArgs>
    {
        public static readonly TakeDamageSchema Instance = new TakeDamageSchema();

        protected override string ActionName => TriggeringConstants.Actions.TakeDamage;

        public override TakeDamageArgs ParseArgs(Dictionary<string, ActionArgValue> namedArgs, ExecCtx<IWorldResolver> ctx)
        {
            var rate = ReadFloat(namedArgs, ctx, 1f, "rate", "damage_rate", "damagerate");
            var reasonParam = ReadInt(namedArgs, ctx, 0, "reason_param", "reasonparam");

            return new TakeDamageArgs(rate, reasonParam);
        }

        public override bool TryValidateArgs(ReadOnlySpan<KeyValuePair<string, ActionArgValue>> args, out string error)
        {
            error = null;
            return true;
        }
    }
}
