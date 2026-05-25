using System;
using ET;

namespace ET.AbilityKit.Demo.ET.Share
{
    /// <summary>
    /// 单位死亡事件
    /// </summary>
    public struct ActorDeadEvent : IEvent
    {
        public Type Type => typeof(ActorDeadEvent);

        /// <summary>
        /// 逻辑层（MobaCore）的 ActorId
        /// </summary>
        public int ActorId;

        /// <summary>
        /// 击杀者 ActorId
        /// </summary>
        public int KillerId;
    }
}
