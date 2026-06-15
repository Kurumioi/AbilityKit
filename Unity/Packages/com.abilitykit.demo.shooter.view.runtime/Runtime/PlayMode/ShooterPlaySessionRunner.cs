#nullable enable

using System;
using System.Collections.Generic;
using AbilityKit.Demo.Shooter.View;
using AbilityKit.Demo.Shooter.View.Hosting;
using AbilityKit.Demo.Shooter.View.Network;
using AbilityKit.Network.Runtime.Conditioning;
using AbilityKit.Protocol.Shooter;

namespace AbilityKit.Demo.Shooter.View.PlayMode
{
    public sealed class ShooterPlaySessionRunner : IDisposable
    {
        private readonly IShooterHostInputSource _inputSource;
        private readonly IShooterHostViewSink _viewSink;
        private ShooterAcceptanceSession? _session;
        private ShooterPlayModeSessionOptions _options;
        private float _accumulator;

        public ShooterPlaySessionRunner(IShooterHostInputSource inputSource, IShooterHostViewSink viewSink)
        {
            _inputSource = inputSource ?? throw new ArgumentNullException(nameof(inputSource));
            _viewSink = viewSink ?? throw new ArgumentNullException(nameof(viewSink));
            _options = ShooterPlayModeSessionOptions.Default;
        }

        public event Action<ShooterAcceptanceSession?>? SessionChanged;

        public bool IsRunning => _session != null;
        public ShooterAcceptanceSession? Session => _session;
        public ShooterPlayModeSessionOptions Options => _options;

        public ShooterAcceptanceSession Start(ShooterPlayModeSessionOptions options)
        {
            Stop();

            _options = options.Normalized();

            var profile = CreateProfile(_options);
            var players = new List<ShooterStartPlayer>(_options.PlayerCount);
            for (var i = 0; i < _options.PlayerCount; i++)
            {
                players.Add(new ShooterStartPlayer(i + 1, $"P{i + 1}", i * 4f, 0f));
            }

            _session = ShooterAcceptanceLab.Create(
                _options.SyncModel,
                profile,
                networkName: _options.NetworkName,
                tickRate: _options.TickRate,
                players: players,
                randomSeed: _options.RandomSeed,
                enableAuthoritativeWorld: _options.EnableAuthoritativeWorld);

            ShooterNetworkConditionRegistry.Builtin.ApplyProfile(profile);
            _accumulator = 0f;
            SessionChanged?.Invoke(_session);
            return _session;
        }

        public void Stop()
        {
            if (_session == null)
            {
                _viewSink.Clear();
                return;
            }

            var session = _session;
            _session = null;
            _accumulator = 0f;
            session.Dispose();
            _viewSink.Clear();
            SessionChanged?.Invoke(null);
        }

        public void Tick(float deltaSeconds)
        {
            if (_session == null)
            {
                return;
            }

            var tickInterval = 1f / _options.TickRate;
            _accumulator += Math.Max(0f, deltaSeconds);

            var guard = 0;
            while (_accumulator >= tickInterval && guard++ < 8)
            {
                _accumulator -= tickInterval;
                StepOnce(tickInterval);
            }

            RenderLatest();
        }

        public void ApplyNetwork(NetworkConditionProfile profile)
        {
            _session?.ApplyNetwork(profile);
        }

        public void Dispose()
        {
            Stop();
            SessionChanged = null;
        }

        private void StepOnce(float deltaSeconds)
        {
            if (_session == null)
            {
                return;
            }

            var input = _inputSource.ReadInput(_options.ControlledPlayerId);
            var command = ShooterClientInputBuilder.CreateCommand(
                _options.ControlledPlayerId,
                input.MoveX,
                input.MoveY,
                input.AimX,
                input.AimY,
                input.Fire);

            _session.Controller.SubmitLocalInput(in command);
            _session.Controller.Tick(deltaSeconds);

            if (_session.HasAuthoritativeWorld)
            {
                _session.TickAuthoritativeWorld(deltaSeconds);
            }

            var snapshot = _session.Runtime.GetSnapshot();
            _session.Presentation.ApplyLocalPredictionSnapshot(in snapshot);
        }

        private void RenderLatest()
        {
            if (_session == null)
            {
                return;
            }

            var frame = new ShooterHostPresentationFrame(
                _session.Presentation.ViewModel.Current,
                _session.AuthoritativePresentation?.ViewModel.Current ?? ShooterSnapshotViewBatch.Empty,
                _session.HasAuthoritativeWorld && _session.AuthoritativePresentation != null,
                _options.ControlledPlayerId,
                _options.WorldScale,
                _session.CarrierNetworkStats,
                _session.LastCarrierSnapshotApplyResult,
                _session.LastCarrierTimeAnchor,
                _session.LagCompensationTelemetry);
            _viewSink.Render(in frame);
        }

        private static NetworkConditionProfile CreateProfile(ShooterPlayModeSessionOptions options)
        {
            return new NetworkConditionProfile(
                options.LatencyMs,
                options.JitterMs,
                options.PacketLossRate,
                options.ReorderRate,
                options.BandwidthKbps);
        }
    }
}
