#nullable enable

using System;
using System.Collections.Generic;
using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Network.Runtime.Conditioning;
using AbilityKit.Network.Runtime.Sync;
using AbilityKit.Protocol.Shooter;

namespace AbilityKit.Demo.Shooter.View
{
    /// <summary>
    /// 负责验收会话中的权威世界推进、Carrier 快照发布�?LagComp 历史采集�?
    /// <see cref="ShooterAcceptanceSession"/> 只保留会话门面职责，具体的权威侧编排集中在这里�?
    /// </summary>
    internal sealed class ShooterAuthoritativeComparisonDriver
    {
        private readonly IShooterClientSyncController _controller;
        private readonly ShooterBattleRuntimePort _authoritativeWorld;
        private readonly ShooterPresentationFacade? _authoritativePresentation;
        private readonly ShooterLagCompensationService _lagCompensation = new ShooterLagCompensationService();

        private readonly Queue<PendingAuthoritativeInput> _pendingInputs = new Queue<PendingAuthoritativeInput>();
        private readonly Random _inputRandom;
        private ShooterCarrierNetworkLink _carrierNetworkLink;
        private NetworkConditionProfile _networkProfile;
        private SyncTimeAnchor _lastCarrierTimeAnchor;
        private double _networkElapsedSeconds;
        private int _lastDeliveredInputCount;

        public ShooterAuthoritativeComparisonDriver(
            IShooterClientSyncController controller,
            ShooterBattleRuntimePort authoritativeWorld,
            ShooterPresentationFacade? authoritativePresentation,
            NetworkConditionProfile networkProfile,
            int networkSeed)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _authoritativeWorld = authoritativeWorld ?? throw new ArgumentNullException(nameof(authoritativeWorld));
            _authoritativePresentation = authoritativePresentation;
            _networkProfile = networkProfile;
            _inputRandom = new Random(networkSeed);
            _carrierNetworkLink = new ShooterCarrierNetworkLink(_controller, networkProfile, networkSeed);
        }

        public NetworkConditioningStats Stats => _carrierNetworkLink.Stats;

        public ShooterSnapshotApplyResult LastApplyResult => _carrierNetworkLink.LastApplyResult;

        public SyncTimeAnchor LastCarrierTimeAnchor => _lastCarrierTimeAnchor;

        public ShooterLagCompensationTelemetry Telemetry => _lagCompensation.Telemetry;

        public ShooterLagCompensationEvaluation? LastLagCompensationEvaluation => _lagCompensation.LastEvaluation;

        public int LastDeliveredInputCount => _lastDeliveredInputCount;

        public bool TryEvaluateShot(in ShooterLagCompensationShot shot, out ShooterLagCompensationEvaluation evaluation)
        {
            _lagCompensation.TryEvaluateShot(in shot, out _);
            if (_lagCompensation.LastEvaluation.HasValue)
            {
                evaluation = _lagCompensation.LastEvaluation.Value;
                return evaluation.Accepted;
            }

            evaluation = default;
            return false;
        }
 
        public void ApplyNetwork(NetworkConditionProfile profile)
        {
            _networkProfile = profile;
            _pendingInputs.Clear();
            _lastDeliveredInputCount = 0;
            _carrierNetworkLink = new ShooterCarrierNetworkLink(_controller, profile);
            _lagCompensation.Clear();
            _lastCarrierTimeAnchor = default;
            _networkElapsedSeconds = 0d;
        }

        public void EnqueueInput(int commandFrame, in ShooterPlayerCommand command)
        {
            if (_networkProfile.PacketLossRate > 0d && _inputRandom.NextDouble() < _networkProfile.PacketLossRate)
            {
                return;
            }

            var latencyMs = _networkProfile.BaseLatencyMs;
            if (_networkProfile.JitterMs > 0)
            {
                latencyMs += _inputRandom.Next(-_networkProfile.JitterMs, _networkProfile.JitterMs + 1);
            }

            var deliverAt = _networkElapsedSeconds + Math.Max(0, latencyMs) / 1000d;
            _pendingInputs.Enqueue(new PendingAuthoritativeInput(deliverAt, commandFrame, command));
        }

        public void Advance(int stepCount, float deltaSeconds)
        {
            if (stepCount <= 0)
            {
                return;
            }

            for (var i = 0; i < stepCount; i++)
            {
                _lastDeliveredInputCount = DeliverDueInputs();
                _authoritativeWorld.Tick(deltaSeconds);
                _lagCompensation.RecordFrame(_authoritativeWorld);
                _networkElapsedSeconds += deltaSeconds;
                var anchor = SyncTimeAnchor
                    .FromLocalFrame(_authoritativeWorld.CurrentFrame, _authoritativeWorld.CurrentFrame, _networkElapsedSeconds)
                    .WithAuthoritativeFrame(_authoritativeWorld.CurrentFrame);
                PublishSnapshot(in anchor);
            }

            if (_authoritativePresentation != null)
            {
                var authoritySnapshot = _authoritativeWorld.GetSnapshot();
                _authoritativePresentation.ApplyLocalPredictionSnapshot(in authoritySnapshot);
            }
        }

        private int DeliverDueInputs()
        {
            var delivered = 0;
            while (_pendingInputs.Count > 0 && _pendingInputs.Peek().DeliverAtSeconds <= _networkElapsedSeconds)
            {
                var pending = _pendingInputs.Dequeue();
                delivered += _authoritativeWorld.SubmitInput(pending.CommandFrame, new[] { pending.Command });
            }

            return delivered;
        }

        private void PublishSnapshot(in SyncTimeAnchor anchor)
        {
            _lastCarrierTimeAnchor = anchor;
            var clockMs = (long)Math.Round(anchor.ElapsedSeconds * 1000d);
            var packed = _authoritativeWorld.ExportPackedSnapshot(worldId: 1UL, isFullSnapshot: true, authorityOverride: true);
            _carrierNetworkLink.PublishSnapshot(in packed, anchor.ElapsedSeconds);
            _carrierNetworkLink.Advance(clockMs);
        }

        private readonly struct PendingAuthoritativeInput
        {
            public readonly double DeliverAtSeconds;
            public readonly int CommandFrame;
            public readonly ShooterPlayerCommand Command;

            public PendingAuthoritativeInput(double deliverAtSeconds, int commandFrame, in ShooterPlayerCommand command)
            {
                DeliverAtSeconds = deliverAtSeconds;
                CommandFrame = commandFrame;
                Command = command;
            }
        }
    }
}

