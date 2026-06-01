using System;
using System.Collections.Generic;
using AbilityKit.Ability.World.DI;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan;
using AbilityKit.Triggering.Runtime.Plan.Json;
using AbilityKit.Demo.Moba.Systems;


namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    /// <summary>
    /// debug_log Action Èê?Schema ÁÄπÊ∞´ÁÆ?
    /// </summary>
    public sealed class DebugLogSchema : MobaPlanActionSchemaBase<DebugLogArgs>
    {
        public static readonly DebugLogSchema Instance = new DebugLogSchema();

        protected override string ActionName => TriggeringConstants.Actions.DebugLog;

        public override DebugLogArgs ParseArgs(Dictionary<string, ActionArgValue> namedArgs, ExecCtx<IWorldResolver> ctx)
        {
            int msgId = 0;
            bool dump = false;

            if (namedArgs == null || namedArgs.Count == 0)
                return DebugLogArgs.Default;

            foreach (var kv in namedArgs)
            {
                var rawValue = kv.Value.Ref.Kind == ENumericValueRefKind.Const
                    ? kv.Value.Ref.ConstValue
                    : ActionSchemaRegistry.ResolveNumericRef(kv.Value.Ref, ctx);

                switch (kv.Key.ToLowerInvariant())
                {
                    case "msg_id":
                    case "msgid":
                    case "id":
                        msgId = (int)System.Math.Round(rawValue);
                        break;
                    case "dump":
                    case "is_dump":
                        dump = rawValue >= 0.5;
                        break;
                }
            }

            return new DebugLogArgs(msgId, dump);
        }

        public override bool TryValidateArgs(ReadOnlySpan<KeyValuePair<string, ActionArgValue>> args, out string error)
        {
            error = null;
            return true;
        }
    }
}
