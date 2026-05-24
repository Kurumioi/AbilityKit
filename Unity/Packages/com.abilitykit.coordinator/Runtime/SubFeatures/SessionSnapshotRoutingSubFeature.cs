using System;
using System.Collections.Generic;
using AbilityKit.Coordinator.Core;

namespace AbilityKit.Coordinator.SubFeatures
{
    /// <summary>
    /// Session Snapshot Routing SubFeature
    ///
    /// Design:
    /// - Handles frame snapshot routing
    /// - Updates view timeline with entity states
    /// - Manages entity spawn/despawn events
    /// </summary>
    public sealed class SessionSnapshotRoutingSubFeature : ISessionSubFeature
    {
        public string Name => "SnapshotRouting";
        public int Priority => 300; // Medium priority

        private ISessionHost _host;
        private readonly HashSet<int> _knownEntities = new();
        private int _lastFrame;

        public void OnAttach(ISessionHost host)
        {
            _host = host;
            _knownEntities.Clear();
            _lastFrame = 0;

            // Subscribe to snapshot events
            _host.Hooks.OnFirstFrameReceived += OnFirstFrameReceived;
        }

        public void OnDetach()
        {
            if (_host == null) return;

            _host.Hooks.OnFirstFrameReceived -= OnFirstFrameReceived;

            _host = null;
            _knownEntities.Clear();
        }

        public void OnTick(float deltaTime) { }

        private void OnFirstFrameReceived()
        {
            _knownEntities.Clear();
            _lastFrame = 0;
        }

        /// <summary>
        /// Route snapshot to view timeline
        /// </summary>
        public void RouteSnapshot(int frame, EntityState[] states, double timeSeconds)
        {
            if (states == null) return;

            foreach (var state in states)
            {
                bool isNew = !_knownEntities.Contains(state.EntityId);

                if (isNew)
                {
                    _knownEntities.Add(state.EntityId);
                    // TODO: Trigger entity spawn event
                }

                if (state.IsDead && _knownEntities.Contains(state.EntityId))
                {
                    _knownEntities.Remove(state.EntityId);
                    // TODO: Trigger entity death event
                }
            }

            _lastFrame = frame;
        }

        /// <summary>
        /// Get all known entity IDs
        /// </summary>
        public IReadOnlyCollection<int> GetKnownEntities() => _knownEntities;
    }
}
