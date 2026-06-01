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

            var origin = triggerArgs.TryResolveOrigin(out var payloadOrigin)
                ? payloadOrigin.WithActors(sourceActorId, targetActorId)
                : MobaGameplayOrigin.FromLegacy(sourceActorId, targetActorId, MobaTraceKind.EffectExecution, 0, 0);

            if (triggerArgs is IMobaTriggerInvocationContext invocation && !origin.IsValid)
            {
                origin = MobaGameplayOrigin.FromLegacy(sourceActorId, targetActorId, MobaTraceKind.EffectExecution, 0, invocation.SourceContextId);
            }

            if (triggerArgs is IMobaTriggerSkillRuntimeContext skillRuntimeProvider && !origin.SkillRuntimeHandle.IsValid)
            {
                skillRuntimeProvider.TryGetSkillRuntimeHandle(out var skillRuntimeHandle);
                if (skillRuntimeHandle.IsValid) origin = origin.WithSkillRuntime(in skillRuntimeHandle);
            }

            if (triggerArgs != null && triggerArgs is System.Collections.Generic.IDictionary<string, object> argsDict)
            {
                var parentContextId = origin.EffectiveParentContextId;
                if (argsDict.TryGetValue("effect.sourceContextId", out var ctxIdObj) && ctxIdObj != null)
                {
                    if (ctxIdObj is long l) parentContextId = l;
                    else if (ctxIdObj is int i) parentContextId = i;
                }

                var skillRuntimeHandle = origin.SkillRuntimeHandle;
                if (!skillRuntimeHandle.IsValid && argsDict.TryGetValue(AbilityContextKeys.SkillRuntimeHandle.ToKeyString(), out var runtimeObj) && runtimeObj is MobaSkillCastRuntimeHandle handle)
                {
                    skillRuntimeHandle = handle;
                }

                origin = MobaGameplayOriginBuilder.Create()
                    .FromOrigin(in origin)
                    .WithActors(sourceActorId, targetActorId)
                    .WithParentContext(parentContextId)
                    .WithSkillRuntime(in skillRuntimeHandle)
                    .Build();
            }

            if (ctx.Context.TryResolve<MobaEffectExecutionService>(out var effects) && effects != null && effects.TryGetCurrentTraceScope(out var traceScope))
            {
                origin = MobaGameplayOriginBuilder.Create()
                    .FromOrigin(in origin)
                    .WithActors(sourceActorId, targetActorId)
                    .WithImmediate(MobaTraceKind.EffectExecution, traceScope.EffectConfigId, traceScope.EffectContextId)
                    .WithRootContext(origin.EffectiveRootContextId)
                    .WithOwnerContext(origin.OwnerContextId)
                    .Build();
            }

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
