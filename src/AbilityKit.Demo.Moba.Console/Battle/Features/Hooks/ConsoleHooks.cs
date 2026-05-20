using System;

namespace AbilityKit.Demo.Moba.Console.Battle.Features
{
    /// <summary>
    /// Console Hooks 事件系统
    /// 提供表现层模块间的事件发布-订阅机制
    /// </summary>
    public sealed class ConsoleHooks : IDisposable
    {
        // 帧生命周期
        private Action<float>? _preTick;
        private Action<float>? _postTick;

        // 视图事件
        private Action? _onViewsRebound;
        private Action<int>? _onFrameAligned;

        // 会话生命周期
        private Action? _onSessionStarting;
        private Action? _onSessionStopped;
        private Action? _onSessionFailed;

        private bool _disposed;

        #region PreTick

        public Action<float> PreTick
        {
            get => _preTick ??= delegate { };
            set => _preTick = value;
        }

        public void AddPreTick(Action<float> handler)
        {
            _preTick += handler;
        }

        public void RemovePreTick(Action<float> handler)
        {
            _preTick -= handler;
        }

        #endregion

        #region PostTick

        public Action<float> PostTick
        {
            get => _postTick ??= delegate { };
            set => _postTick = value;
        }

        public void AddPostTick(Action<float> handler)
        {
            _postTick += handler;
        }

        public void RemovePostTick(Action<float> handler)
        {
            _postTick -= handler;
        }

        #endregion

        #region OnViewsRebound

        public Action OnViewsRebound
        {
            get => _onViewsRebound ??= delegate { };
            set => _onViewsRebound = value;
        }

        public void AddViewsReboundHandler(Action handler)
        {
            _onViewsRebound += handler;
        }

        public void RemoveViewsReboundHandler(Action handler)
        {
            _onViewsRebound -= handler;
        }

        #endregion

        #region OnFrameAligned

        public Action<int> OnFrameAligned
        {
            get => _onFrameAligned ??= delegate { };
            set => _onFrameAligned = value;
        }

        public void AddFrameAlignedHandler(Action<int> handler)
        {
            _onFrameAligned += handler;
        }

        public void RemoveFrameAlignedHandler(Action<int> handler)
        {
            _onFrameAligned -= handler;
        }

        #endregion

        #region Session Lifecycle

        public Action OnSessionStarting
        {
            get => _onSessionStarting ??= delegate { };
            set => _onSessionStarting = value;
        }

        public void AddSessionStartingHandler(Action handler)
        {
            _onSessionStarting += handler;
        }

        public Action OnSessionStopped
        {
            get => _onSessionStopped ??= delegate { };
            set => _onSessionStopped = value;
        }

        public void AddSessionStoppedHandler(Action handler)
        {
            _onSessionStopped += handler;
        }

        public Action OnSessionFailed
        {
            get => _onSessionFailed ??= delegate { };
            set => _onSessionFailed = value;
        }

        public void AddSessionFailedHandler(Action handler)
        {
            _onSessionFailed += handler;
        }

        #endregion

        public void InvokePreTick(float deltaTime)
        {
            _preTick?.Invoke(deltaTime);
        }

        public void InvokePostTick(float deltaTime)
        {
            _postTick?.Invoke(deltaTime);
        }

        public void InvokeViewsRebound()
        {
            _onViewsRebound?.Invoke();
        }

        public void InvokeFrameAligned(int frame)
        {
            _onFrameAligned?.Invoke(frame);
        }

        public void InvokeSessionStarting()
        {
            _onSessionStarting?.Invoke();
        }

        public void InvokeSessionStopped()
        {
            _onSessionStopped?.Invoke();
        }

        public void InvokeSessionFailed()
        {
            _onSessionFailed?.Invoke();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _preTick = null;
            _postTick = null;
            _onViewsRebound = null;
            _onFrameAligned = null;
            _onSessionStarting = null;
            _onSessionStopped = null;
            _onSessionFailed = null;
        }
    }
}
