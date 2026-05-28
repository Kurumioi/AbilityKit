using AbilityKit.Demo.Moba;
using AbilityKit.Demo.Moba.Components;

namespace AbilityKit.Demo.Moba.Events.Buff
{
    /// <summary>
    /// Buff 事件参数
    /// </summary>
    public struct BuffEventArgs
    {
        /// <summary>事件 ID</summary>
        public string EventId;

        /// <summary>来源 ActorId</summary>
        public int SourceActorId;

        /// <summary>目标 ActorId</summary>
        public int TargetActorId;

        /// <summary>BuffId</summary>
        public int BuffId;

        /// <summary>EffectId</summary>
        public int EffectId;

        /// <summary>阶段</summary>
        public string Stage;

        /// <summary>堆叠层数</summary>
        public int StackCount;

        /// <summary>持续时间（秒）</summary>
        public float DurationSeconds;

        /// <summary>移除原因</summary>
        public EffectSourceEndReason RemoveReason;

        /// <summary>来源上下文 ID</summary>
        public long SourceContextId;

        /// <summary>Buff 运行时</summary>
        public BuffRuntime Runtime;
    }
}
