using System;
using AbilityKit.Core.Common.Projectile;
using AbilityKit.Demo.Moba;
using AbilityKit.Demo.Moba.Events.Buff;
using AbilityKit.Demo.Moba.Events.Summon;
using AbilityKit.Demo.Moba.Events.Unit;
using AbilityKit.Demo.Moba.Gameplay.Triggering;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Services.Projectile;
using AbilityKit.Ability.Share.Effect;

namespace AbilityKit.Demo.Moba.Systems
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
    public sealed class MobaTriggerEventAttribute : Attribute
    {
        public string EventIdOrPrefix { get; }
        public Type ArgsType { get; }
        public bool IsPrefix { get; }

        public MobaTriggerEventAttribute(string eventIdOrPrefix, Type argsType, bool isPrefix = false)
        {
            EventIdOrPrefix = eventIdOrPrefix;
            ArgsType = argsType;
            IsPrefix = isPrefix;
        }
    }

    [MobaTriggerEvent("skill.", typeof(SkillCastContext), isPrefix: true)]
    [MobaTriggerEvent(MobaBuffTriggering.Prefixes.Buff, typeof(BuffEventArgs), isPrefix: true)]
    [MobaTriggerEvent("area.", typeof(AreaEventArgs), isPrefix: true)]
    [MobaTriggerEvent("presentation.", typeof(AbilityKit.Demo.Moba.Triggering.PresentationEventArgs), isPrefix: true)]
    [MobaTriggerEvent("gameplay.", typeof(GameplayLifecycleEventArgs), isPrefix: true)]
    [MobaTriggerEvent(DamagePipelineEvents.AttackCreated, typeof(AttackInfo))]
    [MobaTriggerEvent(DamagePipelineEvents.BeforeCalc, typeof(AttackInfo))]
    [MobaTriggerEvent(DamagePipelineEvents.CalcBegin, typeof(AttackCalcInfo))]
    [MobaTriggerEvent(DamagePipelineEvents.AfterBase, typeof(AttackCalcInfo))]
    [MobaTriggerEvent(DamagePipelineEvents.AfterMitigate, typeof(AttackCalcInfo))]
    [MobaTriggerEvent(DamagePipelineEvents.AfterShield, typeof(AttackCalcInfo))]
    [MobaTriggerEvent(DamagePipelineEvents.CalcFinal, typeof(AttackCalcInfo))]
    [MobaTriggerEvent(DamagePipelineEvents.BeforeApply, typeof(AttackCalcInfo))]
    [MobaTriggerEvent(DamagePipelineEvents.AfterApply, typeof(DamageResult))]
    [MobaTriggerEvent(ProjectileTriggering.Events.Hit, typeof(ProjectileHitEvent))]
    [MobaTriggerEvent(ProjectileTriggering.Events.Spawn, typeof(ProjectileSpawnEvent))]
    [MobaTriggerEvent(ProjectileTriggering.Events.Tick, typeof(ProjectileTickEvent))]
    [MobaTriggerEvent(ProjectileTriggering.Events.Exit, typeof(ProjectileExitEvent))]
    [MobaTriggerEvent("summon.", typeof(SummonEventPayload), isPrefix: true)]
    [MobaTriggerEvent("unit.", typeof(UnitEventPayload), isPrefix: true)]
    [MobaTriggerEvent(MobaUnitTriggering.Events.Die, typeof(UnitDieEventPayload))]
    internal static class MobaTriggerEventMappings
    {
    }
}
