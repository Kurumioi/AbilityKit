using System;
using System.Collections.Generic;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Core.Eventing;
using AbilityKit.Demo.Moba.Attributes;
using AbilityKit.Modifiers;

namespace AbilityKit.Demo.Moba.Services.Passive
{
    [WorldService(typeof(LianPoPassiveRageService))]
    public sealed class LianPoPassiveRageService : IService, IWorldDeinitializable
    {
        public const int PassiveSkillId = 10010000;
        public const int ModifierSourceId = 1001000001;

        public const float MaxRage = 100f;
        public const float RageOnDamageDealt = 10f;
        public const float RageOnDamageTaken = 5f;
        public const float OutOfCombatDelaySeconds = 3f;
        public const float RageToHealPerSecond = 20f;
        public const float FullRageAttackSpeedBonus = 0.3f;
        public const float FullRageDefenseBonus = 20f;

        private readonly AbilityKit.Triggering.Eventing.IEventBus _eventBus;
        private readonly MobaActorLookupService _actors;
        private readonly MobaDamageService _damage;
        private readonly IWorldClock _clock;
        private readonly Dictionary<int, float> _lastCombatTimeByActorId = new Dictionary<int, float>();
        private IDisposable _afterApplySub;

        public LianPoPassiveRageService(
            AbilityKit.Triggering.Eventing.IEventBus eventBus,
            MobaActorLookupService actors,
            MobaDamageService damage,
            IWorldClock clock = null)
        {
            _eventBus = eventBus;
            _actors = actors;
            _damage = damage;
            _clock = clock;

            var eid = TriggeringIdUtil.GetEventEid(DamagePipelineEvents.AfterApply);
            _afterApplySub = _eventBus != null ? _eventBus.Subscribe(new EventKey<DamageResult>(eid), OnAfterApply) : null;
        }

        public void TickActor(int actorId, global::ActorEntity entity, float nowSeconds, float deltaSeconds)
        {
            if (actorId <= 0 || entity == null) return;

            if (!HasLianPoPassive(entity))
            {
                ClearRageModifiers(entity);
                return;
            }

            var attrs = new MobaAttrs(entity);
            attrs.Rage = Clamp(attrs.Rage, 0f, MaxRage);
            ApplyRageModifiers(entity, attrs.Rage);

            if (deltaSeconds <= 0f) return;
            if (!_lastCombatTimeByActorId.TryGetValue(actorId, out var lastCombatTime)) return;
            if (nowSeconds - lastCombatTime < OutOfCombatDelaySeconds) return;

            ConvertRageToHeal(actorId, entity, attrs, deltaSeconds);
            ApplyRageModifiers(entity, attrs.Rage);
        }

        public float GetLastCombatTime(int actorId)
        {
            return _lastCombatTimeByActorId.TryGetValue(actorId, out var t) ? t : float.NegativeInfinity;
        }

        private void OnAfterApply(DamageResult result)
        {
            if (result == null || result.Value <= 0f) return;

            var now = _clock != null ? _clock.Time : 0f;
            TryGainRage(result.AttackerActorId, RageOnDamageDealt, now);
            if (result.TargetActorId != result.AttackerActorId)
            {
                TryGainRage(result.TargetActorId, RageOnDamageTaken, now);
            }
        }

        private void TryGainRage(int actorId, float amount, float nowSeconds)
        {
            if (actorId <= 0 || amount <= 0f) return;
            if (_actors == null || !_actors.TryGetActorEntity(actorId, out var entity) || entity == null) return;
            if (!HasLianPoPassive(entity)) return;

            var attrs = new MobaAttrs(entity);
            attrs.Rage = Clamp(attrs.Rage + amount, 0f, MaxRage);
            _lastCombatTimeByActorId[actorId] = nowSeconds;
            ApplyRageModifiers(entity, attrs.Rage);
        }

        private void ConvertRageToHeal(int actorId, global::ActorEntity entity, MobaAttrs attrs, float deltaSeconds)
        {
            var rage = attrs.Rage;
            if (rage <= 0f) return;

            var consume = Math.Min(rage, RageToHealPerSecond * deltaSeconds);
            if (consume <= 0f) return;

            var healed = _damage != null
                ? _damage.ApplyHeal(actorId, actorId, (int)DamageType.None, consume, (int)DamageReasonKind.Buff, PassiveSkillId)
                : 0f;

            if (healed <= 0f && attrs.Hp >= attrs.MaxHp) return;
            attrs.Rage = Clamp(rage - consume, 0f, MaxRage);
        }

        private static bool HasLianPoPassive(global::ActorEntity entity)
        {
            if (entity == null || !entity.hasSkillLoadout) return false;
            var passives = entity.skillLoadout.PassiveSkills;
            if (passives == null || passives.Length == 0) return false;

            for (var i = 0; i < passives.Length; i++)
            {
                var passive = passives[i];
                if (passive != null && passive.PassiveSkillId == PassiveSkillId) return true;
            }

            return false;
        }

        private static void ApplyRageModifiers(global::ActorEntity entity, float rage)
        {
            if (entity == null || !entity.hasAttributeGroup || entity.attributeGroup.Group == null) return;

            var ratio = MaxRage > 0f ? Clamp(rage, 0f, MaxRage) / MaxRage : 0f;
            entity.attributeGroup.Group.ClearModifiers(ModifierSourceId);
            if (ratio <= 0f) return;

            var attrs = new MobaAttrs(entity);
            attrs.AddModifier(BattleAttributeType.ATTACK_SPEED_R, ModifierOp.Add, FullRageAttackSpeedBonus * ratio, ModifierSourceId);
            attrs.AddModifier(BattleAttributeType.PHYSICS_DEFENSE, ModifierOp.Add, FullRageDefenseBonus * ratio, ModifierSourceId);
            attrs.AddModifier(BattleAttributeType.MAGIC_DEFENSE, ModifierOp.Add, FullRageDefenseBonus * ratio, ModifierSourceId);
        }

        private static void ClearRageModifiers(global::ActorEntity entity)
        {
            if (entity == null || !entity.hasAttributeGroup || entity.attributeGroup.Group == null) return;
            entity.attributeGroup.Group.ClearModifiers(ModifierSourceId);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public void OnDeinit(IWorldResolver services)
        {
            Dispose();
        }

        public void Dispose()
        {
            var sub = _afterApplySub;
            if (sub != null)
            {
                _afterApplySub = null;
                sub.Dispose();
            }
        }
    }
}
