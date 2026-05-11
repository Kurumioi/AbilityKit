using System;
using System.Collections.Generic;
using AbilityKit.Triggering.Runtime.Scheduler;

namespace AbilityKit.Triggering.Samples.Scheduler
{
    /// <summary>
    /// Buff 调度数据示例
    /// 展示如何为 Buff 系统定义具体的调度参数
    ///
    /// 【Samples】此类型仅用于演示如何构建项目特定的调度配置
    /// </summary>
    [Serializable]
    public class BuffScheduleData
    {
        /// <summary>Buff 唯一ID</summary>
        public int BuffId;

        /// <summary>Buff 名称</summary>
        public string BuffName;

        /// <summary>Buff 类型</summary>
        public EBuffType BuffType;

        /// <summary>调度配置</summary>
        public SchedulerConfig ScheduleConfig;

        /// <summary>效果数值（如伤害值、治疗值）</summary>
        public float EffectValue;

        /// <summary>效果类型</summary>
        public EEffectType EffectType;
    }

    /// <summary>
    /// Buff 类型枚举
    /// </summary>
    public enum EBuffType
    {
        None,
        DamageOverTime,    // 持续伤害 (DoT)
        HealOverTime,      // 持续治疗 (HoT)
        SpeedBoost,        // 加速
        Slow,              // 减速
        Stun,              // 眩晕
        Silence,           // 沉默
        Shield,            // 护盾
        Invincible,        // 无敌
        Reflect,           // 反射
    }

    /// <summary>
    /// 效果类型枚举
    /// </summary>
    public enum EEffectType
    {
        None,
        PhysicalDamage,
        MagicDamage,
        TrueDamage,
        Heal,
        Shield,
        Buff,
        Debuff,
    }

    /// <summary>
    /// Buff 调度预设配置
    /// 提供常见的 Buff 调度预设
    ///
    /// 【Samples】此类型仅用于演示如何构建项目特定的调度预设
    /// </summary>
    public static class BuffSchedulePresets
    {
        /// <summary>
        /// 持续伤害 (DoT) 预设
        /// 每秒造成一次伤害，持续 N 秒
        /// </summary>
        public static SchedulerConfig DamageOverTime(float intervalMs = 1000f, int maxTicks = 5)
        {
            return SchedulerConfig.Periodic(intervalMs, maxTicks);
        }

        /// <summary>
        /// 持续治疗 (HoT) 预设
        /// 每秒治疗一次，持续 N 秒
        /// </summary>
        public static SchedulerConfig HealOverTime(float intervalMs = 1000f, int maxTicks = 5)
        {
            return SchedulerConfig.Periodic(intervalMs, maxTicks);
        }

        /// <summary>
        /// 延迟伤害预设
        /// 延迟 N 毫秒后造成一次伤害
        /// </summary>
        public static SchedulerConfig DelayedDamage(float delayMs)
        {
            return SchedulerConfig.Delayed(delayMs);
        }

        /// <summary>
        /// 护盾预设
        /// 持续 N 毫秒的护盾效果
        /// </summary>
        public static SchedulerConfig Shield(float durationMs)
        {
            return SchedulerConfig.Continuous(0, durationMs);
        }

        /// <summary>
        /// 无敌预设
        /// 持续 N 毫秒的无敌效果
        /// </summary>
        public static SchedulerConfig Invincible(float durationMs)
        {
            return SchedulerConfig.Continuous(0, durationMs);
        }
    }

    /// <summary>
    /// Buff 调度器管理器示例
    /// 展示如何使用 SchedulerRegistry 管理 Buff 调度
    ///
    /// 【Samples】此类型仅用于演示如何构建项目特定的调度管理器
    /// </summary>
    public sealed class BuffSchedulerManager
    {
        private readonly ISchedulerRegistry _registry;

        /// <summary>
        /// 已注册的 Buff 数据
        /// </summary>
        private readonly Dictionary<int, BuffScheduleData> _buffs = new();

        public BuffSchedulerManager() : this(new SchedulerRegistry())
        {
        }

        public BuffSchedulerManager(ISchedulerRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <summary>
        /// 注册一个 Buff
        /// </summary>
        public SchedulerHandle RegisterBuff(BuffScheduleData data, Action<object> onTick)
        {
            _buffs[data.BuffId] = data;

            return _registry.CreateScheduler(
                schedulerId: data.BuffId,
                businessId: data.BuffId,
                triggerId: 0,
                config: data.ScheduleConfig,
                actionCallback: onTick,
                context: data);
        }

        /// <summary>
        /// 移除一个 Buff
        /// </summary>
        public bool RemoveBuff(int buffId)
        {
            var handles = _registry.FindByBusinessId(buffId);
            bool removed = false;

            foreach (var scheduler in handles)
            {
                if (_registry.Remove(scheduler.Handle))
                {
                    removed = true;
                }
            }

            if (removed)
            {
                _buffs.Remove(buffId);
            }

            return removed;
        }

        /// <summary>
        /// 获取 Buff 数据
        /// </summary>
        public bool TryGetBuff(int buffId, out BuffScheduleData data)
        {
            return _buffs.TryGetValue(buffId, out data);
        }

        /// <summary>
        /// 更新所有 Buff 调度器
        /// </summary>
        public void Update(float deltaTimeMs)
        {
            _registry.Update(deltaTimeMs);
        }

        /// <summary>
        /// 暂停所有 Buff
        /// </summary>
        public void PauseAll()
        {
            _registry.PauseAll();
        }

        /// <summary>
        /// 恢复所有 Buff
        /// </summary>
        public void ResumeAll()
        {
            _registry.ResumeAll();
        }

        /// <summary>
        /// 清除所有 Buff
        /// </summary>
        public void Clear()
        {
            _registry.Clear();
            _buffs.Clear();
        }
    }
}
