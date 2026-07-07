using System;
using AbilityKit.Coordinator.Core;

namespace AbilityKit.Coordinator.SubFeatures
{
    /// <summary>
    /// 会话事件 SubFeature。
    ///
    /// 设计：
    /// - 处理会话生命周期事件。
    /// - 管理状态转换。
    /// - 通知生命周期钩子。
    /// </summary>
    public sealed class SessionEventsSubFeature : ISessionLifecycleSubFeature
    {
        public string Name => "SessionEvents";
        public int Priority => 1000; // 最高优先级，事件应最先触发。

        private ISessionHost _host;
        private SessionState _lastState;

        public void OnAttach(ISessionHost host)
        {
            _host = host;
            _lastState = host.State;

            // 订阅状态变化。
            _host.Hooks.OnSessionStarting += (config) => OnSessionStarting();
            _host.Hooks.OnSessionStarted += OnSessionStarted;
            _host.Hooks.OnSessionStopping += OnSessionStopping;
            _host.Hooks.OnSessionStopped += OnSessionStopped;
        }

        public void OnDetach()
        {
            if (_host == null) return;

            _host.Hooks.OnSessionStarting -= (config) => OnSessionStarting();
            _host.Hooks.OnSessionStarted -= OnSessionStarted;
            _host.Hooks.OnSessionStopping -= OnSessionStopping;
            _host.Hooks.OnSessionStopped -= OnSessionStopped;

            _host = null;
        }

        public void OnTick(float deltaTime) { }

        public void OnSessionStarting()
        {
            _lastState = SessionState.Initializing;
        }

        public void OnSessionStopping()
        {
            _lastState = SessionState.Stopping;
        }

        private void OnSessionStarted(SessionConfig config)
        {
            _lastState = SessionState.Running;
        }

        private void OnSessionStopped()
        {
            _lastState = SessionState.Stopped;
        }
    }
}
