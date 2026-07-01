#nullable enable

using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.FrameSync.Rollback;
using AbilityKit.Ability.Host.Extensions.Client.FrameSync;
using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Network.Runtime.Sync;
using AbilityKit.Protocol.Shooter;
using FrameworkPlayerInputCommand = AbilityKit.Ability.Host.PlayerInputCommand;

namespace AbilityKit.Demo.Shooter.View
{
    public sealed class ShooterClientFrameSyncController
    {
        private readonly IShooterBattleRuntimePort _runtime;
        private readonly ShooterPresentationFacade _presentation;
        private readonly ShooterGatewaySnapshotDecoder _gatewaySnapshotDecoder;
        private readonly ShooterFrameworkSnapshotPipeline _frameworkSnapshotPipeline;
        private readonly ClientPredictionReconciliationCoordinator<ShooterPlayerCommand> _predictionReconciliation = new ClientPredictionReconciliationCoordinator<ShooterPlayerCommand>();
        private readonly RollbackCoordinator _rollback;
        private readonly ShooterClientPredictionRuntimeAdapter _predictionRuntimeAdapter;
        private readonly InputHistoryRingBuffer _frameworkInputHistory;
        private readonly ClientPredictionRunner _predictionRunner;
        private readonly ShooterClientDriftRecoveryPolicy _recoveryPolicy;
        private readonly ShooterFastReconnectDriver _fastReconnect;
        private readonly float _fixedDeltaTime;
        private float _accumulator;
        private int _catchUpTargetFrame;
        private ShooterClientRecoveryState _recoveryState = ShooterClientRecoveryState.Normal;
        private SyncHealthEvent[] _lastHealthEvents = Array.Empty<SyncHealthEvent>();

        public ShooterClientFrameSyncController(IShooterBattleRuntimePort runtime, ShooterPresentationFacade presentation, int tickRate)
            : this(runtime, presentation, tickRate, null)
        {
        }

        public ShooterClientFrameSyncController(IShooterBattleRuntimePort runtime, ShooterPresentationFacade presentation, int tickRate, ShooterGatewaySnapshotDecoder? decoder)
            : this(runtime, presentation, tickRate, decoder, 0ul, 240)
        {
        }

        public ShooterClientFrameSyncController(IShooterBattleRuntimePort runtime, ShooterPresentationFacade presentation, int tickRate, ShooterGatewaySnapshotDecoder? decoder, ulong rollbackWorldId, int rollbackBufferFrames)
            : this(runtime, presentation, tickRate, decoder, rollbackWorldId, rollbackBufferFrames, ShooterClientDriftRecoveryPolicy.Default)
        {
        }

        public ShooterClientFrameSyncController(
            IShooterBattleRuntimePort runtime,
            ShooterPresentationFacade presentation,
            int tickRate,
            ShooterGatewaySnapshotDecoder? decoder,
            ulong rollbackWorldId,
            int rollbackBufferFrames,
            ShooterClientDriftRecoveryPolicy recoveryPolicy)
        {
            if (tickRate <= 0) throw new ArgumentOutOfRangeException(nameof(tickRate));
            if (rollbackBufferFrames <= 0) throw new ArgumentOutOfRangeException(nameof(rollbackBufferFrames));

            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            _presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
            _gatewaySnapshotDecoder = decoder ?? new ShooterGatewaySnapshotDecoder();
            _frameworkSnapshotPipeline = new ShooterFrameworkSnapshotPipeline(_runtime, _presentation);
            var registry = new RollbackRegistry();
            registry.Register(new ShooterPackedSnapshotRollbackProvider(_runtime, rollbackWorldId));
            _rollback = new RollbackCoordinator(registry, new RollbackSnapshotRingBuffer(rollbackBufferFrames));
            _predictionRuntimeAdapter = new ShooterClientPredictionRuntimeAdapter(_runtime);
            _frameworkInputHistory = new InputHistoryRingBuffer(rollbackBufferFrames);
            _predictionRunner = new ClientPredictionRunner(
                _predictionRuntimeAdapter,
                _predictionRuntimeAdapter,
                _rollback,
                _frameworkInputHistory,
                new ClientPredictionReconciler(new WorldStateHashRingBuffer(rollbackBufferFrames)));
            _recoveryPolicy = recoveryPolicy;
            _fastReconnect = new ShooterFastReconnectDriver(recoveryPolicy.ReplayThreshold);
            _fixedDeltaTime = 1f / tickRate;
        }

        public int CurrentFrame => _runtime.CurrentFrame;
        public float FixedDeltaTime => _fixedDeltaTime;
        public float AccumulatedTime => _accumulator;
        public int PendingInputFrameCount => _predictionReconciliation.PendingInputFrameCount;
        public ShooterSnapshotApplyResult LastSnapshotApplyResult { get; private set; } = ShooterSnapshotApplyResult.Ignored;
        public ShooterFrameworkSnapshotPipelineDiagnostics FrameworkSnapshotPipelineDiagnostics => _frameworkSnapshotPipeline.Diagnostics;
        public ShooterClientReconciliationResult LastReconciliationResult { get; private set; } = ShooterClientReconciliationResult.None;
        public bool NeedsFullSnapshotResync { get; private set; }

        public ShooterClientRecoveryState RecoveryState
        {
            get => _recoveryState;
            private set => SetRecoveryState(value);
        }

        public FastReconnectPhase FastReconnectPhase => _fastReconnect.Phase;
        public IReadOnlyList<SyncHealthEvent> LastFastReconnectHealthEvents => _lastHealthEvents;
        public ShooterClientResyncReason LastResyncReason { get; private set; } = ShooterClientResyncReason.None;
        public int LastResyncClientFrame { get; private set; }
        public int LastResyncAuthoritativeFrame { get; private set; }
        public uint LastResyncClientStateHash { get; private set; }
        public uint LastResyncAuthoritativeStateHash { get; private set; }

        public bool TryRestorePredictedSnapshot(int frame)
        {
            if (!_rollback.TryRestore(new FrameIndex(frame)))
            {
                return false;
            }

            _predictionReconciliation.Clear();
            _accumulator = 0f;
            PublishRuntimeSnapshot();
            return true;
        }

        public ShooterClientFrameTickResult CatchUpToFrame(int targetFrame)
        {
            if (!_runtime.IsStarted)
            {
                return ShooterClientFrameTickResult.NotStarted;
            }

            if (RecoveryState == ShooterClientRecoveryState.AwaitingFullSnapshot || RecoveryState == ShooterClientRecoveryState.ApplyingFullSnapshot)
            {
                return new ShooterClientFrameTickResult(0, _runtime.CurrentFrame, _runtime.ComputeStateHash());
            }

            if (targetFrame <= _runtime.CurrentFrame)
            {
                _accumulator = 0f;
                return new ShooterClientFrameTickResult(0, _runtime.CurrentFrame, _runtime.ComputeStateHash());
            }

            var ticks = 0;
            while (_runtime.CurrentFrame < targetFrame)
            {
                if (!StepPredictedFrame(Array.Empty<FrameworkPlayerInputCommand>()))
                {
                    break;
                }

                ticks++;
            }

            _accumulator = 0f;
            if (ticks > 0)
            {
                PublishRuntimeSnapshot();
            }

            return new ShooterClientFrameTickResult(ticks, _runtime.CurrentFrame, _runtime.ComputeStateHash());
        }

        public int SubmitLocalInput(in ShooterPlayerCommand command)
        {
            return SubmitLocalInputs(new[] { command });
        }

        public int SubmitLocalInputs(ShooterPlayerCommand[] commands)
        {
            if (commands == null || commands.Length == 0)
            {
                return 0;
            }

            if (RecoveryState == ShooterClientRecoveryState.AwaitingFullSnapshot || RecoveryState == ShooterClientRecoveryState.ApplyingFullSnapshot)
            {
                return 0;
            }

            var frame = _runtime.CurrentFrame;
            var accepted = _runtime.SubmitInput(frame, commands);
            if (accepted > 0)
            {
                RecordPendingInput(frame, commands);
                _frameworkInputHistory.Store(
                    new FrameIndex(frame),
                    ShooterClientPredictionRuntimeAdapter.CreateInputCommands(new FrameIndex(frame), commands));
            }

            return accepted;
        }

        public bool TryEnterCatchUp(int authoritativeFrame)
        {
            if (!_runtime.IsStarted || authoritativeFrame <= _runtime.CurrentFrame)
            {
                return false;
            }

            var frameGap = authoritativeFrame - _runtime.CurrentFrame;
            if (frameGap > _recoveryPolicy.SmallCatchUpThreshold)
            {
                MarkFullSnapshotResyncNeeded(
                    ShooterClientResyncReason.FrameTooFarBehind,
                    _runtime.CurrentFrame,
                    authoritativeFrame,
                    _runtime.ComputeStateHash(),
                    0u);
                return false;
            }

            _catchUpTargetFrame = authoritativeFrame;
            LastResyncAuthoritativeFrame = authoritativeFrame;
            RecoveryState = ShooterClientRecoveryState.CatchUp;
            return true;
        }

        public void MarkGatewayInputResyncRequested(int clientFrame, int authoritativeFrame, uint clientStateHash = 0u, uint authoritativeStateHash = 0u)
        {
            MarkFullSnapshotResyncNeeded(
                ShooterClientResyncReason.ClientHashRejectedByServer,
                clientFrame,
                authoritativeFrame,
                clientStateHash,
                authoritativeStateHash);
        }

        private void RecordPendingInput(int frame, ShooterPlayerCommand[] commands)
        {
            _predictionReconciliation.RecordLocalInput(frame, commands);
        }

        public ShooterSnapshotApplyResult ApplyGatewayPush(uint opCode, ArraySegment<byte> payload)
        {
            if (!_gatewaySnapshotDecoder.IsSnapshotPush(opCode))
            {
                LastSnapshotApplyResult = ShooterSnapshotApplyResult.Ignored;
                LastReconciliationResult = ShooterClientReconciliationResult.None;
                return LastSnapshotApplyResult;
            }

            var snapshot = _gatewaySnapshotDecoder.Decode(payload);
            var replayTargetFrame = _runtime.CurrentFrame;
            var predictedHashBeforeCorrection = _runtime.IsStarted ? _runtime.ComputeStateHash() : 0u;
            var wasAwaitingFullSnapshot = NeedsFullSnapshotResync;
            var hasPredictedControlledPlayer = TryCapturePredictedControlledPlayer(out var predictedControlledPlayer);

            LastSnapshotApplyResult = _frameworkSnapshotPipeline.ApplyGatewaySnapshot(in snapshot);
            if (LastSnapshotApplyResult == ShooterSnapshotApplyResult.AppliedPackedSnapshot)
            {
                var authoritativeFrame = _frameworkSnapshotPipeline.LastAppliedFrame;
                var authoritativeStateHash = _frameworkSnapshotPipeline.LastAppliedStateHash;
                var importedStateHash = _runtime.ComputeStateHash();
                var isStrongRecoverySnapshot = IsStrongRecoverySnapshot(_frameworkSnapshotPipeline.LastAppliedSnapshotFlags);

                RecoveryState = wasAwaitingFullSnapshot ? ShooterClientRecoveryState.ApplyingFullSnapshot : ShooterClientRecoveryState.Normal;
                _accumulator = 0f;
                CaptureRollbackSnapshot();
                var reconciliation = new ShooterClientReconciliationResult(
                    LastSnapshotApplyResult,
                    _predictionReconciliation.ReconcileAfterAuthoritativeSnapshot(
                        replayTargetFrame,
                        predictedHashBeforeCorrection,
                        authoritativeFrame,
                        authoritativeStateHash,
                        importedStateHash,
                        _runtime.CurrentFrame,
                        () => _runtime.CurrentFrame,
                        () => _runtime.ComputeStateHash(),
                        SubmitReplayInput,
                        StepReplayFrame));

                LastReconciliationResult = reconciliation;
                PreserveReasonableControlledPrediction(hasPredictedControlledPlayer, in predictedControlledPlayer, in reconciliation);
                if (reconciliation.AuthoritativeHashMatched)
                {
                    var frameDelta = replayTargetFrame - authoritativeFrame;
                    if (Math.Abs(frameDelta) > _recoveryPolicy.ReplayThreshold)
                    {
                        MarkFullSnapshotResyncNeeded(
                            frameDelta > 0 ? ShooterClientResyncReason.FrameTooFarAhead : ShooterClientResyncReason.FrameTooFarBehind,
                            replayTargetFrame,
                            authoritativeFrame,
                            predictedHashBeforeCorrection,
                            authoritativeStateHash);
                    }
                    else
                    {
                        ClearFullSnapshotResync();
                        RecoveryState = wasAwaitingFullSnapshot && isStrongRecoverySnapshot ? ShooterClientRecoveryState.Recovered : ShooterClientRecoveryState.Normal;
                    }
                }
                else
                {
                    MarkFullSnapshotResyncNeeded(
                        ShooterClientResyncReason.AuthoritativeHashMismatch,
                        replayTargetFrame,
                        authoritativeFrame,
                        predictedHashBeforeCorrection,
                        authoritativeStateHash);
                }

                _presentation.PublishReconciliation(in reconciliation);
                PublishRuntimeSnapshot();

                if (RecoveryState == ShooterClientRecoveryState.Normal && !wasAwaitingFullSnapshot)
                {
                    HeartbeatFastReconnect(authoritativeFrame);
                }
            }
            else
            {
                LastReconciliationResult = ShooterClientReconciliationResult.None;
                if (LastSnapshotApplyResult == ShooterSnapshotApplyResult.ImportFailed)
                {
                    MarkFullSnapshotResyncNeeded(
                        ShooterClientResyncReason.ImportFailed,
                        replayTargetFrame,
                        0,
                        predictedHashBeforeCorrection,
                        0u);
                }
            }

            return LastSnapshotApplyResult;
        }

        public ShooterClientFrameTickResult Tick(float deltaTime)
        {
            if (deltaTime < 0f) throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (!_runtime.IsStarted)
            {
                return ShooterClientFrameTickResult.NotStarted;
            }

            if (RecoveryState == ShooterClientRecoveryState.AwaitingFullSnapshot || RecoveryState == ShooterClientRecoveryState.ApplyingFullSnapshot)
            {
                _accumulator = 0f;
                return new ShooterClientFrameTickResult(0, _runtime.CurrentFrame, _runtime.ComputeStateHash());
            }

            if (RecoveryState == ShooterClientRecoveryState.CatchUp)
            {
                return TickCatchUp();
            }

            if (RecoveryState == ShooterClientRecoveryState.Recovered)
            {
                RecoveryState = ShooterClientRecoveryState.Normal;
                _accumulator = 0f;
                return new ShooterClientFrameTickResult(0, _runtime.CurrentFrame, _runtime.ComputeStateHash());
            }

            _accumulator += deltaTime;
            var ticks = 0;
            while (_accumulator >= _fixedDeltaTime)
            {
                if (!StepPredictedFrame(Array.Empty<FrameworkPlayerInputCommand>()))
                {
                    break;
                }

                _accumulator -= _fixedDeltaTime;
                ticks++;
            }

            if (ticks > 0)
            {
                PublishRuntimeSnapshot();
            }

            return new ShooterClientFrameTickResult(ticks, _runtime.CurrentFrame, _runtime.ComputeStateHash());
        }

        private bool StepPredictedFrame(FrameworkPlayerInputCommand[] inputs)
        {
            if (!_runtime.IsStarted)
            {
                return false;
            }

            var currentFrame = _runtime.CurrentFrame;
            var nextFrame = new FrameIndex(currentFrame + 1);
            _predictionRunner.TickPredicted(nextFrame, _fixedDeltaTime, inputs, _predictionRuntimeAdapter.ComputeHash);
            return _runtime.CurrentFrame > currentFrame;
        }

        private bool StepReplayFrame()
        {
            return StepPredictedFrame(GetStoredFrameworkInputs(_runtime.CurrentFrame));
        }

        private int SubmitReplayInput(int frame, ShooterPlayerCommand[] commands)
        {
            if (commands == null || commands.Length == 0)
            {
                return 0;
            }

            _frameworkInputHistory.Store(
                new FrameIndex(frame),
                ShooterClientPredictionRuntimeAdapter.CreateInputCommands(new FrameIndex(frame), commands));
            return commands.Length;
        }

        private FrameworkPlayerInputCommand[] GetStoredFrameworkInputs(int frame)
        {
            return _frameworkInputHistory.TryGet(new FrameIndex(frame), out var inputs)
                ? inputs
                : Array.Empty<FrameworkPlayerInputCommand>();
        }

        private ShooterClientFrameTickResult TickCatchUp()
        {
            var targetFrame = _catchUpTargetFrame;
            if (targetFrame <= _runtime.CurrentFrame)
            {
                RecoveryState = ShooterClientRecoveryState.Recovered;
                _accumulator = 0f;
                return new ShooterClientFrameTickResult(0, _runtime.CurrentFrame, _runtime.ComputeStateHash());
            }

            var ticks = 0;
            var maxTicks = Math.Min(_recoveryPolicy.MaxCatchUpTicksPerUpdate, targetFrame - _runtime.CurrentFrame);
            while (ticks < maxTicks && _runtime.CurrentFrame < targetFrame)
            {
                if (!StepPredictedFrame(Array.Empty<FrameworkPlayerInputCommand>()))
                {
                    break;
                }

                ticks++;
            }

            _accumulator = 0f;
            if (ticks > 0)
            {
                PublishRuntimeSnapshot();
            }

            if (_runtime.CurrentFrame >= targetFrame)
            {
                RecoveryState = ShooterClientRecoveryState.Normal;
            }

            return new ShooterClientFrameTickResult(ticks, _runtime.CurrentFrame, _runtime.ComputeStateHash());
        }

        private void CaptureRollbackSnapshot()
        {
            if (_runtime.IsStarted)
            {
                _rollback.CaptureAndStore(new FrameIndex(_runtime.CurrentFrame));
            }
        }

        private void MarkFullSnapshotResyncNeeded(
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
            _catchUpTargetFrame = authoritativeFrame > clientFrame ? authoritativeFrame : clientFrame;
            RecoveryState = ShooterClientRecoveryState.AwaitingFullSnapshot;
        }

        private void ClearFullSnapshotResync()
        {
            NeedsFullSnapshotResync = false;
            LastResyncReason = ShooterClientResyncReason.None;
            LastResyncClientFrame = 0;
            LastResyncAuthoritativeFrame = 0;
            LastResyncClientStateHash = 0u;
            LastResyncAuthoritativeStateHash = 0u;
            _catchUpTargetFrame = 0;
        }

        private static bool IsStrongRecoverySnapshot(uint snapshotFlags)
        {
            return (snapshotFlags & ShooterPackedSnapshotFlags.Full) != 0u
                || (snapshotFlags & ShooterPackedSnapshotFlags.AuthorityOverride) != 0u;
        }

        private void SetRecoveryState(ShooterClientRecoveryState next)
        {
            var previous = _recoveryState;
            _recoveryState = next;
            if (previous == next)
            {
                return;
            }

            DriveFastReconnect(next);
        }

        private void DriveFastReconnect(ShooterClientRecoveryState next)
        {
            _fastReconnect.ResetEventBuffer();
            switch (next)
            {
                case ShooterClientRecoveryState.CatchUp:
                {
                    var gap = _catchUpTargetFrame > _runtime.CurrentFrame ? _catchUpTargetFrame - _runtime.CurrentFrame : 1;
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
                    _fastReconnect.Reconcile(FastReconnectPhase.Connected, _runtime.CurrentFrame, 0);
                    break;
            }

            CaptureHealthEvents();
        }

        private void HeartbeatFastReconnect(int authoritativeFrame)
        {
            _fastReconnect.ResetEventBuffer();
            _fastReconnect.Heartbeat(authoritativeFrame);
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

        private bool TryCapturePredictedControlledPlayer(out ShooterSveltoPlayerComponent player)
        {
            var controlledPlayerId = _presentation.ControlledPlayerId;
            if (controlledPlayerId <= 0 || !_runtime.IsStarted)
            {
                player = default;
                return false;
            }

            return _runtime.TryGetPlayer(controlledPlayerId, out player);
        }

        private void PreserveReasonableControlledPrediction(
            bool hasPredictedControlledPlayer,
            in ShooterSveltoPlayerComponent predictedControlledPlayer,
            in ShooterClientReconciliationResult reconciliation)
        {
            if (!hasPredictedControlledPlayer || predictedControlledPlayer.PlayerId <= 0)
            {
                return;
            }

            if (!_runtime.TryGetPlayer(predictedControlledPlayer.PlayerId, out var reconciledPlayer))
            {
                return;
            }

            var dx = predictedControlledPlayer.X - reconciledPlayer.X;
            var dy = predictedControlledPlayer.Y - reconciledPlayer.Y;
            var distanceSquared = dx * dx + dy * dy;
            var maxReasonableDistance = GetMaxReasonableControlledPredictionDistance(in reconciliation);
            if (distanceSquared > maxReasonableDistance * maxReasonableDistance)
            {
                return;
            }

            if (distanceSquared <= 0.000001f)
            {
                return;
            }

            reconciledPlayer.X = predictedControlledPlayer.X;
            reconciledPlayer.Y = predictedControlledPlayer.Y;
            reconciledPlayer.AimX = predictedControlledPlayer.AimX;
            reconciledPlayer.AimY = predictedControlledPlayer.AimY;
            _runtime.SetPlayer(in reconciledPlayer);
        }

        private float GetMaxReasonableControlledPredictionDistance(in ShooterClientReconciliationResult reconciliation)
        {
            var replayFrames = Math.Max(1, Math.Abs(reconciliation.PredictedFrameBeforeCorrection - reconciliation.AuthoritativeFrame));
            var replayDistance = ShooterBattleTuning.PlayerSpeed * _fixedDeltaTime * replayFrames;
            return replayDistance + ShooterBattleTuning.PlayerSpeed * _fixedDeltaTime + 0.05f;
        }

        private void PublishRuntimeSnapshot()
        {
            _presentation.ApplyLocalPredictionSnapshot(_runtime.GetSnapshot());
        }
    }

    public enum ShooterClientResyncReason
    {
        None = 0,
        ImportFailed = 1,
        AuthoritativeHashMismatch = 2,
        ClientHashRejectedByServer = 3,
        FrameTooFarBehind = 4,
        FrameTooFarAhead = 5,
        SnapshotTimeout = 6,
        WorldMismatch = 7
    }

    public readonly struct ShooterClientFrameTickResult
    {
        public static readonly ShooterClientFrameTickResult NotStarted = new ShooterClientFrameTickResult(0, 0, 0u);
        public readonly int Ticks;
        public readonly int Frame;
        public readonly uint StateHash;
        public ShooterClientFrameTickResult(int ticks, int frame, uint stateHash)
        {
            Ticks = ticks;
            Frame = frame;
            StateHash = stateHash;
        }
    }

    public readonly struct ShooterClientReconciliationResult
    {
        public static readonly ShooterClientReconciliationResult None = new ShooterClientReconciliationResult(ShooterSnapshotApplyResult.Ignored, 0, 0u, 0, 0u, 0u, false, 0, 0, 0u, 0, 0, 0);
        public readonly ShooterSnapshotApplyResult ApplyResult;
        public readonly int PredictedFrameBeforeCorrection;
        public readonly uint PredictedHashBeforeCorrection;
        public readonly int AuthoritativeFrame;
        public readonly uint AuthoritativeStateHash;
        public readonly uint ImportedStateHash;
        public readonly bool AuthoritativeHashMatched;
        public readonly int ReplayTicks;
        public readonly int FinalFrame;
        public readonly uint FinalStateHash;
        public readonly int PendingInputFramesBeforeCorrection;
        public readonly int PendingInputFramesAfterTrim;
        public readonly int PendingInputFramesAfterReplay;

        public ShooterClientReconciliationResult(
            ShooterSnapshotApplyResult applyResult,
            int predictedFrameBeforeCorrection,
            uint predictedHashBeforeCorrection,
            int authoritativeFrame,
            uint authoritativeStateHash,
            uint importedStateHash,
            bool authoritativeHashMatched,
            int replayTicks,
            int finalFrame,
            uint finalStateHash,
            int pendingInputFramesBeforeCorrection,
            int pendingInputFramesAfterTrim,
            int pendingInputFramesAfterReplay)
        {
            ApplyResult = applyResult;
            PredictedFrameBeforeCorrection = predictedFrameBeforeCorrection;
            PredictedHashBeforeCorrection = predictedHashBeforeCorrection;
            AuthoritativeFrame = authoritativeFrame;
            AuthoritativeStateHash = authoritativeStateHash;
            ImportedStateHash = importedStateHash;
            AuthoritativeHashMatched = authoritativeHashMatched;
            ReplayTicks = replayTicks;
            FinalFrame = finalFrame;
            FinalStateHash = finalStateHash;
            PendingInputFramesBeforeCorrection = pendingInputFramesBeforeCorrection;
            PendingInputFramesAfterTrim = pendingInputFramesAfterTrim;
            PendingInputFramesAfterReplay = pendingInputFramesAfterReplay;
        }

        public ShooterClientReconciliationResult(ShooterSnapshotApplyResult applyResult, in ClientPredictionReconciliationResult result)
            : this(
                applyResult,
                result.PredictedFrameBeforeCorrection,
                result.PredictedHashBeforeCorrection,
                result.AuthoritativeFrame,
                result.AuthoritativeStateHash,
                result.ImportedStateHash,
                result.AuthoritativeHashMatched,
                result.ReplayTicks,
                result.FinalFrame,
                result.FinalStateHash,
                result.PendingInputFramesBeforeCorrection,
                result.PendingInputFramesAfterTrim,
                result.PendingInputFramesAfterReplay)
        {
        }
    }
}
