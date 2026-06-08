using AbilityKit.Ability.Triggering;
using AbilityKit.Ability.World;
using AbilityKit.Ability.World.DI;
using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.Components;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Trace;

namespace AbilityKit.Demo.Moba.Systems.Buffs
{
    [WorldSystem(order: MobaSystemOrder.BuffsTick, Phase = WorldSystemPhase.Execute)]
    public sealed class MobaBuffTickSystem : WorldSystemBase
    {
        private MobaConfigDatabase _configs;
        private IMobaEffectiveTagQueryService _tags;
        private IMobaContinuousTagTemplateRegistry _tagTemplates;
        private BuffLifecycleExecutor _lifecycle;
        private global::Entitas.IGroup<global::ActorEntity> _group;

        public MobaBuffTickSystem(global::Entitas.IContexts contexts, IWorldResolver services)
            : base(contexts, services)
        {
        }

        protected override void OnInit()
        {
            Services.TryResolve(out _configs);
            Services.TryResolve(out _tags);
            Services.TryResolve(out _tagTemplates);

            _lifecycle = BuffLifecycleExecutorFactory.Create(Services);
            _group = Contexts.Actor().GetGroup(ActorMatcher.AllOf(ActorComponentsLookup.ActorId, ActorComponentsLookup.Buffs));
        }

        protected override void OnExecute()
        {
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
                    BuffMO buffCfg = null;
                    if (_configs != null && _configs.TryGetBuff(runtime.BuffId, out buffCfg) && buffCfg != null)
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
                        SyncFromContinuous(runtime);
                    }

                    if (!endedByTags && runtime.Continuous != null && !runtime.Continuous.IsTerminated) continue;
                    if (!endedByTags && runtime.Continuous == null && runtime.Remaining > 0f) continue;

                    var endReason = endedByTags ? TraceLifecycleReason.Interrupted : TraceLifecycleReason.Expired;
                    _lifecycle?.EndRuntime(e, list, j, runtime, runtime.SourceId, endReason);
                }
            }
        }

        private static void SyncFromContinuous(BuffRuntime runtime)
        {
            if (runtime == null || runtime.Continuous == null) return;

            runtime.Remaining = runtime.Continuous.RemainingSeconds;
            runtime.IntervalRemainingSeconds = runtime.Continuous.IntervalRemainingSeconds;
        }

    }
}
