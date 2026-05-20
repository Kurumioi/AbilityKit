using System;
using System.Collections.Generic;

namespace AbilityKit.Demo.Moba.Console.View
{
    /// <summary>
    /// Console 帧可追溯视图接口
    /// 对标 Unity IFrameSeekableView
    /// </summary>
    public interface IConsoleFrameSeekableView
    {
        /// <summary>
        /// 获取实体 ID
        /// </summary>
        int EntityId { get; }

        /// <summary>
        /// 跳转到指定帧
        /// </summary>
        void SeekToFrame(int frame, float secondsPerFrame);

        /// <summary>
        /// 注册时间线
        /// </summary>
        void RegisterToTimeline(ConsoleViewTimeline timeline);

        /// <summary>
        /// 从时间线注销
        /// </summary>
        void UnregisterFromTimeline(ConsoleViewTimeline timeline);
    }

    /// <summary>
    /// Console 视图时间线
    /// 对标 Unity ViewTimeline
    /// 管理所有帧可追溯视图的帧同步
    /// </summary>
    public sealed class ConsoleViewTimeline : IDisposable
    {
        private readonly Dictionary<int, IConsoleFrameSeekableView> _seekableViews = new();
        private bool _disposed;

        /// <summary>
        /// 注册帧可追溯视图
        /// </summary>
        public void Register(IConsoleFrameSeekableView view)
        {
            if (view == null) return;
            if (_disposed) return;

            if (_seekableViews.ContainsKey(view.EntityId))
            {
                Platform.Log.Warn($"[ViewTimeline] View already registered: {view.EntityId}");
                return;
            }

            _seekableViews[view.EntityId] = view;
            view.RegisterToTimeline(this);
            Platform.Log.Trace($"[ViewTimeline] Registered view: {view.EntityId}");
        }

        /// <summary>
        /// 注销帧可追溯视图
        /// </summary>
        public void Unregister(IConsoleFrameSeekableView view)
        {
            if (view == null) return;
            if (_disposed) return;

            if (_seekableViews.Remove(view.EntityId))
            {
                view.UnregisterFromTimeline(this);
                Platform.Log.Trace($"[ViewTimeline] Unregistered view: {view.EntityId}");
            }
        }

        /// <summary>
        /// 注销指定 ID 的视图
        /// </summary>
        public void UnregisterById(int entityId)
        {
            if (_disposed) return;

            if (_seekableViews.TryGetValue(entityId, out var view))
            {
                _seekableViews.Remove(entityId);
                view.UnregisterFromTimeline(this);
                Platform.Log.Trace($"[ViewTimeline] Unregistered view by id: {entityId}");
            }
        }

        /// <summary>
        /// 所有视图跳转到指定帧
        /// </summary>
        public void SeekAll(int frame, float secondsPerFrame)
        {
            if (_disposed) return;

            foreach (var view in _seekableViews.Values)
            {
                try
                {
                    view.SeekToFrame(frame, secondsPerFrame);
                }
                catch (Exception ex)
                {
                    Platform.Log.Error($"[ViewTimeline] Seek failed for {view.EntityId}: {ex.Message}");
                }
            }

            Platform.Log.Trace($"[ViewTimeline] SeekAll to frame {frame}");
        }

        /// <summary>
        /// 获取视图数量
        /// </summary>
        public int Count => _seekableViews.Count;

        /// <summary>
        /// 是否包含指定视图
        /// </summary>
        public bool Contains(int entityId) => _seekableViews.ContainsKey(entityId);

        /// <summary>
        /// 获取视图
        /// </summary>
        public bool TryGetView(int entityId, out IConsoleFrameSeekableView view)
            => _seekableViews.TryGetValue(entityId, out view);

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var view in _seekableViews.Values)
            {
                try
                {
                    view.UnregisterFromTimeline(this);
                }
                catch (Exception ex)
                {
                    Platform.Log.Error($"[ViewTimeline] Unregister failed: {ex.Message}");
                }
            }

            _seekableViews.Clear();
            Platform.Log.View("[ViewTimeline] Disposed");
        }
    }
}
