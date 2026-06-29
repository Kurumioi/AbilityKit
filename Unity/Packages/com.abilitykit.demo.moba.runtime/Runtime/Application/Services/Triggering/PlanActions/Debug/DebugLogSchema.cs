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
    /// debug_log Action 参数 Schema 定义。
    /// </summary>
    public sealed class DebugLogSchema : MobaPlanActionSchemaBase<DebugLogArgs>
    {
        public static readonly DebugLogSchema Instance = new DebugLogSchema();

        protected override string ActionName => TriggeringConstants.Actions.DebugLog;

        public override DebugLogArgs ParseArgs(Dictionary<string, ActionArgValue> namedArgs, ExecCtx<IWorldResolver> ctx)
        {
            var msgId = ReadInt(namedArgs, ctx, 0, "msg_id", "msgid", "message", "msg", "id");
            var dump = ReadBool(namedArgs, ctx, false, "dump", "dump_args", "is_dump");

            return new DebugLogArgs(msgId, dump);
        }

        public override bool TryValidateArgs(ReadOnlySpan<KeyValuePair<string, ActionArgValue>> args, out string error)
        {
            error = null;
            return true;
        }
    }
}
