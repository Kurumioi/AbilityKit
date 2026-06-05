using System;
using AbilityKit.Core.Common.Log;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World;
using AbilityKit.Ability.World.Services;

namespace AbilityKit.Demo.Moba.Systems
{
    [WorldSystem(order: MobaSystemOrder.SkillPipelines, Phase = WorldSystemPhase.Execute)]
    public sealed class MobaSkillPipelineStepSystem : WorldSystemBase
    {
        private SkillExecutor _skills;
        private IMobaSkillPipelineLibrary _pipelines;
        private IWorldClock _clock;

        private global::Entitas.IGroup<global::ActorEntity> _group;
        private int _executeLogCount;
        private int _stepLogCount;

        public MobaSkillPipelineStepSystem(global::Entitas.IContexts contexts, IWorldResolver services)
            : base(contexts, services)
        {
        }

        protected override void OnInit()
        {
            Services.TryResolve(out _skills);
            Services.TryResolve(out _clock);
            _group = Contexts.Actor().GetGroup(ActorMatcher.AllOf(ActorComponentsLookup.ActorId));
            if (_skills == null || _clock == null || _group == null)
            {
                Log.Warning($"[MobaSkillPipelineStepSystem] Init incomplete. hasSkills={_skills != null}, hasClock={_clock != null}, hasGroup={_group != null}");
            }
        }

        protected override void OnExecute()
        {
            if (_skills == null || _clock == null)
            {
                if (_executeLogCount++ < 3)
                {
                    Log.Warning($"[MobaSkillPipelineStepSystem] Skip execute: hasSkills={_skills != null}, hasClock={_clock != null}");
                }
                return;
            }

            if (_clock.DeltaTime <= 0f)
            {
                if (_executeLogCount++ < 3)
                {
                    Log.Warning($"[MobaSkillPipelineStepSystem] Skip execute: deltaTime={_clock.DeltaTime:0.####}");
                }
                return;
            }

            var entities = _group.GetEntities();
            if (entities == null || entities.Length == 0)
            {
                if (_executeLogCount++ < 3)
                {
                    Log.Warning("[MobaSkillPipelineStepSystem] Skip execute: actor group empty.");
                }
                return;
            }

            for (int i = 0; i < entities.Length; i++)
            {
                var e = entities[i];
                if (e == null || !e.hasActorId) continue;
                _skills.Step(e.actorId.Value);
            }
        }
    }
}

