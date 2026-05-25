using System;
using ET;

namespace ET.AbilityKit.Demo.ET.Share
{
    /// <summary>
    /// 单位出生事件
    /// </summary>
    public struct ActorSpawnEvent : IEvent
    {
        public Type Type => typeof(ActorSpawnEvent);

        /// <summary>
        /// ET 框架实体 ID
        /// </summary>
        public long ActorId;

        /// <summary>
        /// 逻辑层（MobaCore）的 ActorId
        /// </summary>
        public int MobaActorId;

        /// <summary>
        /// 实体代码
        /// </summary>
        public int EntityCode;

        /// <summary>
        /// 单位种类
        /// </summary>
        public ActorKind Kind;

        /// <summary>
        /// 单位名称
        /// </summary>
        public string Name;

        /// <summary>
        /// X 坐标
        /// </summary>
        public float X;

        /// <summary>
        /// Y 坐标
        /// </summary>
        public float Y;

        /// <summary>
        /// 旋转角度
        /// </summary>
        public float Rotation;

        /// <summary>
        /// 缩放
        /// </summary>
        public float Scale;

        /// <summary>
        /// 队伍 ID
        /// </summary>
        public int TeamId;

        /// <summary>
        /// 最大 HP
        /// </summary>
        public float MaxHp;

        /// <summary>
        /// 是否是本地玩家
        /// </summary>
        public bool IsLocalPlayer;
    }
}
