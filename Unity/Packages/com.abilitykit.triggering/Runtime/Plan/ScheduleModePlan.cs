using AbilityKit.Triggering.Runtime.Config;

namespace AbilityKit.Triggering.Runtime.Plan
{
    /// <summary>
    /// 璋冨害妯″紡杩愯鏃舵暟鎹?
    /// </summary>
    public readonly struct ScheduleModePlan
    {
        public static ScheduleModePlan None => default;

        /// <summary>
        /// 璋冨害妯″紡
        /// </summary>
        public readonly EScheduleMode Mode;

        /// <summary>
        /// 璋冨害闂撮殧锛堟绉掞級锛? 琛ㄧず姣忔 Update 閮藉彲椹卞姩
        /// </summary>
        public readonly float IntervalMs;

        /// <summary>
        /// 鏈€澶ф墽琛屾鏁帮紝-1=鏃犻檺
        /// </summary>
        public readonly int MaxExecutions;

        /// <summary>
        /// 鏄惁鍙腑鏂?
        /// </summary>
        public readonly bool CanBeInterrupted;

        public ScheduleModePlan(EScheduleMode mode, float intervalMs = 0, int maxExecutions = -1, bool canBeInterrupted = true)
        {
            Mode = mode;
            IntervalMs = intervalMs;
            MaxExecutions = maxExecutions;
            CanBeInterrupted = canBeInterrupted;
        }

        /// <summary>
        /// 鍒涘缓澶栭儴鐢熷懡鍛ㄦ湡鎺у埗鐨勬寔缁皟搴﹁鍒掋€?
        /// </summary>
        public static ScheduleModePlan Continuous(float intervalMs = 0, int maxExecutions = -1, bool canBeInterrupted = true)
            => new ScheduleModePlan(EScheduleMode.Continuous, intervalMs, maxExecutions, canBeInterrupted);

        public static ScheduleModePlan Periodic(float intervalMs, int maxExecutions = -1)
            => new ScheduleModePlan(EScheduleMode.Periodic, intervalMs, maxExecutions, canBeInterrupted: true);

        public static ScheduleModePlan Timed(float delayMs)
            => new ScheduleModePlan(EScheduleMode.Timed, delayMs, 1, canBeInterrupted: true);
    }
}
