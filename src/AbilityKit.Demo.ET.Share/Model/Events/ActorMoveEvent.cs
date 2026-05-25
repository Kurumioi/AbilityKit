using System;
using ET;

namespace ET.AbilityKit.Demo.ET.Share
{
    /// <summary>
    /// 单位移动事件
    /// </summary>
    public struct ActorMoveEvent : IEvent
    {
        public Type Type => typeof(ActorMoveEvent);

        /// <summary>
        /// 逻辑层（MobaCore）的 ActorId
        /// </summary>
        public int ActorId;

        /// <summary>
        /// X 坐标
        /// </summary>
        public float X;

        /// <summary>
        /// Y 坐标
        /// </summary>
        public float Y;
    }
}
