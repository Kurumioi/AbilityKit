using System;
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
        private IWorldClock _clock;

        private MobaWorldSystemServices _systemServices;
        private global::Entitas.IGroup<global::ActorEntity> _group;

        public MobaSkillPipelineStepSystem(global::Entitas.IContexts contexts, IWorldResolver services)
            : base(contexts, services)
        {
        }

        protected override void OnInit()
        {
            Services.TryResolve(out _skills);
            Services.TryResolve(out _clock);
            _systemServices = MobaWorldSystemExecution.Resolve(Services);
            _group = Contexts.Actor().GetGroup(ActorMatcher.AllOf(ActorComponentsLookup.ActorId));
        }

        protected override void OnExecute()
        {
            MobaWorldSystemExecution.Require(
                _skills != null && _clock != null && _group != null,
                Services,
                nameof(MobaSkillPipelineStepSystem),
                "skill.pipeline.step",
                "SkillExecutor, IWorldClock and actor group",
                $"hasSkills={_skills != null}, hasClock={_clock != null}, hasGroup={_group != null}");

            if (_clock.DeltaTime <= 0f)
            {
                MobaWorldSystemExecution.Warn(
                    in _systemServices,
                    "skill.pipeline.invalidDeltaTime",
                    $"[MobaSkillPipelineStepSystem] Skip execute: deltaTime={_clock.DeltaTime:0.####}");
                return;
            }

            var entities = _group.GetEntities();
            if (entities == null || entities.Length == 0)
            {
                MobaWorldSystemExecution.Warn(
                    in _systemServices,
                    "skill.pipeline.emptyGroup",
                    "[MobaSkillPipelineStepSystem] Skip execute: actor group empty.");
                return;
            }

            var start = _systemServices.StartTimestamp;
            var stepped = 0;
            for (int i = 0; i < entities.Length; i++)
            {
                var e = entities[i];
                if (e == null || !e.hasActorId) continue;
                var actorId = e.actorId.Value;
                try
                {
                    _skills.Step(actorId);
                    stepped++;
                }
                catch (Exception ex)
                {
                    MobaWorldSystemExecution.HandleException(
                        in _systemServices,
                        ex,
                        nameof(MobaSkillPipelineStepSystem),
                        "skill.pipeline.step",
                        actorId: actorId);
                }
            }

            MobaWorldSystemExecution.Sample(in _systemServices, "moba.skill.pipeline.actorCandidates", entities.Length);
            MobaWorldSystemExecution.Sample(in _systemServices, "moba.skill.pipeline.stepped", stepped);
            MobaWorldSystemExecution.RecordDuration(
                in _systemServices,
                MobaBattleDiagnosticMetric.SkillPipelineStep,
                start,
                MobaBattleDiagnosticsDefaults.SkillPipelineStepWarnMs,
                $"candidates={entities.Length} stepped={stepped}");
        }
    }
}

