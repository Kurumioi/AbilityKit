using System.Collections.Generic;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Services.Search;
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

            var coreInput = MobaPlanActionInputResolver.Resolve(triggerArgs, ctx);
            var effectInput = new MobaEffectActionInput(in coreInput);
            var sourceActorId = effectInput.CasterActorId;

            if (args.QueryTemplateId > 0)
            {
                if (!ctx.Context.TryResolve<SearchTargetService>(out var search) || search == null)
                {
                    Log.Warning($"[Plan] add_buff cannot resolve SearchTargetService. queryTemplateId={args.QueryTemplateId}");
                    return;
                }

                var explicitTargetActorId = args.TargetActorId > 0 ? args.TargetActorId : effectInput.TargetActorId;
                var targets = new List<int>(8);
                var aimPosition = coreInput.AimPosition;
                if (!search.TrySearchActorIds(args.QueryTemplateId, sourceActorId, in aimPosition, explicitTargetActorId, targets))
                {
                    return;
                }

                for (int i = 0; i < targets.Count; i++)
                {
                    ApplyBuffs(buffSvc, args, effectInput, sourceActorId, targets[i]);
                }
                return;
            }

            var targetActorId = args.TargetActorId > 0 ? args.TargetActorId : effectInput.TargetActorId;
            if (targetActorId <= 0)
            {
                Log.Warning("[Plan] add_buff requires valid target actorId");
                return;
            }

            ApplyBuffs(buffSvc, args, effectInput, sourceActorId, targetActorId);
        }

        private static void ApplyBuffs(MobaBuffService buffSvc, AddBuffArgs args, MobaEffectActionInput input, int sourceActorId, int targetActorId)
        {
            if (targetActorId <= 0) return;

            var origin = input.BuildOrigin(sourceActorId, targetActorId, MobaTraceKind.EffectExecution, 0);
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
