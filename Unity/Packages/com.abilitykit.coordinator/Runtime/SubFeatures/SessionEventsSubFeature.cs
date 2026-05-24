using System;
using AbilityKit.Coordinator.Core;

namespace AbilityKit.Coordinator.SubFeatures
{
    /// <summary>
    /// Session Events SubFeature
    ///
    /// Design:
    /// - Handles session lifecycle events
    /// - Manages state transitions
    /// - Notifies hooks of lifecycle changes
    /// </summary>
    public sealed class SessionEventsSubFeature : ISessionLifecycleSubFeature
    {
        public string Name => "SessionEvents";
        public int Priority => 1000; // Highest priority - events should fire first

        private ISessionHost _host;
        private SessionState _lastState;

        public void OnAttach(ISessionHost host)
        {
            _host = host;
            _lastState = host.State;

            // Subscribe to state changes
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
