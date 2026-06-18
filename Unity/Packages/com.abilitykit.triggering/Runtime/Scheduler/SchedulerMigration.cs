using AbilityKit.Triggering.Runtime.RuleScheduler;
using LegacySchedulerConfig = AbilityKit.Triggering.Runtime.Scheduler.SchedulerConfig;
using LegacyScheduleMode = AbilityKit.Triggering.Runtime.Scheduler.EScheduleMode;

namespace AbilityKit.Triggering.Runtime.Scheduler
{
    /// <summary>
    /// Runtime/Scheduler 兼容配置到正式调度入口的迁移辅助。
    ///
    /// 该类型不执行旧调度器，也不把旧 SchedulerRegistry 接回主线；它只提供可测试、可复用的语义映射，
    /// 帮助旧配置迁移到 RuleScheduler 或 ActionScheduler 等正式入口。
    /// </summary>
    public static class SchedulerMigration
    {
        public const string TriggerPlanActionSchedulingRuntime = "Runtime.ActionScheduler";
        public const string RuleSchedulingRuntime = "Runtime.RuleScheduler";
        public const string GenericHandleSchedulingRuntime = "Runtime.Schedule";

        /// <summary>
        /// 将旧 SchedulerConfig 映射为规则级时间计划。
        /// </summary>
        public static RuleSchedulePlan ToRuleSchedulePlan(
            in LegacySchedulerConfig config,
            string groupId = null,
            string subjectId = null,
            string label = null,
            bool replaceExisting = false)
        {
            switch (config.Mode)
            {
                case LegacyScheduleMode.Immediate:
                    return new RuleSchedulePlan(
                        ERuleScheduleMode.Immediate,
                        maxOccurrences: 1,
                        groupId: groupId,
                        subjectId: subjectId,
                        label: label,
                        canBeInterrupted: config.CanBeInterrupted,
                        replaceExisting: replaceExisting);

                case LegacyScheduleMode.Delayed:
                    return new RuleSchedulePlan(
                        ERuleScheduleMode.Delayed,
                        delayMs: config.DelayMs,
                        maxOccurrences: 1,
                        groupId: groupId,
                        subjectId: subjectId,
                        label: label,
                        canBeInterrupted: config.CanBeInterrupted,
                        replaceExisting: replaceExisting);

                case LegacyScheduleMode.Periodic:
                    return new RuleSchedulePlan(
                        ERuleScheduleMode.Every,
                        delayMs: config.DelayMs,
                        intervalMs: config.IntervalMs,
                        maxOccurrences: config.MaxExecutions,
                        groupId: groupId,
                        subjectId: subjectId,
                        label: label,
                        canBeInterrupted: config.CanBeInterrupted,
                        replaceExisting: replaceExisting);

                case LegacyScheduleMode.Continuous:
                    return new RuleSchedulePlan(
                        ERuleScheduleMode.WhileActive,
                        delayMs: config.DelayMs,
                        intervalMs: config.IntervalMs,
                        maxOccurrences: -1,
                        groupId: groupId,
                        subjectId: subjectId,
                        label: label,
                        canBeInterrupted: config.CanBeInterrupted,
                        replaceExisting: replaceExisting);

                default:
                    return new RuleSchedulePlan(
                        ERuleScheduleMode.Immediate,
                        maxOccurrences: 1,
                        groupId: groupId,
                        subjectId: subjectId,
                        label: label,
                        canBeInterrupted: config.CanBeInterrupted,
                        replaceExisting: replaceExisting);
            }
        }

        /// <summary>
        /// 按旧 SchedulerConfig 的语义给出推荐正式运行时。
        /// </summary>
        public static string GetRecommendedRuntime(in LegacySchedulerConfig config, bool isTriggerPlanAction = false)
        {
            if (isTriggerPlanAction)
                return TriggerPlanActionSchedulingRuntime;

            switch (config.Mode)
            {
                case LegacyScheduleMode.Immediate:
                case LegacyScheduleMode.Delayed:
                case LegacyScheduleMode.Periodic:
                case LegacyScheduleMode.Continuous:
                    return RuleSchedulingRuntime;
                default:
                    return GenericHandleSchedulingRuntime;
            }
        }
    }
}
