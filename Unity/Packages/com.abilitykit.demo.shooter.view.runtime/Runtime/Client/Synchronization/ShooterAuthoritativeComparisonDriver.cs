#nullable enable

using System;
using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Network.Runtime.Conditioning;
using AbilityKit.Network.Runtime.Sync;

namespace AbilityKit.Demo.Shooter.View
{
    /// <summary>
    /// 负责验收会话中的权威世界推进、Carrier 快照发布与 LagComp 历史采集。
    /// <see cref="ShooterAcceptanceSession"/> 只保留会话门面职责，具体的权威侧编排集中在这里。
    /// </summary>
    internal sealed class ShooterAuthoritativeComparisonDriver
    {
        private readonly IShooterClientSyncController _controller;
        private readonly ShooterBattleRuntimePort _authoritativeWorld;
        private readonly ShooterPresentationFacade? _authoritativePresentation;
        private readonly ShooterLagCompensationService _lagCompensation = new ShooterLagCompensationService();

        private ShooterCarrierNetworkLink _carrierNetworkLink;
        private SyncTimeAnchor _lastCarrierTimeAnchor;
        private double _networkElapsedSeconds;

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
            _carrierNetworkLink = new ShooterCarrierNetworkLink(_controller, networkProfile, networkSeed);
        }

        public NetworkConditioningStats Stats => _carrierNetworkLink.Stats;

        public ShooterSnapshotApplyResult LastApplyResult => _carrierNetworkLink.LastApplyResult;

        public SyncTimeAnchor LastCarrierTimeAnchor => _lastCarrierTimeAnchor;

        public ShooterLagCompensationTelemetry Telemetry => _lagCompensation.Telemetry;

        public ShooterLagCompensationEvaluation? LastLagCompensationEvaluation => _lagCompensation.LastEvaluation;

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
            _carrierNetworkLink = new ShooterCarrierNetworkLink(_controller, profile);
            _lagCompensation.Clear();
            _lastCarrierTimeAnchor = default;
            _networkElapsedSeconds = 0d;
        }

        public void Advance(int stepCount, float deltaSeconds)
        {
            if (stepCount <= 0)
            {
                return;
            }

            for (var i = 0; i < stepCount; i++)
            {
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

        private void PublishSnapshot(in SyncTimeAnchor anchor)
        {
            _lastCarrierTimeAnchor = anchor;
            var clockMs = (long)Math.Round(anchor.ElapsedSeconds * 1000d);
            var packed = _authoritativeWorld.ExportPackedSnapshot(worldId: 1UL, isFullSnapshot: true, authorityOverride: true);
            _carrierNetworkLink.PublishSnapshot(in packed, anchor.ElapsedSeconds);
            _carrierNetworkLink.Advance(clockMs);
        }
    }
}
