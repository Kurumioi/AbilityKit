using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Ability.World.DI;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan;
using AbilityKit.Core.Common.Log;
using AbilityKit.Demo.Moba.Systems;


namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    [PlanActionModule(order: 20)]
    public sealed class AddBuffPlanActionModule : MobaPlanActionModuleBase<AddBuffArgs, AddBuffPlanActionModule>
    {
        protected override IActionSchema<AddBuffArgs, IWorldResolver> Schema => AddBuffSchema.Instance;

        protected override void Execute(object triggerArgs, AddBuffArgs args, ExecCtx<IWorldResolver> ctx)
        {
            if (!ctx.Context.TryResolve<MobaBuffService>(out var buffSvc) || buffSvc == null)
            {
                Log.Warning("[Plan] add_buff cannot resolve MobaBuffService");
                return;
            }

            if (args.BuffIds == null || args.BuffIds.Length == 0)
            {
                Log.Warning("[Plan] add_buff requires buffIds");
                return;
            }

            var input = MobaPlanActionInputResolver.Resolve(triggerArgs, ctx);
            var targetActorId = args.TargetActorId > 0 ? args.TargetActorId : input.TargetActorId;
            if (targetActorId <= 0)
            {
                Log.Warning("[Plan] add_buff requires valid target actorId");
                return;
            }

            var sourceActorId = input.CasterActorId;
            var executionContext = input.ExecutionContext;
            var traceScope = input.TraceScope;
            var origin = MobaActionOriginBuilder.Build(
                in executionContext,
                in traceScope,
                sourceActorId,
                targetActorId,
                MobaTraceKind.EffectExecution,
                0);

            for (int i = 0; i < args.BuffIds.Length; i++)
            {
                var buffId = args.BuffIds[i];
                if (buffId <= 0) continue;
                var buffOrigin = BuffOriginContext.FromOrigin(in origin);
                buffSvc.ApplyBuffImmediate(targetActorId, buffId, sourceActorId, durationOverrideMs: 0, buffOrigin);
            }
        }
    }
}
