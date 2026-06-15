using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Ability.World.DI;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan;
using AbilityKit.Core.Mathematics;


namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    [PlanActionModule(order: MobaPlanActionModuleOrders.SpawnSummon)]
    public sealed class SpawnSummonPlanActionModule : MobaPlanActionModuleBase<SpawnSummonArgs, SpawnSummonPlanActionModule>
    {
        protected override IActionSchema<SpawnSummonArgs, IWorldResolver> Schema => SpawnSummonSchema.Instance;

        protected override void Execute(object triggerArgs, SpawnSummonArgs args, ExecCtx<IWorldResolver> ctx)
        {
            if (!TryResolveRequired(ctx, out MobaSummonService summonSvc))
            {
                return;
            }

            var input = MobaPlanActionInputResolver.ResolveSummon(triggerArgs, ctx);
            if (!input.HasCasterActor)
            {
                LogRejected("requires caster actor");
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
                LogRejected("requires summon_id > 0");
                return;
            }
            var positionMode = (SpawnSummonPositionMode)args.PositionMode;
            if (!input.TryResolveSpawnPosition(positionMode, out var spawnPos))
            {
                LogRejected($"cannot resolve spawn position. mode={positionMode}");
                return;
            }

            var forward = input.HasAimDirection ? input.AimDirection : Vec3.Forward;
            var sourceContext = input.CreateSourceContext(casterActorId, summonId);
            if (summonSvc.TrySummon(casterActorId, summonId, in spawnPos, in forward, in sourceContext))
            {
                LogApplied($"caster={casterActorId} summonId={summonId}");
            }
        }
    }
}
