using System;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// 单位视图数据
    /// 存储单位的表现层渲染数据
    /// </summary>
    public class UnitViewData
    {
        public long ActorId;
        public string Name;
        public ActorKind Kind;
        public float X;
        public float Y;
        public float Hp;
        public float MaxHp;
        public bool IsDead;
        public bool IsLocalPlayer;

        /// <summary>
        /// 插值渲染位置
        /// </summary>
        public float RenderX;
        public float RenderY;

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public float LastUpdateTime;
    }
}
