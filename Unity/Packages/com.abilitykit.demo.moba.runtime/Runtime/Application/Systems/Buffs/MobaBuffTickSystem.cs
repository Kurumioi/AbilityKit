using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Triggering;
using AbilityKit.Ability.Triggering.Runtime;
using AbilityKit.Ability.World;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Core.Continuous;
using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.Components;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.GameplayTags;
using AbilityKit.Trace;

namespace AbilityKit.Demo.Moba.Systems.Buffs
{
    [WorldSystem(order: MobaSystemOrder.BuffsTick, Phase = WorldSystemPhase.Execute)]
    public sealed class MobaBuffTickSystem : WorldSystemBase
    {
        private MobaConfigDatabase _configs;
        private IWorldClock _clock;
        private IGameplayTagService _tags;
        private IMobaContinuousTagTemplateRegistry _tagTemplates;
        private BuffEventPublisher _buffEvents;
        private BuffStageEffectExecutor _stageEffects;
        private BuffLifecycleExecutor _lifecycle;
        private global::Entitas.IGroup<global::ActorEntity> _group;

        public MobaBuffTickSystem(global::Entitas.IContexts contexts, IWorldResolver services)
            : base(contexts, services)
        {
        }

        protected override void OnInit()
        {
            Services.TryResolve(out _configs);
            Services.TryResolve(out _clock);
            Services.TryResolve(out AbilityKit.Triggering.Eventing.IEventBus eventBus);
            Services.TryResolve(out ITriggerActionRunner actionRunner);
            Services.TryResolve(out MobaPeriodicEffectService ongoing);
            Services.TryResolve(out MobaTraceRegistry trace);
            Services.TryResolve(out MobaEffectExecutionService effects);
            Services.TryResolve(out _tags);
            Services.TryResolve(out _tagTemplates);
            Services.TryResolve(out IFrameTime frameTime);
            Services.TryResolve(out IContinuousManager continuous);
            Services.TryResolve(out MobaActorLookupService actors);
            Services.TryResolve(out MobaSkillCastRuntimeService skillRuntimes);

            var repo = new BuffRepository();
            var ctx = new BuffContextService(trace, actionRunner, frameTime);
            _buffEvents = new BuffEventPublisher(eventBus);
            var periodicBinder = new BuffPeriodicEffectBinder(ongoing, actionRunner);
            _stageEffects = new BuffStageEffectExecutor(effects);
            var stacking = new BuffStackingPolicyApplier();
            _lifecycle = new BuffLifecycleExecutor(_configs, actors, ongoing, _tags, _tagTemplates, repo, ctx, _buffEvents, periodicBinder, _stageEffects, stacking, continuous, skillRuntimes);
            _group = Contexts.Actor().GetGroup(ActorMatcher.AllOf(ActorComponentsLookup.ActorId, ActorComponentsLookup.Buffs));
        }

        protected override void OnExecute()
        {
            if (_clock == null) return;
            var dt = _clock.DeltaTime;
            if (dt <= 0f) return;

            var entities = _group.GetEntities();
            if (entities == null || entities.Length == 0) return;

            for (int i = 0; i < entities.Length; i++)
            {
                var e = entities[i];
                if (e == null || !e.hasActorId || !e.hasBuffs) continue;

                var list = e.buffs.Active;
                if (list == null || list.Count == 0) continue;

                for (int j = list.Count - 1; j >= 0; j--)
                {
                    var runtime = list[j];
                    if (runtime == null)
                    {
                        list.RemoveAt(j);
                        continue;
                    }

                    var endedByTags = false;
                    if (_configs != null && _configs.TryGetBuff(runtime.BuffId, out var buffCfg) && buffCfg != null)
                    {
                        if (runtime.TagRequirements == null)
                        {
                            runtime.TagRequirements = BuffTagLifecycle.ResolveRequirements(buffCfg, _tagTemplates);
                        }

                        endedByTags = BuffTagLifecycle.ShouldEnd(_tags, e.actorId.Value, runtime.TagRequirements);
                    }

                    if (endedByTags)
                    {
                        runtime.Remaining = 0f;
                    }
                    else
                    {
                        runtime.Continuous?.Tick(dt);
                        SyncRemainingFromContinuous(runtime, dt);
                    }

                    if (!endedByTags && runtime.Continuous != null && !runtime.Continuous.IsTerminated) continue;
                    if (!endedByTags && runtime.Continuous == null && runtime.Remaining > 0f) continue;

                    var endReason = endedByTags ? TraceLifecycleReason.Interrupted : TraceLifecycleReason.Expired;
                    _lifecycle?.EndRuntime(e, list, j, runtime, runtime.SourceId, endReason);
                }
            }
        }

        private static void SyncRemainingFromContinuous(BuffRuntime runtime, float deltaTimeSeconds)
        {
            if (runtime == null) return;

            if (runtime.Continuous != null)
            {
                runtime.Remaining = runtime.Continuous.RemainingSeconds;
                return;
            }

            runtime.Remaining -= deltaTimeSeconds;
        }

    }
}
