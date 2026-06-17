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
    /// dash Action 参数 Schema 定义。
    /// </summary>
    public sealed class DashSchema : MobaPlanActionSchemaBase<DashArgs>
    {
        public static readonly DashSchema Instance = new DashSchema();

        protected override string ActionName => TriggeringConstants.Actions.Dash;

        public override DashArgs ParseArgs(Dictionary<string, ActionArgValue> namedArgs, ExecCtx<IWorldResolver> ctx)
        {
            var speed = ReadFloat(namedArgs, ctx, 0f, "speed");
            var durationMs = ReadFloat(namedArgs, ctx, 0f, "duration_ms", "duration", "durationms");
            var directionMode = ReadInt(namedArgs, ctx, 0, "direction_mode", "directionmode", "dir_mode");
            var priority = ReadInt(namedArgs, ctx, 10, "priority");
            var applyToCaster = ReadBool(namedArgs, ctx, true, "apply_to_caster", "applytocaster");

            return new DashArgs(speed, durationMs, directionMode, priority, applyToCaster);
        }

        public override bool TryValidateArgs(ReadOnlySpan<KeyValuePair<string, ActionArgValue>> args, out string error)
        {
            error = null;
            return true;
        }
    }
}
