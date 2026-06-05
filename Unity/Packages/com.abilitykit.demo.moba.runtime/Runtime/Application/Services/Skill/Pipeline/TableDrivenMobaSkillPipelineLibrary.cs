using System;
using System.Collections.Generic;
using AbilityKit.Core.Common.Log;
using AbilityKit.Core.Generic;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Demo.Moba.Share.Config;
using AbilityKit.Pipeline;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Ability.World.DI;
using AbilityKit.GameplayTags;

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
        private readonly SkillConditionRegistry _conditionRegistry;
        private readonly IGameplayTagService _tags;

        public TableDrivenMobaSkillPipelineLibrary(
            MobaConfigDatabase configs,
            MobaEffectInvokerService effects,
            SkillConditionRegistry conditionRegistry = null,
            IGameplayTagService tags = null)
        {
            _configs = configs;
            _effects = effects;
            _conditionRegistry = conditionRegistry;
            _tags = tags;
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
            if (flow == null || flow.Phases == null || flow.Phases.Count == 0)
            {
                return System.Array.Empty<IAbilityPipelinePhase<SkillPipelineContext>>();
            }

            var list = new List<IAbilityPipelinePhase<SkillPipelineContext>>(flow.Phases.Count);
            for (int i = 0; i < flow.Phases.Count; i++)
            {
                var phase = BuildPhase(flow.Phases[i], checksPhaseId, timelinePhaseId, $"skill.flow.{flow.Id}.{i}");
                if (phase != null) list.Add(phase);
            }

            return list;
        }

        private IAbilityPipelinePhase<SkillPipelineContext> BuildPhase(
            SkillPhaseDTO phase,
            AbilityPipelinePhaseId checksPhaseId,
            AbilityPipelinePhaseId timelinePhaseId,
            string fallbackPhaseId)
        {
            if (phase == null) return null;

            var type = (SkillPhaseType)phase.Type;
            switch (type)
            {
                case SkillPhaseType.Checks:
                    return new SkillFlowChecksPhase(MakePhaseId(phase, checksPhaseId.Value), phase.Checks, _conditionRegistry, _tags);
                case SkillPhaseType.Timeline:
                    if (phase.Timeline == null) return null;
                    var events = ToArray(phase.Timeline.Events);
                    return new SkillTimelinePhase(MakePhaseId(phase, timelinePhaseId.Value), phase.Timeline.DurationMs, events, _effects);
                case SkillPhaseType.Sequence:
                    return BuildSequencePhase(phase, checksPhaseId, timelinePhaseId, fallbackPhaseId);
                case SkillPhaseType.Parallel:
                    return BuildParallelPhase(phase, checksPhaseId, timelinePhaseId, fallbackPhaseId);
                case SkillPhaseType.Repeat:
                    return BuildRepeatPhase(phase, checksPhaseId, timelinePhaseId, fallbackPhaseId);
                case SkillPhaseType.Delay:
                    return BuildDelayPhase(phase, fallbackPhaseId);
                default:
                    return null;
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
            var repeatCount = repeatDto != null && repeatDto.RepeatCount != 0 ? repeatDto.RepeatCount : 1;
            var repeat = new AbilityRepeatPhase<SkillPipelineContext>(MakePhaseId(phase, fallbackPhaseId), repeatCount)
            {
                RepeatInterval = repeatDto != null && repeatDto.IntervalMs > 0 ? repeatDto.IntervalMs / 1000f : 0f
            };

            var child = repeatDto != null && repeatDto.Phase != null
                ? BuildPhase(repeatDto.Phase, checksPhaseId, timelinePhaseId, fallbackPhaseId + ".repeat")
                : BuildFirstChild(phase.Children, checksPhaseId, timelinePhaseId, fallbackPhaseId + ".repeat");
            if (child != null) repeat.SetRepeatPhase(child);
            return repeat;
        }

        private IAbilityPipelinePhase<SkillPipelineContext> BuildDelayPhase(SkillPhaseDTO phase, string fallbackPhaseId)
        {
            var delayMs = phase.Delay != null ? phase.Delay.DelayMs : 0;
            return new AbilityDelayPhase<SkillPipelineContext>(MakePhaseId(phase, fallbackPhaseId), delayMs > 0 ? delayMs / 1000f : 0f);
        }

        private void AddChildren(
            AbilityCompositePhase<SkillPipelineContext> parent,
            IReadOnlyList<SkillPhaseDTO> children,
            AbilityPipelinePhaseId checksPhaseId,
            AbilityPipelinePhaseId timelinePhaseId,
            string fallbackPhaseId)
        {
            if (parent == null || children == null) return;
            for (int i = 0; i < children.Count; i++)
            {
                var child = BuildPhase(children[i], checksPhaseId, timelinePhaseId, fallbackPhaseId + "." + i);
                if (child != null) parent.AddSubPhase(child);
            }
        }

        private IAbilityPipelinePhase<SkillPipelineContext> BuildFirstChild(
            IReadOnlyList<SkillPhaseDTO> children,
            AbilityPipelinePhaseId checksPhaseId,
            AbilityPipelinePhaseId timelinePhaseId,
            string fallbackPhaseId)
        {
            if (children == null || children.Count == 0) return null;
            return BuildPhase(children[0], checksPhaseId, timelinePhaseId, fallbackPhaseId);
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
