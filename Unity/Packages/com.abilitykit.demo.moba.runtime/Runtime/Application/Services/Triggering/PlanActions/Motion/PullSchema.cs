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
    /// pull Action �?Schema 瀹氫�?
    /// </summary>
    public sealed class PullSchema : MobaPlanActionSchemaBase<PullArgs>
    {
        public static readonly PullSchema Instance = new PullSchema();

        protected override string ActionName => TriggeringConstants.Actions.Pull;

        public override PullArgs ParseArgs(Dictionary<string, ActionArgValue> namedArgs, ExecCtx<IWorldResolver> ctx)
        {
            var speed = ReadFloat(namedArgs, ctx, 0f, "speed");
            var durationMs = ReadFloat(namedArgs, ctx, 0f, "duration_ms", "duration", "durationms");
            var directionMode = ReadInt(namedArgs, ctx, 0, "direction_mode", "directionmode", "dir_mode");
            var targetDistance = ReadFloat(namedArgs, ctx, 0f, "target_distance", "targetdistance");
            var priority = ReadInt(namedArgs, ctx, 12, "priority");

            return new PullArgs(speed, durationMs, directionMode, targetDistance, priority);
        }

        public override bool TryValidateArgs(ReadOnlySpan<KeyValuePair<string, ActionArgValue>> args, out string error)
        {
            error = null;
            return true;
        }
    }
}
