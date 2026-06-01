using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Triggering.Runtime;
using AbilityKit.Ability.World;
using AbilityKit.Ability.World.DI;
using AbilityKit.Core.Continuous;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.GameplayTags;

namespace AbilityKit.Demo.Moba.Systems.Buffs
{
    [WorldSystem(order: MobaSystemOrder.BuffsRemove, Phase = WorldSystemPhase.Execute)]
    public sealed class MobaBuffRemoveSystem : WorldSystemBase
    {
        private BuffLifecycleExecutor _lifecycle;
        private global::Entitas.IGroup<global::ActorEntity> _group;

        public MobaBuffRemoveSystem(global::Entitas.IContexts contexts, IWorldResolver services)
            : base(contexts, services)
        {
        }

        protected override void OnInit()
        {
            Services.TryResolve(out MobaConfigDatabase configs);
            Services.TryResolve(out AbilityKit.Triggering.Eventing.IEventBus eventBus);
            Services.TryResolve(out ITriggerActionRunner actionRunner);
            Services.TryResolve(out MobaPeriodicEffectService ongoing);
            Services.TryResolve(out MobaTraceRegistry trace);
            Services.TryResolve(out MobaEffectExecutionService effects);
            Services.TryResolve(out IGameplayTagService tags);
            Services.TryResolve(out IMobaContinuousTagTemplateRegistry tagTemplates);
            Services.TryResolve(out IFrameTime frameTime);
            Services.TryResolve(out IContinuousManager continuous);
            Services.TryResolve(out MobaActorLookupService actors);
            Services.TryResolve(out MobaSkillCastRuntimeService skillRuntimes);

            var repo = new BuffRepository();
            var ctx = new BuffContextService(trace, actionRunner, frameTime);
            var events = new BuffEventPublisher(eventBus);
            var periodicBinder = new BuffPeriodicEffectBinder(ongoing, actionRunner);
            var stageEffects = new BuffStageEffectExecutor(effects);
            var stacking = new BuffStackingPolicyApplier();
            _lifecycle = new BuffLifecycleExecutor(configs, actors, ongoing, tags, tagTemplates, repo, ctx, events, periodicBinder, stageEffects, stacking, continuous, skillRuntimes);
            _group = Contexts.Actor().GetGroup(ActorMatcher.AllOf(ActorComponentsLookup.ActorId, ActorComponentsLookup.RemoveBuffRequest));
        }

        protected override void OnExecute()
        {
            var entities = _group.GetEntities();
            if (entities == null || entities.Length == 0) return;

            for (int i = 0; i < entities.Length; i++)
            {
                var e = entities[i];
                if (e == null || !e.hasActorId || !e.hasRemoveBuffRequest) continue;

                var req = e.removeBuffRequest;
                e.RemoveRemoveBuffRequest();
                _lifecycle?.Remove(new BuffRemoveRequest
                {
                    TargetActorId = e.actorId.Value,
                    BuffId = req.BuffId,
                    SourceActorId = req.SourceId,
                    Reason = req.Reason,
                });
            }
        }
    }
}
