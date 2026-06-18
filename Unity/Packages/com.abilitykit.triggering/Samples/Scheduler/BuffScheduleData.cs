using System;
using System.Collections.Generic;
using AbilityKit.Triggering.Runtime.RuleScheduler;
using AbilityKit.Triggering.Runtime.Scheduler;

namespace AbilityKit.Triggering.Samples.Scheduler
{
    /// <summary>
    /// Buff 调度数据示例。
    /// 展示如何为 Buff 系统定义项目侧调度参数。
    ///
    /// ScheduleConfig 保留旧数据兼容字段；实际运行时调度通过 SchedulerMigration 转换到 RuleSchedulePlan。
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

        /// <summary>旧调度配置，运行前通过 SchedulerMigration 转换为 RuleSchedulePlan</summary>
        public SchedulerConfig ScheduleConfig;

        /// <summary>效果数值（如伤害值、治疗值）</summary>
        public float EffectValue;

        /// <summary>效果类型</summary>
        public EEffectType EffectType;
    }

    /// <summary>
    /// Buff 类型枚举。
    /// </summary>
    public enum EBuffType
    {
        None,
        DamageOverTime,
        HealOverTime,
        SpeedBoost,
        Slow,
        Stun,
        Silence,
        Shield,
        Invincible,
        Reflect,
    }

    /// <summary>
    /// 效果类型枚举。
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
    /// Buff 调度预设配置。
    /// 提供常见 Buff 调度预设，并通过 SchedulerMigration 迁移到正式 RuleScheduler。
    /// </summary>
    public static class BuffSchedulePresets
    {
        /// <summary>
        /// 持续伤害 (DoT) 预设：每秒造成一次伤害，持续 N 次。
        /// </summary>
        public static SchedulerConfig DamageOverTime(float intervalMs = 1000f, int maxTicks = 5)
        {
            return SchedulerConfig.Periodic(intervalMs, maxTicks);
        }

        /// <summary>
        /// 持续治疗 (HoT) 预设：每秒治疗一次，持续 N 次。
        /// </summary>
        public static SchedulerConfig HealOverTime(float intervalMs = 1000f, int maxTicks = 5)
        {
            return SchedulerConfig.Periodic(intervalMs, maxTicks);
        }

        /// <summary>
        /// 延迟伤害预设：延迟 N 毫秒后造成一次伤害。
        /// </summary>
        public static SchedulerConfig DelayedDamage(float delayMs)
        {
            return SchedulerConfig.Delayed(delayMs);
        }

        /// <summary>
        /// 护盾预设：持续 N 毫秒的护盾效果。
        /// </summary>
        public static SchedulerConfig Shield(float durationMs)
        {
            return SchedulerConfig.Continuous(0, durationMs);
        }

        /// <summary>
        /// 无敌预设：持续 N 毫秒的无敌效果。
        /// </summary>
        public static SchedulerConfig Invincible(float durationMs)
        {
            return SchedulerConfig.Continuous(0, durationMs);
        }
    }

    /// <summary>
    /// Buff 调度器管理器示例。
    /// 展示如何将旧 Buff SchedulerConfig 迁移到正式 RuleScheduler 管理。
    /// </summary>
    public sealed class BuffSchedulerManager
    {
        private readonly RuleSchedulerRegistry _registry;
        private readonly Dictionary<int, BuffScheduleData> _buffs = new Dictionary<int, BuffScheduleData>();
        private readonly Dictionary<int, RuleScheduleHandle> _handles = new Dictionary<int, RuleScheduleHandle>();

        public BuffSchedulerManager() : this(new RuleSchedulerRegistry())
        {
        }

        public BuffSchedulerManager(RuleSchedulerRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <summary>
        /// 注册一个 Buff。
        /// </summary>
        public RuleScheduleHandle RegisterBuff(BuffScheduleData data, Action<object> onTick)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (onTick == null) throw new ArgumentNullException(nameof(onTick));

            _buffs[data.BuffId] = data;

            var plan = SchedulerMigration.ToRuleSchedulePlan(
                in data.ScheduleConfig,
                groupId: FormatBuffGroupId(data.BuffId),
                subjectId: data.BuffType.ToString(),
                label: data.BuffName,
                replaceExisting: true);

            var handle = _registry.Schedule(in plan, new DelegateRuleScheduleEffect(_ => onTick(data)));
            _handles[data.BuffId] = handle;
            return handle;
        }

        /// <summary>
        /// 移除一个 Buff。
        /// </summary>
        public bool RemoveBuff(int buffId)
        {
            if (!_handles.TryGetValue(buffId, out var handle))
            {
                return false;
            }

            var removed = _registry.Cancel(handle);
            if (removed)
            {
                _handles.Remove(buffId);
                _buffs.Remove(buffId);
            }

            return removed;
        }

        /// <summary>
        /// 获取 Buff 数据。
        /// </summary>
        public bool TryGetBuff(int buffId, out BuffScheduleData data)
        {
            return _buffs.TryGetValue(buffId, out data);
        }

        /// <summary>
        /// 更新所有 Buff 调度器。
        /// </summary>
        public void Update(float deltaTimeMs)
        {
            _registry.Update(deltaTimeMs);
        }

        /// <summary>
        /// 暂停所有 Buff。
        /// </summary>
        public void PauseAll()
        {
            foreach (var handle in _handles.Values)
            {
                _registry.Pause(handle);
            }
        }

        /// <summary>
        /// 恢复所有 Buff。
        /// </summary>
        public void ResumeAll()
        {
            foreach (var handle in _handles.Values)
            {
                _registry.Resume(handle);
            }
        }

        /// <summary>
        /// 清除所有 Buff。
        /// </summary>
        public void Clear()
        {
            _registry.Clear();
            _handles.Clear();
            _buffs.Clear();
        }

        private static string FormatBuffGroupId(int buffId)
        {
            return "buff:" + buffId;
        }
    }
}
