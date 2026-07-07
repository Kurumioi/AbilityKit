#nullable enable

using System;
using System.Collections.Generic;
using AbilityKit.Network.Runtime.Sync;

namespace AbilityKit.Demo.Shooter.View
{
    internal sealed class ShooterClientRecoveryCoordinator
    {
        private readonly Func<int> _getCurrentFrame;
        private readonly ShooterFastReconnectDriver _fastReconnect;
        private SyncHealthEvent[] _lastHealthEvents = Array.Empty<SyncHealthEvent>();
        private ShooterClientRecoveryState _state = ShooterClientRecoveryState.Normal;

        public ShooterClientRecoveryCoordinator(ShooterClientDriftRecoveryPolicy policy, Func<int> getCurrentFrame)
        {
            _getCurrentFrame = getCurrentFrame ?? throw new ArgumentNullException(nameof(getCurrentFrame));
            _fastReconnect = new ShooterFastReconnectDriver(policy.ReplayThreshold);
        }

        public ShooterClientRecoveryState State => _state;

        public bool NeedsFullSnapshotResync { get; private set; }

        public FastReconnectPhase FastReconnectPhase => _fastReconnect.Phase;

        public IReadOnlyList<SyncHealthEvent> LastFastReconnectHealthEvents => _lastHealthEvents;

        public ShooterClientResyncReason LastResyncReason { get; private set; } = ShooterClientResyncReason.None;

        public int LastResyncClientFrame { get; private set; }

        public int LastResyncAuthoritativeFrame { get; private set; }

        public uint LastResyncClientStateHash { get; private set; }

        public uint LastResyncAuthoritativeStateHash { get; private set; }

        public int CatchUpTargetFrame { get; private set; }

        public void SetState(ShooterClientRecoveryState next)
        {
            var previous = _state;
            _state = next;
            if (previous == next)
            {
                return;
            }

            DriveFastReconnect(next);
        }

        public void EnterCatchUp(int authoritativeFrame)
        {
            CatchUpTargetFrame = authoritativeFrame;
            LastResyncAuthoritativeFrame = authoritativeFrame;
            SetState(ShooterClientRecoveryState.CatchUp);
        }

        public void MarkFullSnapshotResyncNeeded(
            ShooterClientResyncReason reason,
            int clientFrame,
            int authoritativeFrame,
            uint clientStateHash,
            uint authoritativeStateHash)
        {
            NeedsFullSnapshotResync = true;
            LastResyncReason = reason;
            LastResyncClientFrame = clientFrame;
            LastResyncAuthoritativeFrame = authoritativeFrame;
            LastResyncClientStateHash = clientStateHash;
            LastResyncAuthoritativeStateHash = authoritativeStateHash;
            CatchUpTargetFrame = authoritativeFrame > clientFrame ? authoritativeFrame : clientFrame;
            SetState(ShooterClientRecoveryState.AwaitingFullSnapshot);
        }

        public void ClearFullSnapshotResync()
        {
            NeedsFullSnapshotResync = false;
            LastResyncReason = ShooterClientResyncReason.None;
            LastResyncClientFrame = 0;
            LastResyncAuthoritativeFrame = 0;
            LastResyncClientStateHash = 0u;
            LastResyncAuthoritativeStateHash = 0u;
            CatchUpTargetFrame = 0;
        }

        public void HeartbeatFastReconnect(int authoritativeFrame)
        {
            _fastReconnect.ResetEventBuffer();
            _fastReconnect.Heartbeat(authoritativeFrame);
            CaptureHealthEvents();
        }

        private void DriveFastReconnect(ShooterClientRecoveryState next)
        {
            _fastReconnect.ResetEventBuffer();
            switch (next)
            {
                case ShooterClientRecoveryState.CatchUp:
                {
                    var currentFrame = _getCurrentFrame();
                    var gap = CatchUpTargetFrame > currentFrame ? CatchUpTargetFrame - currentFrame : 1;
                    _fastReconnect.Reconcile(FastReconnectPhase.Resuming, LastResyncAuthoritativeFrame, gap);
                    break;
                }
                case ShooterClientRecoveryState.AwaitingFullSnapshot:
                {
                    var gap = LastResyncAuthoritativeFrame - LastResyncClientFrame;
                    _fastReconnect.Reconcile(FastReconnectPhase.AwaitingFullSnapshot, LastResyncAuthoritativeFrame, gap);
                    break;
                }
                case ShooterClientRecoveryState.ApplyingFullSnapshot:
                    break;
                case ShooterClientRecoveryState.Recovered:
                    _fastReconnect.Reconcile(FastReconnectPhase.Recovered, LastResyncAuthoritativeFrame, 0);
                    break;
                case ShooterClientRecoveryState.Normal:
                default:
                    _fastReconnect.Reconcile(FastReconnectPhase.Connected, _getCurrentFrame(), 0);
                    break;
            }

            CaptureHealthEvents();
        }

        private void CaptureHealthEvents()
        {
            var collected = _fastReconnect.CollectedEvents;
            if (collected.Count == 0)
            {
                _lastHealthEvents = Array.Empty<SyncHealthEvent>();
                return;
            }

            var buffer = new SyncHealthEvent[collected.Count];
            for (var i = 0; i < collected.Count; i++)
            {
                buffer[i] = collected[i];
            }

            _lastHealthEvents = buffer;
        }
    }
}
