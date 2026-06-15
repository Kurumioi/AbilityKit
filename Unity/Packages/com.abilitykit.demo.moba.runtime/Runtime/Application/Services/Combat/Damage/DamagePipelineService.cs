using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Core.Eventing;
using AbilityKit.Core.Logging;

namespace AbilityKit.Demo.Moba.Services
{
    [WorldService(typeof(DamagePipelineService))]
    public sealed class DamagePipelineService : IService
    {
        private readonly MobaActorLookupService _actors;
        private readonly MobaDamageService _damage;
        private readonly AbilityKit.Triggering.Eventing.IEventBus _eventBus;
        private readonly List<IMobaDamagePipelineStage> _standardStages;
        private readonly IMobaBattleDiagnosticsService _diagnostics;

        public DamagePipelineService(
            MobaActorLookupService actors,
            MobaDamageService damage,
            AbilityKit.Triggering.Eventing.IEventBus eventBus,
            MobaDamageMitigationService mitigation = null,
            MobaShieldService shields = null,
            IMobaBattleDiagnosticsService diagnostics = null)
        {
            _actors = actors ?? throw new ArgumentNullException(nameof(actors));
            _damage = damage ?? throw new ArgumentNullException(nameof(damage));
            _eventBus = eventBus;
            _diagnostics = diagnostics;
            _standardStages = new List<IMobaDamagePipelineStage>(4)
            {
                new MobaBaseDamagePipelineStage(),
                new MobaDamageMitigationPipelineStage(mitigation),
                new MobaShieldAbsorbPipelineStage(shields),
                new MobaFinalDamagePipelineStage(),
            };
        }

        public DamageResult Execute(AttackInfo attack)
        {
            if (attack == null) return null;
            if (attack.TargetActorId <= 0) return null;

            var diagnostics = _diagnostics;
            var start = diagnostics != null ? diagnostics.GetTimestamp() : 0L;

            try
            {
                if (!_actors.TryGetActorEntity(attack.TargetActorId, out var target) || target == null)
                {
                    diagnostics?.Counter("moba.damage.targetMissing");
                    return null;
                }

                Publish(DamagePipelineEvents.AttackCreated, attack);
                Publish(DamagePipelineEvents.BeforeCalc, attack);

                var calc = new AttackCalcInfo(attack);

                Publish(DamagePipelineEvents.CalcBegin, calc);

                ApplyFormula(calc);

                Publish(DamagePipelineEvents.BeforeApply, calc);

                var targetAttrs = target.GetMobaAttrs();
                var oldHp = targetAttrs.Hp;
                var maxHp = targetAttrs.MaxHp;

                var applied = _damage.ApplyDamage(
                    attackerActorId: attack.AttackerActorId,
                    targetActorId: attack.TargetActorId,
                    damageType: (int)attack.DamageType,
                    value: calc.HpDamage.Value,
                    reasonKind: (int)attack.ReasonKind,
                    reasonParam: attack.ReasonParam);

                var result = new DamageResult
                {
                    AttackerActorId = attack.AttackerActorId,
                    TargetActorId = attack.TargetActorId,

                    DamageType = attack.DamageType,
                    CritType = attack.CritType,
                    ReasonKind = attack.ReasonKind,
                    ReasonParam = attack.ReasonParam,
                    Value = applied,
                    TargetHp = Clamp(oldHp - applied, 0f, maxHp),
                    TargetMaxHp = maxHp,
                };

                if (attack.TryGetOrigin(out var origin))
                {
                    result.SetOrigin(in origin);
                }

                Publish(DamagePipelineEvents.AfterApply, result);
                diagnostics?.Counter("moba.damage.applied");
                diagnostics?.Sample("moba.damage.value", applied);
                return result;
            }
            finally
            {
                diagnostics?.RecordDuration(
                    MobaBattleDiagnosticMetric.DamagePipeline,
                    start,
                    MobaBattleDiagnosticsDefaults.DamagePipelineWarnMs,
                    $"attacker={attack.AttackerActorId} target={attack.TargetActorId} type={attack.DamageType}");
            }
        }

        private void ApplyFormula(AttackCalcInfo calc)
        {
            if (calc == null || calc.Attack == null) return;

            var attack = calc.Attack;
            var kind = (DamageFormulaKind)attack.FormulaKind;
            if (kind == DamageFormulaKind.None) kind = DamageFormulaKind.Standard;

            switch (kind)
            {
                case DamageFormulaKind.Standard:
                default:
                    RunStages(calc, _standardStages);
                    break;
            }
        }

        private void RunStages(AttackCalcInfo calc, List<IMobaDamagePipelineStage> stages)
        {
            if (calc == null || stages == null) return;

            var diagnostics = _diagnostics;
            for (var i = 0; i < stages.Count; i++)
            {
                var stage = stages[i];
                if (stage == null) continue;

                var start = diagnostics != null ? diagnostics.GetTimestamp() : 0L;
                stage.Execute(calc);
                diagnostics?.RecordDuration(
                    MobaBattleDiagnosticMetric.DamageStage,
                    start,
                    MobaBattleDiagnosticsDefaults.DamageStageWarnMs,
                    $"stage={stage.GetType().Name} event={stage.EventId}");
                Publish(stage.EventId, calc);
            }
        }

        private void Publish(string eventId, object payload)
        {
            var eventBus = _eventBus;
            if (eventBus == null) return;
            if (string.IsNullOrEmpty(eventId)) return;

            var eid = TriggeringIdUtil.GetEventEid(eventId);

            if (payload is AttackInfo ai2)
            {
                eventBus.Publish(new EventKey<AttackInfo>(eid), in ai2);
                object boxed = ai2;
                eventBus.Publish(new EventKey<object>(eid), in boxed);
            }
            else if (payload is AttackCalcInfo ac2)
            {
                eventBus.Publish(new EventKey<AttackCalcInfo>(eid), in ac2);
                object boxed = ac2;
                eventBus.Publish(new EventKey<object>(eid), in boxed);
            }
            else if (payload is DamageResult dr2)
            {
                eventBus.Publish(new EventKey<DamageResult>(eid), in dr2);
                object boxed = dr2;
                eventBus.Publish(new EventKey<object>(eid), in boxed);
            }
            else
            {
                object boxed = payload;
                eventBus.Publish(new EventKey<object>(eid), in boxed);
            }
        }

        private static float Clamp(float v, float min, float max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        public void Dispose()
        {
        }
    }
}
