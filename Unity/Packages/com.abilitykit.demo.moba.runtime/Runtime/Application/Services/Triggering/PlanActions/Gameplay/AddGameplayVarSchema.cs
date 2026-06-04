using System;
using System.Collections.Generic;
using AbilityKit.Ability.World.DI;
using AbilityKit.Demo.Moba.Systems;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan;

namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    public sealed class AddGameplayVarSchema : MobaPlanActionSchemaBase<AddGameplayVarArgs>
    {
        public static readonly AddGameplayVarSchema Instance = new AddGameplayVarSchema();

        protected override string ActionName => TriggeringConstants.Actions.AddGameplayVar;

        public override AddGameplayVarArgs ParseArgs(Dictionary<string, ActionArgValue> namedArgs, ExecCtx<IWorldResolver> ctx)
        {
            var keyId = ReadInt(namedArgs, ctx, 0, "key_id", "keyid", "id");
            var delta = ReadFloat(namedArgs, ctx, 0f, "delta", "value", "amount");
            return new AddGameplayVarArgs(keyId, delta);
        }

        public override bool TryValidateArgs(ReadOnlySpan<KeyValuePair<string, ActionArgValue>> args, out string error)
        {
            return RequireAny(args, "key_id", out error, "key_id", "keyid", "id");
        }
    }
}
