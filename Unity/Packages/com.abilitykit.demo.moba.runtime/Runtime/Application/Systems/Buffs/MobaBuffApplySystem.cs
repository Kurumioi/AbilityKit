using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Triggering.Runtime;
using AbilityKit.Ability.World;
using AbilityKit.Ability.World.DI;
using AbilityKit.Core.Continuous;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.Services;

namespace AbilityKit.Demo.Moba.Systems.Buffs
{
    [WorldSystem(order: MobaSystemOrder.BuffsApply, Phase = WorldSystemPhase.Execute)]
    public sealed class MobaBuffApplySystem : WorldSystemBase
    {
        private BuffLifecycleExecutor _lifecycle;
        private global::Entitas.IGroup<global::ActorEntity> _group;

        public MobaBuffApplySystem(global::Entitas.IContexts contexts, IWorldResolver services)
            : base(contexts, services)
        {
        }

        protected override void OnInit()
        {
            Services.TryResolve(out MobaConfigDatabase configs);
            Services.TryResolve(out AbilityKit.Triggering.Eventing.IEventBus eventBus);
            Services.TryResolve(out ITriggerActionRunner actionRunner);
            Services.TryResolve(out MobaTraceRegistry trace);
            Services.TryResolve(out MobaEffectExecutionService effects);
            Services.TryResolve(out IMobaEffectiveTagQueryService tags);
            Services.TryResolve(out IMobaContinuousTagTemplateRegistry tagTemplates);
            Services.TryResolve(out IFrameTime frameTime);
            Services.TryResolve(out IContinuousManager continuous);
            Services.TryResolve(out MobaActorLookupService actors);
            Services.TryResolve(out MobaSkillCastRuntimeService skillRuntimes);

            var repo = new BuffRepository();
            var ctx = new BuffContextService(trace, actionRunner, frameTime);
            var events = new BuffEventPublisher(eventBus);
            var stageEffects = new BuffStageEffectExecutor(effects);
            var stacking = new BuffStackingPolicyApplier();
            _lifecycle = new BuffLifecycleExecutor(configs, actors, tags, tagTemplates, repo, ctx, events, stageEffects, stacking, continuous, skillRuntimes);
            _group = Contexts.Actor().GetGroup(ActorMatcher.AllOf(ActorComponentsLookup.ActorId, ActorComponentsLookup.ApplyBuffRequest));
        }

        protected override void OnExecute()
        {
            var entities = _group.GetEntities();
            if (entities == null || entities.Length == 0) return;

            for (int i = 0; i < entities.Length; i++)
            {
                var e = entities[i];
                if (e == null || !e.hasActorId || !e.hasApplyBuffRequest) continue;

                var req = e.applyBuffRequest;
                e.RemoveApplyBuffRequest();

                _lifecycle?.Apply(new BuffApplyRequest
                {
                    TargetActorId = e.actorId.Value,
                    BuffId = req.BuffId,
                    SourceActorId = req.SourceId,
                    DurationOverrideMs = req.DurationOverrideMs,
                    Origin = BuffOriginContext.FromActors(req.ParentContextId, req.OriginSourceActorId, req.OriginTargetActorId),
                });
            }
        }
    }
}
