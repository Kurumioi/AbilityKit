using System;
using AbilityKit.Modifiers;
using AbilityKit.Triggering.Runtime.Config;

namespace AbilityKit.Triggering.Runtime.Executable
{
    // ========================================================================
    // 装饰器 DSL
    // ========================================================================

    public static class DecoratorDsl
    {
        /// <summary>
        /// DOT: 持续伤害
        /// </summary>
        [Obsolete("DOT belongs to legacy Runtime/Executable DSL. Use Runtime.Plan executables and TriggerPlanExecutableDsl.Scheduled/Periodic instead.")]
        public static IScheduledExecutable DOT(
            ISimpleExecutable damageEffect,
            float durationMs,
            float intervalMs,
            params string[] tags)
        {
            var decorated = new DecoratorBuilder(damageEffect)
                .WithTags(tags)
                .WithDuration(durationMs)
                .Build();
            return ScheduledExecutableFactory.WrapPeriodic(decorated, intervalMs);
        }

        /// <summary>
        /// HOT: 持续治疗
        /// </summary>
        [Obsolete("HOT belongs to legacy Runtime/Executable DSL. Use Runtime.Plan executables and TriggerPlanExecutableDsl.Scheduled/Periodic instead.")]
        public static IScheduledExecutable HOT(
            ISimpleExecutable healEffect,
            float durationMs,
            float intervalMs,
            params string[] tags)
        {
            var decorated = new DecoratorBuilder(healEffect)
                .WithTags(tags)
                .WithDuration(durationMs)
                .Build();
            return ScheduledExecutableFactory.WrapPeriodic(decorated, intervalMs);
        }

        /// <summary>
        /// Buff: 属性增益
        /// </summary>
        [Obsolete("Buff belongs to legacy Runtime/Executable DSL. Use Runtime.Plan executables and TriggerPlanExecutableDsl.Scheduled/Periodic instead.")]
        public static IScheduledExecutable Buff(
            ISimpleExecutable applyEffect,
            float durationMs,
            ModifierData[] modifiers,
            params string[] tags)
        {
            var decorated = new DecoratorBuilder(applyEffect)
                .WithTags(tags)
                .WithDuration(durationMs)
                .WithModifiers(modifiers)
                .Build();
            return ScheduledExecutableFactory.WrapPeriodic(decorated, 1f);
        }

        /// <summary>
        /// Aura: 被动光环 (无限时间)
        /// </summary>
        [Obsolete("Aura belongs to legacy Runtime/Executable DSL. Use Runtime.Plan executables and TriggerPlanExecutableDsl.Scheduled/Periodic instead.")]
        public static IScheduledExecutable Aura(
            ISimpleExecutable effect,
            ModifierData[] modifiers,
            params string[] tags)
        {
            var decorated = new DecoratorBuilder(effect)
                .WithTags(tags)
                .WithDuration(-1)
                .WithModifiers(modifiers)
                .Build();
            return ScheduledExecutableFactory.WrapPeriodic(decorated, 100f);
        }
    }
}
