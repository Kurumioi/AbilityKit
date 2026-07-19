using System;
using AbilityKit.Demo.Moba.Attributes;
using AbilityKit.Demo.Moba.Diagnostics;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;

namespace AbilityKit.Demo.Moba.Services
{
    using AbilityKit.Demo.Moba;
    [WorldService(typeof(MobaDamageService))]
    public sealed class MobaDamageService : IService
    {
        private readonly MobaActorLookupService _actors;
        private readonly MobaDamageEventSnapshotService _snapshots;
        private readonly MobaCombatRulesService _rules;
        private readonly IMobaBattleDiagnosticEventSink _eventCollector;

        public MobaDamageService(
            MobaActorLookupService actors,
            MobaDamageEventSnapshotService snapshots,
            MobaCombatRulesService rules = null,
            IMobaBattleDiagnosticEventSink eventCollector = null)
        {
            _actors = actors ?? throw new ArgumentNullException(nameof(actors));
            _snapshots = snapshots ?? throw new ArgumentNullException(nameof(snapshots));
            _rules = rules;
            _eventCollector = eventCollector;
        }

        public float ApplyDamage(int attackerActorId, int targetActorId, int damageType, float value, int reasonKind = 0, int reasonParam = 0)
        {
            if (targetActorId <= 0) return 0f;
            if (value <= 0f) return 0f;

            if (_rules != null && !_rules.CanReceiveDamage(attackerActorId, targetActorId).Passed) return 0f;
            if (!_actors.TryGetActorEntity(targetActorId, out var target) || target == null) return 0f;

            var attrs = target.GetMobaAttrs();
            var oldHp = attrs.Hp;
            var maxHp = attrs.MaxHp;

            var newHp = Clamp(oldHp - value, 0f, maxHp);
            var actual = oldHp - newHp;
            if (actual <= 0f) return 0f;

            attrs.Hp = newHp;
            _snapshots.ReportDamage(attackerActorId, targetActorId, damageType, actual, reasonKind, reasonParam, newHp, maxHp);
            CollectDirectDamage(attackerActorId, targetActorId, damageType, actual, reasonKind, reasonParam, newHp, maxHp);
            return actual;
        }

        public float ApplyHeal(int healerActorId, int targetActorId, int healType, float value, int reasonKind = 0, int reasonParam = 0)
        {
            if (targetActorId <= 0) return 0f;
            if (value <= 0f) return 0f;

            if (_rules != null && (!_rules.TryGetActor(targetActorId, out _) || !_rules.IsAlive(targetActorId))) return 0f;
            if (!_actors.TryGetActorEntity(targetActorId, out var target) || target == null) return 0f;

            var attrs = target.GetMobaAttrs();
            var oldHp = attrs.Hp;
            var maxHp = attrs.MaxHp;

            var newHp = Clamp(oldHp + value, 0f, maxHp);
            var actual = newHp - oldHp;
            if (actual <= 0f) return 0f;

            attrs.Hp = newHp;
            _snapshots.ReportHeal(healerActorId, targetActorId, healType, actual, reasonKind, reasonParam, newHp, maxHp);
            CollectHeal(healerActorId, targetActorId, healType, actual, reasonKind, reasonParam, newHp, maxHp);
            return actual;
        }

        internal static MobaBattleDiagnosticEventDraft CreateDirectDamageDraft(
            int attackerActorId,
            int targetActorId,
            int damageType,
            float value,
            int reasonKind,
            int reasonParam,
            float targetHp,
            float maxHp)
        {
            var configId = reasonParam;
            var summary = $"directDamage={value:0.###}, damageType={damageType}, reasonKind={reasonKind}, targetHp={targetHp:0.###}, maxHp={maxHp:0.###}";

            return new MobaBattleDiagnosticEventDraft(
                BattleDiagnosticEventKind.Damage,
                BattleDiagnosticEventChannel.DamageAndHeal,
                BattleDiagnosticEventOutcome.Succeeded,
                attackerActorId,
                targetActorId,
                configId,
                summary: summary);
        }

        internal static MobaBattleDiagnosticEventDraft CreateHealDraft(
            int healerActorId,
            int targetActorId,
            int healType,
            float value,
            int reasonKind,
            int reasonParam,
            float targetHp,
            float maxHp)
        {
            var configId = reasonParam;
            var summary = $"heal={value:0.###}, healType={healType}, reasonKind={reasonKind}, targetHp={targetHp:0.###}, maxHp={maxHp:0.###}";

            return new MobaBattleDiagnosticEventDraft(
                BattleDiagnosticEventKind.Heal,
                BattleDiagnosticEventChannel.DamageAndHeal,
                BattleDiagnosticEventOutcome.Succeeded,
                healerActorId,
                targetActorId,
                configId,
                summary: summary);
        }

        private void CollectDirectDamage(
            int attackerActorId,
            int targetActorId,
            int damageType,
            float value,
            int reasonKind,
            int reasonParam,
            float targetHp,
            float maxHp)
        {
            if (_eventCollector == null) return;

            try
            {
                var draft = CreateDirectDamageDraft(
                    attackerActorId,
                    targetActorId,
                    damageType,
                    value,
                    reasonKind,
                    reasonParam,
                    targetHp,
                    maxHp);
                _eventCollector.TryCollect(in draft);
            }
            catch (Exception)
            {
                // 诊断提交失败不应影响直接伤害流程，静默吞掉异常。
            }
        }

        private void CollectHeal(
            int healerActorId,
            int targetActorId,
            int healType,
            float value,
            int reasonKind,
            int reasonParam,
            float targetHp,
            float maxHp)
        {
            if (_eventCollector == null) return;

            try
            {
                var draft = CreateHealDraft(
                    healerActorId,
                    targetActorId,
                    healType,
                    value,
                    reasonKind,
                    reasonParam,
                    targetHp,
                    maxHp);
                _eventCollector.TryCollect(in draft);
            }
            catch (Exception)
            {
                // 诊断提交失败不应影响治疗流程，静默吞掉异常。
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
