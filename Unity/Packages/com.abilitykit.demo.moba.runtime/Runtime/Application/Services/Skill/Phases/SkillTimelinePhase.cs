using System;
using AbilityKit.Core.Serialization;
using AbilityKit.Demo.Moba.Share.Config;
using AbilityKit.Demo.Moba;
using AbilityKit.Core.Logging;
using AbilityKit.Pipeline;

namespace AbilityKit.Demo.Moba.Services
{
    using AbilityKit.Ability;
    public sealed class SkillTimelinePhase : AbilityPipelinePhaseBase<SkillPipelineContext>
    {
        private readonly int _durationMs;
        private readonly SkillTimelineEventDTO[] _events;
        private readonly MobaEffectInvokerService _effects;

        private int _nextIndex;

        public SkillTimelinePhase(AbilityPipelinePhaseId phaseId, int durationMs, SkillTimelineEventDTO[] events, MobaEffectInvokerService effects)
            : base(phaseId)
        {
            _durationMs = durationMs;
            _events = events;
            _effects = effects;
        }

        protected override void OnEnter(SkillPipelineContext context)
        {
            _nextIndex = 0;
        }

        protected override void OnExecute(SkillPipelineContext context)
        {
            // wait for OnUpdate
        }

        public override void OnUpdate(SkillPipelineContext context, float deltaTime)
        {
            if (IsComplete) return;

            context.SetTimelineNextEventIndex(_nextIndex);

            var elapsedMs = (int)(context.ElapsedTime * 1000f);

            if (_events != null)
            {
                while (_nextIndex < _events.Length)
                {
                    var e = _events[_nextIndex];
                    if (e == null)
                    {
                        _nextIndex++;
                        context.SetTimelineNextEventIndex(_nextIndex);
                        continue;
                    }

                    if (elapsedMs < e.AtMs) break;

                    var raw = e.ExecuteMode;
                    if (raw != (int)EffectExecuteMode.InternalOnly)
                    {
                        throw new InvalidOperationException($"Unsupported timeline effect execute mode. phase={PhaseId.Value}, eventIndex={_nextIndex}, effectId={e.EffectId}, executeMode={raw}, skillId={context?.SkillId ?? 0}");
                    }

                    if (e.EffectId <= 0)
                    {
                        throw new InvalidOperationException($"Invalid timeline effect id. phase={PhaseId.Value}, eventIndex={_nextIndex}, effectId={e.EffectId}, skillId={context?.SkillId ?? 0}");
                    }

                    var effects = ResolveEffects(context);
                    if (effects == null)
                    {
                        throw new InvalidOperationException($"Skill timeline requires MobaEffectInvokerService. phase={PhaseId.Value}, eventIndex={_nextIndex}, effectId={e.EffectId}, skillId={context?.SkillId ?? 0}");
                    }

                    effects.Execute(e.EffectId, context);

                    _nextIndex++;

                    context.SetTimelineNextEventIndex(_nextIndex);
                }
            }

            if (_durationMs > 0)
            {
                if (elapsedMs >= _durationMs)
                {
                    Complete(context);
                }
            }
            else
            {
                if (_events == null || _nextIndex >= _events.Length)
                {
                    Complete(context);
                }
            }
        }

        public override void Reset()
        {
            base.Reset();
            _nextIndex = 0;
        }

        private MobaEffectInvokerService ResolveEffects(SkillPipelineContext context)
        {
            if (_effects != null) return _effects;
            if (context?.WorldServices == null) return null;
            return context.WorldServices.TryResolve<MobaEffectInvokerService>(out var effects) ? effects : null;
        }
    }
}
