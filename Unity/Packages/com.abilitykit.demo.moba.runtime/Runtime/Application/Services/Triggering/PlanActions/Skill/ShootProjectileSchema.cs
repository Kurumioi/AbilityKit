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
    /// shoot_projectile Action 参数 Schema 定义。
    /// </summary>
    public sealed class ShootProjectileSchema : MobaPlanActionSchemaBase<ShootProjectileArgs>
    {
        public static readonly ShootProjectileSchema Instance = new ShootProjectileSchema();

        protected override string ActionName => TriggeringConstants.Actions.ShootProjectile;

        public override ShootProjectileArgs ParseArgs(Dictionary<string, ActionArgValue> namedArgs, ExecCtx<IWorldResolver> ctx)
        {
            var launcherId = ReadInt(namedArgs, ctx, 0, "launcher_id", "launcherid", "launcher");
            var projectileId = ReadInt(namedArgs, ctx, 0, "projectile_id", "projectileid", "projectile");

            return new ShootProjectileArgs(launcherId, projectileId);
        }

        public override bool TryValidateArgs(ReadOnlySpan<KeyValuePair<string, ActionArgValue>> args, out string error)
        {
            if (!RequireAny(args, "launcher_id", out error, "launcher_id", "launcherid", "launcher")) return false;
            if (!RequireAny(args, "projectile_id", out error, "projectile_id", "projectileid", "projectile")) return false;
            error = null;
            return true;
        }
    }
}
