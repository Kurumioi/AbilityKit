namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    /// <summary>
    /// debug_log Action 的强类型参数。
    /// </summary>
    public readonly struct DebugLogArgs
    {
        /// <summary>
        /// 消息 ID；来自 TriggerPlanJsonDatabase 的 string 表。
        /// 传 0 表示不输出消息 ID，仅输出上下文信息。
        /// </summary>
        public readonly int MsgId;

        /// <summary>
        /// 是否输出完整上下文信息（dump）。
        /// </summary>
        public readonly bool Dump;

        public DebugLogArgs(int msgId, bool dump)
        {
            MsgId = msgId;
            Dump = dump;
        }

        public static DebugLogArgs Default => new DebugLogArgs(0, false);

        /// <summary>
        /// 无参数版本，仅输出上下文信息。
        /// </summary>
        public static DebugLogArgs Empty => default;
    }
}
