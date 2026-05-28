using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Ability.World.DI;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan;
using AbilityKit.Core.Common.Log;

namespace AbilityKit.Demo.Moba.Systems
{
    [PlanActionModule(order: 20)]
    public sealed class AddBuffPlanActionModule : NamedArgsPlanActionModuleBase<AddBuffArgs, IWorldResolver, AddBuffPlanActionModule>
    {
        protected override ActionId ActionId => TriggeringConstants.AddBuffId;
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

            var targetActorId = args.TargetActorId;
            if (targetActorId <= 0)
            {
                if (!PlanContextValueResolver.TryGetTargetActorId(triggerArgs, out targetActorId) || targetActorId <= 0)
                {
                    Log.Warning("[Plan] add_buff requires valid target actorId");
                    return;
                }
            }

            PlanContextValueResolver.TryGetCasterActorId(triggerArgs, out var sourceActorId);

            var target = buffSvc.TryGetActorEntity(targetActorId);
            if (target == null)
            {
                Log.Warning($"[Plan] add_buff cannot resolve target ActorEntity: actorId={targetActorId}");
                return;
            }

            long parentContextId = 0;
            if (triggerArgs != null && triggerArgs is System.Collections.Generic.IDictionary<string, object> argsDict)
            {
                if (argsDict.TryGetValue("effect.sourceContextId", out var ctxIdObj) && ctxIdObj != null)
                {
                    if (ctxIdObj is long l) parentContextId = l;
                    else if (ctxIdObj is int i) parentContextId = i;
                }
            }

            for (int i = 0; i < args.BuffIds.Length; i++)
            {
                var buffId = args.BuffIds[i];
                if (buffId <= 0) continue;
                buffSvc.ApplyBuffImmediate(target, buffId, sourceActorId, durationOverrideMs: 0,
                    originSource: null, originTarget: null, parentContextId: parentContextId);
            }
        }
    }
}
