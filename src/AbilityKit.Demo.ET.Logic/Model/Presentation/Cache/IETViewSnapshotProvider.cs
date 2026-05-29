using System;
using System.Collections.Generic;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// View 快照提供器接口
    /// 用于 ET.View 层查询实体状态
    /// </summary>
    public interface IETViewSnapshotProvider
    {
        /// <summary>
        /// 获取当前帧号
        /// </summary>
        int CurrentFrame { get; }

        /// <summary>
        /// 获取缓存组件
        /// </summary>
        ETBattleEntityCacheComponent GetCacheComponent();

        /// <summary>
        /// 尝试获取实体变换快照
        /// </summary>
        bool TryGetTransformSnapshot(int actorId, out TransformSnapshot snapshot);

        /// <summary>
        /// 尝试获取实体 HP 快照
        /// </summary>
        bool TryGetHpSnapshot(int actorId, out HpSnapshot snapshot);

        /// <summary>
        /// 获取所有实体变换快照
        /// </summary>
        IReadOnlyList<TransformSnapshot> GetAllTransformSnapshots();

        /// <summary>
        /// 获取所有实体 HP 快照
        /// </summary>
        IReadOnlyList<HpSnapshot> GetAllHpSnapshots();

        /// <summary>
        /// 检查实体是否存在
        /// </summary>
        bool HasEntity(int actorId);

        /// <summary>
        /// 检查实体是否死亡
        /// </summary>
        bool IsEntityDead(int actorId);
    }

    /// <summary>
    /// 变换快照
    /// </summary>
    public struct TransformSnapshot
    {
        public int ActorId;
        public float X;
        public float Y;
        public float Rotation;
        public float RenderX;
        public float RenderY;
        public bool IsDead;
    }

    /// <summary>
    /// HP 快照
    /// </summary>
    public struct HpSnapshot
    {
        public int ActorId;
        public float Hp;
        public float MaxHp;
        public float HpPercent => MaxHp > 0 ? Hp / MaxHp : 0;
    }
}
