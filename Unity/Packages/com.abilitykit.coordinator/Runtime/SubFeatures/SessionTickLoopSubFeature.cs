using System;
using System.Diagnostics;
using AbilityKit.Coordinator.Core;

namespace AbilityKit.Coordinator.SubFeatures
{
    /// <summary>
    /// 会话 TickLoop SubFeature。
    ///
    /// 设计：
    /// - 管理主帧 tick 循环。
    /// - 按配置频率调用 World.Tick()。
    /// - 驱动会话推进。
    /// </summary>
    public sealed class SessionTickLoopSubFeature : ISessionSubFeature, ISessionPreTickSubFeature, ISessionPostTickSubFeature
    {
        public string Name => "TickLoop";
        public int Priority => 500; // 高优先级，tick 应较早执行。

        private ISessionHost _host;
        private double _lastTickTime;
        private bool _isRunning;
        private double _frameInterval;

        public bool IsRunning => _isRunning;

        public void OnAttach(ISessionHost host)
        {
            _host = host;
            _isRunning = false;
            _frameInterval = 1.0 / host.Config.TickRate;
            _lastTickTime = GetTimeSeconds();

            // 订阅生命周期事件。
            _host.Hooks.OnSessionStarted += HandleSessionStarted;
            _host.Hooks.OnSessionStopping += OnSessionStopping;
        }

        public void OnDetach()
        {
            if (_host == null) return;

            _host.Hooks.OnSessionStarted -= HandleSessionStarted;
            _host.Hooks.OnSessionStopping -= OnSessionStopping;

            _host = null;
            _isRunning = false;
        }

        public void OnTick(float deltaTime) { }

        public void OnPreTick(float deltaTime)
        {
            if (!_isRunning) return;

            double currentTime = GetTimeSeconds();
            if (currentTime - _lastTickTime >= _frameInterval)
            {
                _lastTickTime = currentTime;
                // Tick 由 session coordinator 处理。
            }
        }

        public void OnPostTick(float deltaTime) { }

        private void HandleSessionStarted(SessionConfig config)
        {
            _frameInterval = 1.0 / config.TickRate;
            _isRunning = true;
            _lastTickTime = GetTimeSeconds();
        }

        private void OnSessionStopping()
        {
            _isRunning = false;
        }

        private static double GetTimeSeconds()
        {
            return Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
        }
    }
}
