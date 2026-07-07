using System;

namespace AbilityKit.Coordinator.Timeline
{
    /// <summary>
    /// 视图时间线接口。
    ///
    /// 设计：
    /// - 管理实体插值状态。
    /// - 基于帧提供平滑插值渲染。
    /// - 支持回放时按帧跳转。
    /// </summary>
    public interface IViewTimeline : IDisposable
    {
        // ============== 属性 ==============

        /// <summary>
        /// 当前渲染时间，单位为秒。
        /// </summary>
        double RenderTimeSeconds { get; }

        /// <summary>
        /// 插值延迟（回退时间），单位为秒。
        /// </summary>
        double InterpolationBackTimeSeconds { get; set; }

        /// <summary>
        /// 时间线是否处于激活状态。
        /// </summary>
        bool IsActive { get; }

        // ============== 生命周期 ==============

        /// <summary>
        /// 启动时间线。
        /// </summary>
        void Start();

        /// <summary>
        /// 停止时间线。
        /// </summary>
        void Stop();

        /// <summary>
        /// 重置时间线。
        /// </summary>
        void Reset();

        // ============== 实体状态管理 ==============

        /// <summary>
        /// 为实体添加位置采样。
        /// </summary>
        void AddPositionSample(int entityId, double time, float x, float y, float z);

        /// <summary>
        /// 为实体添加旋转采样。
        /// </summary>
        void AddRotationSample(int entityId, double time, float rotation);

        /// <summary>
        /// 标记实体是否死亡。
        /// </summary>
        void SetEntityDead(int entityId, bool isDead);

        /// <summary>
        /// 移除实体状态。
        /// </summary>
        void RemoveEntity(int entityId);

        // ============== 插值更新 ==============

        /// <summary>
        /// 更新插值（每帧调用）。
        /// </summary>
        void UpdateInterpolation(double renderTime);

        // ============== 帧跳转 ==============

        /// <summary>
        /// 将所有实体跳转到指定帧。
        /// </summary>
        void SeekToFrame(int frame, float secondsPerFrame);

        // ============== 状态查询 ==============

        /// <summary>
        /// 获取插值后的渲染位置。
        /// </summary>
        bool TryGetRenderPosition(int entityId, out float x, out float y, out float z);

        /// <summary>
        /// 获取插值后的渲染旋转。
        /// </summary>
        bool TryGetRenderRotation(int entityId, out float rotation);

        /// <summary>
        /// 获取当前（已确认）位置。
        /// </summary>
        bool TryGetCurrentPosition(int entityId, out float x, out float y, out float z);
    }

    /// <summary>
    /// 实体插值状态。
    /// 存储用于插值的位置和旋转采样。
    /// </summary>
    public class EntityInterpolationState
    {
        /// <summary>
        /// 实体 ID。
        /// </summary>
        public int EntityId { get; set; }

        /// <summary>
        /// 位置采样缓冲区。
        /// </summary>
        public IVectorSampleBuffer PositionBuffer { get; set; }

        /// <summary>
        /// 旋转采样缓冲区。
        /// </summary>
        public ISampleBuffer RotationBuffer { get; set; }

        /// <summary>
        /// 当前（已确认）位置。
        /// </summary>
        public float CurrentX { get; set; }
        public float CurrentY { get; set; }
        public float CurrentZ { get; set; }

        /// <summary>
        /// 当前（已确认）旋转。
        /// </summary>
        public float CurrentRotation { get; set; }

        /// <summary>
        /// 渲染（插值后）位置。
        /// </summary>
        public float RenderX { get; set; }
        public float RenderY { get; set; }
        public float RenderZ { get; set; }

        /// <summary>
        /// 渲染（插值后）旋转。
        /// </summary>
        public float RenderRotation { get; set; }

        /// <summary>
        /// 实体是否死亡。
        /// </summary>
        public bool IsDead { get; set; }
    }

    /// <summary>
    /// 采样缓冲区接口。
    /// </summary>
    public interface ISampleBuffer
    {
        int Count { get; }
        void Add(double time, float value);
        bool TryEvaluate(double time, out float value);
        void Clear();
    }

    /// <summary>
    /// 向量采样缓冲区接口（用于位置）。
    /// </summary>
    public interface IVectorSampleBuffer
    {
        int Count { get; }
        void Add(double time, float x, float y, float z);
        bool TryEvaluate(double time, out float x, out float y, out float z);
        void Clear();
    }
}
