using System;
using System.Collections.Generic;
using AbilityKit.Core.Logging;
using AbilityKit.Core.Serialization;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Demo.Moba.Share.Config;
using AbilityKit.Pipeline;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Ability.World.DI;
namespace AbilityKit.Demo.Moba.Services
{
    [WorldService(typeof(IMobaSkillPipelineLibrary), WorldLifetime.Scoped)]
    public sealed class TableDrivenMobaSkillPipelineLibrary : IMobaSkillPipelineLibrary
    {
        private static readonly AbilityPipelinePhaseId PreCastChecksPhaseId = new AbilityPipelinePhaseId("skill.checks.precast");
        private static readonly AbilityPipelinePhaseId PreCastTimelinePhaseId = new AbilityPipelinePhaseId("skill.timeline.precast");
        private static readonly AbilityPipelinePhaseId CastChecksPhaseId = new AbilityPipelinePhaseId("skill.checks.cast");
        private static readonly AbilityPipelinePhaseId CastTimelinePhaseId = new AbilityPipelinePhaseId("skill.timeline.cast");

        private readonly MobaConfigDatabase _configs;
        private readonly MobaEffectInvokerService _effects;
        private readonly IWorldResolver _services;
        private MobaTriggerPlanExecutor _rulePlanExecutor;

        public TableDrivenMobaSkillPipelineLibrary(
            MobaConfigDatabase configs,
            MobaEffectInvokerService effects,
            IWorldResolver services = null)
        {
            _configs = configs;
            _effects = effects;
            _services = services;
        }

        public bool TryGet(
            int skillId,
            out IAbilityPipelineConfig preCastConfig,
            out IReadOnlyList<IAbilityPipelinePhase<SkillPipelineContext>> preCastPhases,
            out IAbilityPipelineConfig castConfig,
            out IReadOnlyList<IAbilityPipelinePhase<SkillPipelineContext>> castPhases)
        {
            preCastConfig = null;
            preCastPhases = null;
            castConfig = null;
            castPhases = null;

            if (skillId <= 0) return false;
            if (_configs == null) return false;

            if (!_configs.TryGetSkill(skillId, out var skill) || skill == null) return false;

            if (skill.PreCastFlowId > 0 && _configs.TryGetSkillFlow(skill.PreCastFlowId, out var preFlow) && preFlow != null)
            {
                preCastConfig = new AbilityKit.Ability.Share.Impl.Pipeline.Skill.SkillPipelineConfig((skillId << 2) | 0, $"Skill_{skillId}_PreCast");
                preCastPhases = BuildFlowPhases(preFlow, checksPhaseId: PreCastChecksPhaseId, timelinePhaseId: PreCastTimelinePhaseId);
            }

            if (skill.CastFlowId <= 0) return false;
            if (!_configs.TryGetSkillFlow(skill.CastFlowId, out var castFlow) || castFlow == null) return false;

            castConfig = new AbilityKit.Ability.Share.Impl.Pipeline.Skill.SkillPipelineConfig((skillId << 2) | 1, $"Skill_{skillId}_Cast");
            castPhases = BuildFlowPhases(castFlow, checksPhaseId: CastChecksPhaseId, timelinePhaseId: CastTimelinePhaseId);

            return true;
        }

        private IReadOnlyList<IAbilityPipelinePhase<SkillPipelineContext>> BuildFlowPhases(
            SkillFlowMO flow,
            AbilityPipelinePhaseId checksPhaseId,
            AbilityPipelinePhaseId timelinePhaseId)
        {
            if (flow == null)
            {
                throw new InvalidOperationException("Skill flow is missing.");
            }

            if (flow.Phases == null || flow.Phases.Count == 0)
            {
                throw new InvalidOperationException($"Skill flow requires at least one phase. flowId={flow.Id}");
            }

            var list = new List<IAbilityPipelinePhase<SkillPipelineContext>>(flow.Phases.Count);
            for (int i = 0; i < flow.Phases.Count; i++)
            {
                var phase = BuildPhase(flow.Phases[i], checksPhaseId, timelinePhaseId, $"skill.flow.{flow.Id}.{i}");
                list.Add(phase);
            }

            return list;
        }

        private IAbilityPipelinePhase<SkillPipelineContext> BuildPhase(
            SkillPhaseDTO phase,
            AbilityPipelinePhaseId checksPhaseId,
            AbilityPipelinePhaseId timelinePhaseId,
            string fallbackPhaseId)
        {
            if (phase == null) throw new InvalidOperationException($"Skill flow phase is missing. phaseId={fallbackPhaseId}");

            var type = (SkillPhaseType)phase.Type;
            switch (type)
            {
                case SkillPhaseType.Checks:
                    throw new InvalidOperationException($"Skill Checks phase is deprecated. Use RulePlan trigger conditions instead. phaseId={MakePhaseId(phase, checksPhaseId.Value).Value}");
                case SkillPhaseType.Timeline:
                    if (phase.Timeline == null)
                    {
                        throw new InvalidOperationException($"Timeline skill phase requires timeline config. phaseId={MakePhaseId(phase, timelinePhaseId.Value).Value}");
                    }
                    var events = ToArray(phase.Timeline.Events);
                    return new SkillTimelinePhase(MakePhaseId(phase, timelinePhaseId.Value), phase.Timeline.DurationMs, events, _effects);
                case SkillPhaseType.Handlers:
                    throw new InvalidOperationException($"Skill Handlers phase is deprecated. Use RulePlan trigger actions instead. phaseId={MakePhaseId(phase, fallbackPhaseId).Value}");
                case SkillPhaseType.RulePlan:
                    if (phase.RulePlan == null)
                    {
                        throw new InvalidOperationException($"RulePlan skill phase requires rule plan config. phaseId={MakePhaseId(phase, fallbackPhaseId).Value}");
                    }
                    return new SkillRulePlanPhase(MakePhaseId(phase, fallbackPhaseId), phase.RulePlan, GetOrCreateRulePlanExecutor());
                case SkillPhaseType.Sequence:
                    return BuildSequencePhase(phase, checksPhaseId, timelinePhaseId, fallbackPhaseId);
                case SkillPhaseType.Parallel:
                    return BuildParallelPhase(phase, checksPhaseId, timelinePhaseId, fallbackPhaseId);
                case SkillPhaseType.Repeat:
                    return BuildRepeatPhase(phase, checksPhaseId, timelinePhaseId, fallbackPhaseId);
                case SkillPhaseType.Delay:
                    return BuildDelayPhase(phase, fallbackPhaseId);
                default:
                    throw new InvalidOperationException($"Unsupported skill phase type. phaseId={MakePhaseId(phase, fallbackPhaseId).Value}, type={phase.Type}");
            }
        }

        private IAbilityPipelinePhase<SkillPipelineContext> BuildSequencePhase(
            SkillPhaseDTO phase,
            AbilityPipelinePhaseId checksPhaseId,
            AbilityPipelinePhaseId timelinePhaseId,
            string fallbackPhaseId)
        {
            var sequence = new AbilitySequencePhase<SkillPipelineContext>(MakePhaseId(phase, fallbackPhaseId));
            AddChildren(sequence, phase.Children, checksPhaseId, timelinePhaseId, fallbackPhaseId);
            return sequence;
        }

        private IAbilityPipelinePhase<SkillPipelineContext> BuildParallelPhase(
            SkillPhaseDTO phase,
            AbilityPipelinePhaseId checksPhaseId,
            AbilityPipelinePhaseId timelinePhaseId,
            string fallbackPhaseId)
        {
            var parallel = new AbilityParallelPhase<SkillPipelineContext>(MakePhaseId(phase, fallbackPhaseId));
            AddChildren(parallel, phase.Children, checksPhaseId, timelinePhaseId, fallbackPhaseId);
            return parallel;
        }

        private IAbilityPipelinePhase<SkillPipelineContext> BuildRepeatPhase(
            SkillPhaseDTO phase,
            AbilityPipelinePhaseId checksPhaseId,
            AbilityPipelinePhaseId timelinePhaseId,
            string fallbackPhaseId)
        {
            var repeatDto = phase.Repeat;
            if (repeatDto == null)
            {
                throw new InvalidOperationException($"Repeat skill phase requires repeat config. phaseId={MakePhaseId(phase, fallbackPhaseId).Value}");
            }

            if (repeatDto.RepeatCount <= 0)
            {
                throw new InvalidOperationException($"Repeat skill phase repeat count must be positive. phaseId={MakePhaseId(phase, fallbackPhaseId).Value}, repeatCount={repeatDto.RepeatCount}");
            }

            if (repeatDto.IntervalMs < 0)
            {
                throw new InvalidOperationException($"Repeat skill phase interval cannot be negative. phaseId={MakePhaseId(phase, fallbackPhaseId).Value}, intervalMs={repeatDto.IntervalMs}");
            }

            if (repeatDto.Phase == null)
            {
                throw new InvalidOperationException($"Repeat skill phase requires an explicit child phase. phaseId={MakePhaseId(phase, fallbackPhaseId).Value}");
            }

            var repeat = new AbilityRepeatPhase<SkillPipelineContext>(MakePhaseId(phase, fallbackPhaseId), repeatDto.RepeatCount)
            {
                RepeatInterval = repeatDto.IntervalMs > 0 ? repeatDto.IntervalMs / 1000f : 0f
            };

            repeat.SetRepeatPhase(BuildPhase(repeatDto.Phase, checksPhaseId, timelinePhaseId, fallbackPhaseId + ".repeat"));
            return repeat;
        }

        private IAbilityPipelinePhase<SkillPipelineContext> BuildDelayPhase(SkillPhaseDTO phase, string fallbackPhaseId)
        {
            if (phase.Delay == null)
            {
                throw new InvalidOperationException($"Delay skill phase requires delay config. phaseId={MakePhaseId(phase, fallbackPhaseId).Value}");
            }

            var delayMs = phase.Delay.DelayMs;
            if (delayMs < 0)
            {
                throw new InvalidOperationException($"Delay skill phase delay cannot be negative. phaseId={MakePhaseId(phase, fallbackPhaseId).Value}, delayMs={delayMs}");
            }

            return new AbilityDelayPhase<SkillPipelineContext>(MakePhaseId(phase, fallbackPhaseId), delayMs / 1000f);
        }

        private void AddChildren(
            AbilityCompositePhase<SkillPipelineContext> parent,
            IReadOnlyList<SkillPhaseDTO> children,
            AbilityPipelinePhaseId checksPhaseId,
            AbilityPipelinePhaseId timelinePhaseId,
            string fallbackPhaseId)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            if (children == null || children.Count == 0)
            {
                throw new InvalidOperationException($"Composite skill phase requires at least one child phase. phaseId={fallbackPhaseId}");
            }

            for (int i = 0; i < children.Count; i++)
            {
                var child = BuildPhase(children[i], checksPhaseId, timelinePhaseId, fallbackPhaseId + "." + i);
                parent.AddSubPhase(child);
            }
        }

        private MobaTriggerPlanExecutor GetOrCreateRulePlanExecutor()
        {
            if (_rulePlanExecutor != null) return _rulePlanExecutor;
            if (_services == null) return null;

            _services.TryResolve<AbilityKit.Triggering.Eventing.IEventBus>(out var eventBus);
            _services.TryResolve<AbilityKit.Triggering.Registry.FunctionRegistry>(out var functions);
            _services.TryResolve<AbilityKit.Triggering.Registry.ActionRegistry>(out var actions);
            _services.TryResolve<AbilityKit.Triggering.Payload.IPayloadAccessorRegistry>(out var payloads);
            _services.TryResolve<AbilityKit.Triggering.Runtime.Plan.Json.TriggerPlanJsonDatabase>(out var planDb);

            _rulePlanExecutor = new MobaTriggerPlanExecutor(_services, planDb, eventBus, functions, actions, payloads);
            return _rulePlanExecutor;
        }

        private static AbilityPipelinePhaseId MakePhaseId(SkillPhaseDTO phase, string fallback)
        {
            return new AbilityPipelinePhaseId(!string.IsNullOrEmpty(phase?.PhaseId) ? phase.PhaseId : fallback);
        }

        private static SkillTimelineEventDTO[] ToArray(IReadOnlyList<SkillTimelineEventDTO> list)
        {
            if (list == null || list.Count == 0) return System.Array.Empty<SkillTimelineEventDTO>();
            var arr = new SkillTimelineEventDTO[list.Count];
            for (int i = 0; i < list.Count; i++) arr[i] = list[i];
            return arr;
        }

        public void Dispose()
        {
        }
    }
}
