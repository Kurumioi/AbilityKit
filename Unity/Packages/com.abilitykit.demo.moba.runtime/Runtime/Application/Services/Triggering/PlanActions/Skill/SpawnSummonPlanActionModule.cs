using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Ability.World.DI;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan;
using AbilityKit.Core.Common.Log;
using AbilityKit.Core.Math;


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

            var input = MobaPlanActionInputResolver.ResolveSummon(triggerArgs, ctx);
            if (!input.HasCasterActor)
            {
                Log.Warning("[Plan] spawn_summon requires caster actor");
                return;
            }

            var casterActorId = input.CasterActorId;
            var summonId = args.SummonId;
            if (ctx.Context.TryResolve<MobaSkillParamModifierService>(out var paramResolver) && paramResolver != null)
            {
                summonId = paramResolver.Summon.ResolveSummonId(casterActorId, summonId);
            }

            if (summonId <= 0)
            {
                Log.Warning("[Plan] spawn_summon requires summon_id > 0");
                return;
            }
            var positionMode = (SpawnSummonPositionMode)args.PositionMode;
            if (!input.TryResolveSpawnPosition(positionMode, out var spawnPos))
            {
                Log.Warning($"[Plan] spawn_summon cannot resolve spawn position. mode={positionMode}");
                return;
            }

            var forward = input.HasAimDirection ? input.AimDirection : Vec3.Forward;
            var sourceContext = input.CreateSourceContext(casterActorId, summonId);
            summonSvc.TrySummon(casterActorId, summonId, in spawnPos, in forward, in sourceContext);
        }
    }
}
