using System;
using System.Collections.Generic;
using ActorSnapshot = AbilityKit.Demo.Moba.Console.Battle.Sync.ActorStateSnapshot;

namespace AbilityKit.Demo.Moba.Console.View
{
    /// <summary>
    /// Console 视图绑定器接口
    /// 对标 moba.view 的 BattleViewBinder
    /// </summary>
    public interface IConsoleViewBinder : IDisposable
    {
        /// <summary>
        /// 渲染时间（秒）- 比逻辑时间滞后
        /// </summary>
        double RenderTime { get; set; }

        /// <summary>
        /// 是否启用插值
        /// </summary>
        bool InterpolationEnabled { get; set; }

        /// <summary>
        /// 回溯时间（秒）- 渲染时间比逻辑时间滞后的量
        /// </summary>
        float BackTimeSeconds { get; set; }

        /// <summary>
        /// Tick 率（帧/秒）
        /// </summary>
        int TickRate { get; set; }

        /// <summary>
        /// 同步实体状态
        /// </summary>
        void SyncActor(int actorId, ActorSnapshot snapshot, double logicTime);

        /// <summary>
        /// 每帧更新渲染位置
        /// </summary>
        void TickRender(float deltaTime, double logicTime);

        /// <summary>
        /// 获取实体的渲染位置
        /// </summary>
        bool TryGetRenderPosition(int actorId, out float x, out float y, out float z);

        /// <summary>
        /// 获取实体是否已死亡
        /// </summary>
        bool IsActorDead(int actorId);

        /// <summary>
        /// 获取所有实体的渲染位置
        /// </summary>
        IEnumerable<(int ActorId, float X, float Y, float Z, bool IsDead)> GetAllRenderPositions();

        /// <summary>
        /// 移除实体
        /// </summary>
        void RemoveActor(int actorId);

        /// <summary>
        /// 清除所有实体
        /// </summary>
        void Clear();

        /// <summary>
        /// 获取实体数量
        /// </summary>
        int Count { get; }
    }
}
