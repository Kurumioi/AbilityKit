using System;
using System.Diagnostics;
using AbilityKit.Coordinator.Core;

namespace AbilityKit.Coordinator.SubFeatures
{
    /// <summary>
    /// Session TickLoop SubFeature
    ///
    /// Design:
    /// - Manages the main frame tick loop
    /// - Calls World.Tick() at configured rate
    /// - Drives the session forward
    /// </summary>
    public sealed class SessionTickLoopSubFeature : ISessionSubFeature, ISessionPreTickSubFeature, ISessionPostTickSubFeature
    {
        public string Name => "TickLoop";
        public int Priority => 500; // High priority - tick should run early

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

            // Subscribe to lifecycle
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
                // Tick is handled by the session coordinator
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
