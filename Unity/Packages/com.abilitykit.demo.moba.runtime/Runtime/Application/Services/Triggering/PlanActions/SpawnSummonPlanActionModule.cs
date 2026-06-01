using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Ability.World.DI;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan;
using AbilityKit.Core.Common.Log;
using AbilityKit.Core.Math;
using AbilityKit.Demo.Moba.Systems;


namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    [PlanActionModule(order: 30)]
    public sealed class SpawnSummonPlanActionModule : MobaPlanActionModuleBase<SpawnSummonArgs, SpawnSummonPlanActionModule>
    {
        protected override IActionSchema<SpawnSummonArgs, IWorldResolver> Schema => SpawnSummonSchema.Instance;

        protected override void Execute(object triggerArgs, SpawnSummonArgs args, ExecCtx<IWorldResolver> ctx)
        {
            if (!ctx.Context.TryResolve<MobaSummonService>(out var summonSvc) || summonSvc == null)
            {
                Log.Warning("[Plan] spawn_summon cannot resolve MobaSummonService");
                return;
            }

            if (args.SummonId <= 0)
            {
                Log.Warning("[Plan] spawn_summon requires summon_id > 0");
                return;
            }

            var casterActorId = 0;
            PlanContextValueResolver.TryGetCasterActorId(triggerArgs, out casterActorId);

            Vec3 spawnPos = Vec3.Zero;
            Vec3 forward = Vec3.Forward;

            var positionMode = (SpawnSummonPositionMode)args.PositionMode;
            switch (positionMode)
            {
                case SpawnSummonPositionMode.Caster:
                    if (casterActorId > 0 && ctx.Context.TryResolve<MobaActorLookupService>(out var actors) && actors != null)
                    {
                        if (actors.TryGetActorEntity(casterActorId, out var caster) && caster != null && caster.hasTransform)
                        {
                            spawnPos = caster.transform.Value.Position;
                        }
                    }
                    break;

                case SpawnSummonPositionMode.Target:
                    int targetActorId = 0;
                    if (PlanContextValueResolver.TryGetTargetActorId(triggerArgs, out targetActorId) && targetActorId > 0)
                    {
                        if (ctx.Context.TryResolve<MobaActorLookupService>(out var lookup) && lookup != null)
                        {
                            if (lookup.TryGetActorEntity(targetActorId, out var target) && target != null && target.hasTransform)
                            {
                                spawnPos = target.transform.Value.Position;
                            }
                        }
                    }
                    break;

                case SpawnSummonPositionMode.AimPos:
                    if (PlanContextValueResolver.TryGetAimPos(triggerArgs, out var aimPos))
                    {
                        spawnPos = new Vec3(aimPos.X, 0, aimPos.Y);
                    }
                    break;
            }

            summonSvc.TrySummon(casterActorId, args.SummonId, in spawnPos, in forward);
        }
    }
}
