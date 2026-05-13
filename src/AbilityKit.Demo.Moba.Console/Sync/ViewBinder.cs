using System;
using System.Collections.Generic;
using AbilityKit.Core.Math;

namespace AbilityKit.Demo.Moba.Console.Sync
{
    /// <summary>
    /// 视图绑定器
    /// 管理实体的插值采样和渲染位置
    /// </summary>
    public sealed class ViewBinder
    {
        private readonly Dictionary<int, SampleBuffer> _buffers = new();
        private readonly Dictionary<int, Vec3> _renderPositions = new();
        private readonly Dictionary<int, Vec3> _latestPositions = new();

        private double _renderTime;
        private int _lastRenderFrame = -1;

        public const float DefaultBackTime = 1f;

        /// <summary>
        /// 添加采样点
        /// </summary>
        public void Sample(int actorId, double time, in Vec3 pos)
        {
            if (!_buffers.TryGetValue(actorId, out var buffer))
            {
                buffer = new SampleBuffer();
                _buffers[actorId] = buffer;
            }

            buffer.Add(time, pos);
            _latestPositions[actorId] = pos;
        }

        /// <summary>
        /// 尝试获取插值后的位置
        /// </summary>
        public bool TryEvaluate(int actorId, double time, out Vec3 pos)
        {
            if (_buffers.TryGetValue(actorId, out var buffer))
            {
                return buffer.TryEvaluate(time, out pos);
            }

            pos = default;
            return false;
        }

        /// <summary>
        /// 获取最新位置
        /// </summary>
        public bool TryGetLatestPosition(int actorId, out Vec3 pos)
        {
            return _latestPositions.TryGetValue(actorId, out pos);
        }

        /// <summary>
        /// 插值 Tick
        /// </summary>
        public void TickInterpolation(double logicTime, int logicFrame, float backTime = DefaultBackTime, int tickRate = 30)
        {
            var fixedDelta = 1.0 / tickRate;
            var targetTime = logicTime - (fixedDelta * backTime);
            if (targetTime < 0) targetTime = 0;

            _renderTime = targetTime;
            _lastRenderFrame = logicFrame;

            foreach (var kvp in _buffers)
            {
                if (kvp.Value.TryEvaluate(_renderTime, out var pos))
                {
                    _renderPositions[kvp.Key] = pos;
                }
                else if (_latestPositions.TryGetValue(kvp.Key, out var latest))
                {
                    _renderPositions[kvp.Key] = latest;
                }
            }
        }

        /// <summary>
        /// 获取渲染位置
        /// </summary>
        public bool TryGetRenderPosition(int actorId, out Vec3 pos)
        {
            return _renderPositions.TryGetValue(actorId, out pos);
        }

        /// <summary>
        /// 获取当前渲染时间
        /// </summary>
        public double RenderTime => _renderTime;

        /// <summary>
        /// 获取上一个渲染帧
        /// </summary>
        public int LastRenderFrame => _lastRenderFrame;

        /// <summary>
        /// 清除所有数据
        /// </summary>
        public void Clear()
        {
            _buffers.Clear();
            _renderPositions.Clear();
            _latestPositions.Clear();
            _renderTime = 0;
            _lastRenderFrame = -1;
        }
    }
}
