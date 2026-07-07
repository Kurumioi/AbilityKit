using System;

namespace AbilityKit.Coordinator
{
    /// <summary>
    /// 视图事件接收接口。
    ///
    /// 设计原则：
    /// - 框架定义契约，应用层解释数据。
    /// - 帧快照包含某一帧的全部数据。
    /// - 简单生命周期事件由框架定义。
    /// - 业务专属事件以原始数据传递，由应用层解释。
    ///
    /// 该接口是可选的。未设置时协调器仍可运行，但应用层需要通过 ILogicWorldDriverBridge 手动查询状态。
    /// </summary>
    public interface IViewEventSink
    {
        // ============== 帧快照事件 ==============

        /// <summary>
        /// 进入游戏快照（初始状态）到达时调用。
        /// 快照包含全部实体及其初始状态。
        ///
        /// 应用层应：
        /// - 为快照中的实体创建全部表现对象。
        /// - 根据配置初始化 UI 元素。
        /// - 缓存 EntityState 数据，供后续插值使用。
        /// </summary>
        /// <param name="snapshot">包含全部实体状态的帧快照。</param>
        void OnEnterGameSnapshot(in FrameSnapshotData snapshot);

        /// <summary>
        /// 角色变换快照到达时调用。
        /// 快照包含角色的位置/旋转变化。
        ///
        /// 应用层应：
        /// - 更新表现位置。
        /// - 将插值数据入队，用于平滑渲染。
        /// </summary>
        /// <param name="snapshot">包含变换数据的帧快照。</param>
        void OnActorTransformSnapshot(in FrameSnapshotData snapshot);

        /// <summary>
        /// 伤害事件快照到达时调用。
        /// 快照包含用于表现的伤害信息。
        ///
        /// 应用层应：
        /// - 显示伤害数字。
        /// - 播放受击效果。
        /// - 更新 HP 条。
        /// </summary>
        /// <param name="snapshot">包含伤害数据的帧快照。</param>
        void OnDamageEventSnapshot(in FrameSnapshotData snapshot);

        /// <summary>
        /// 帧同步完成时调用。
        /// 此时全部快照事件均已处理。
        ///
        /// 应用层应：
        /// - 刷新挂起的表现更新。
        /// - 启动下一帧插值。
        /// </summary>
        /// <param name="frame">已完成的帧号。</param>
        void OnFrameSyncComplete(int frame);

        // ============== 生命周期事件 ==============

        /// <summary>
        /// 战斗开始时调用。
        /// </summary>
        /// <param name="frame">起始帧号。</param>
        void OnBattleStart(int frame);

        /// <summary>
        /// 战斗结束时调用。
        /// </summary>
        /// <param name="frame">结束帧号。</param>
        /// <param name="winTeamId">获胜队伍 ID（0 表示平局）。</param>
        void OnBattleEnd(int frame, int winTeamId);

        // ============== 扩展事件 ==============

        /// <summary>
        /// 自定义事件（技能释放、Buff 等）到达时调用。
        /// eventType 字符串允许应用层筛选并处理自定义数据。
        ///
        /// 常见事件类型（由应用层定义）：
        /// - "SkillCast" - 技能已释放。
        /// - "BuffApply" - Buff 已应用。
        /// - "BuffRemove" - Buff 已移除。
        /// - "ProjectileSpawn" - 投射物已创建。
        /// - "ProjectileHit" - 投射物命中目标。
        ///
        /// 应用层应：
        /// - 根据 eventType 分派到具体事件处理。
        /// - 根据 eventType 反序列化 customData。
        /// </summary>
        /// <param name="eventType">应用层定义的事件类型标识。</param>
        /// <param name="entityId">与该事件相关的主实体 ID。</param>
        /// <param name="customData">事件专属数据（格式由应用层决定）。</param>
        void OnCustomEvent(string eventType, int entityId, byte[] customData);
    }
}
