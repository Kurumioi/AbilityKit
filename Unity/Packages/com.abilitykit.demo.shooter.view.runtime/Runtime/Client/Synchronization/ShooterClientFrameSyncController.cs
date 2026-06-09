#nullable enable

using System;
using AbilityKit.Ability.Host.Extensions.Client.FrameSync;
using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Protocol.Shooter;

namespace AbilityKit.Demo.Shooter.View
{
    public sealed class ShooterClientFrameSyncController
    {
        private readonly IShooterBattleRuntimePort _runtime;
        private readonly ShooterPresentationFacade _presentation;
        private readonly ShooterPackedSnapshotSyncController _snapshotSync;
        private readonly ClientPredictionInputHistory<ShooterPlayerCommand> _pendingInputs = new ClientPredictionInputHistory<ShooterPlayerCommand>();
        private readonly float _fixedDeltaTime;
        private float _accumulator;

        public ShooterClientFrameSyncController(IShooterBattleRuntimePort runtime, ShooterPresentationFacade presentation, int tickRate)
            : this(runtime, presentation, tickRate, null)
        {
        }

        public ShooterClientFrameSyncController(IShooterBattleRuntimePort runtime, ShooterPresentationFacade presentation, int tickRate, ShooterGatewaySnapshotDecoder? decoder)
        {
            if (tickRate <= 0) throw new ArgumentOutOfRangeException(nameof(tickRate));

            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            _presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
            _snapshotSync = decoder == null
                ? new ShooterPackedSnapshotSyncController(_runtime, _presentation)
                : new ShooterPackedSnapshotSyncController(_runtime, _presentation, decoder);
            _fixedDeltaTime = 1f / tickRate;
        }

        public int CurrentFrame => _runtime.CurrentFrame;

        public float FixedDeltaTime => _fixedDeltaTime;

        public float AccumulatedTime => _accumulator;

        public int PendingInputFrameCount => _pendingInputs.Count;

        public ShooterSnapshotApplyResult LastSnapshotApplyResult { get; private set; } = ShooterSnapshotApplyResult.Ignored;

        public ShooterClientReconciliationResult LastReconciliationResult { get; private set; } = ShooterClientReconciliationResult.None;

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

            var frame = _runtime.CurrentFrame;
            var accepted = _runtime.SubmitInput(frame, commands);
            if (accepted > 0)
            {
                RecordPendingInput(frame, commands);
            }

            return accepted;
        }

        public ShooterSnapshotApplyResult ApplyGatewayPush(uint opCode, ArraySegment<byte> payload)
        {
            var replayTargetFrame = _runtime.CurrentFrame;
            var predictedHashBeforeCorrection = _runtime.IsStarted ? _runtime.ComputeStateHash() : 0u;
            var pendingBeforeCorrection = _pendingInputs.Count;

            LastSnapshotApplyResult = _snapshotSync.TryApplyGatewayPush(opCode, payload);
            if (LastSnapshotApplyResult == ShooterSnapshotApplyResult.AppliedPackedSnapshot)
            {
                var authoritativeFrame = _snapshotSync.LastAppliedFrame;
                var authoritativeStateHash = _snapshotSync.LastAppliedStateHash;
                var importedStateHash = _runtime.ComputeStateHash();

                _accumulator = 0f;
                TrimConfirmedInputs(_runtime.CurrentFrame);
                var pendingAfterTrim = _pendingInputs.Count;
                var replayTicks = ReplayPendingInputs(replayTargetFrame);
                var finalStateHash = _runtime.ComputeStateHash();

                var reconciliation = new ShooterClientReconciliationResult(
                    LastSnapshotApplyResult,
                    replayTargetFrame,
                    predictedHashBeforeCorrection,
                    authoritativeFrame,
                    authoritativeStateHash,
                    importedStateHash,
                    authoritativeStateHash == importedStateHash,
                    replayTicks,
                    _runtime.CurrentFrame,
                    finalStateHash,
                    pendingBeforeCorrection,
                    pendingAfterTrim,
                    _pendingInputs.Count);

                LastReconciliationResult = reconciliation;
                _presentation.PublishReconciliation(in reconciliation);
                PublishRuntimeSnapshot();
            }
            else
            {
                LastReconciliationResult = ShooterClientReconciliationResult.None;
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

            _accumulator += deltaTime;
            var ticks = 0;
            while (_accumulator + 0.000001f >= _fixedDeltaTime)
            {
                if (!StepRuntimeFrame())
                {
                    break;
                }

                ticks++;
                _accumulator -= _fixedDeltaTime;
            }

            if (ticks > 0)
            {
                PublishRuntimeSnapshot();
            }

            return new ShooterClientFrameTickResult(ticks, _runtime.CurrentFrame, _runtime.ComputeStateHash());
        }

        private bool StepRuntimeFrame()
        {
            return _runtime.Tick(_fixedDeltaTime);
        }

        private int ReplayPendingInputs(int targetFrame)
        {
            var result = _pendingInputs.ReplayTo(
                targetFrame,
                () => _runtime.CurrentFrame,
                (frame, commands) => _runtime.SubmitInput(frame, commands),
                StepRuntimeFrame);
            return result.ReplayTicks;
        }

        private void RecordPendingInput(int frame, ShooterPlayerCommand[] commands)
        {
            _pendingInputs.Record(frame, commands);
        }

        private void TrimConfirmedInputs(int confirmedFrame)
        {
            _pendingInputs.TrimBefore(confirmedFrame);
        }

        private void PublishRuntimeSnapshot()
        {
            _presentation.ApplyShooterSnapshot(_runtime.GetSnapshot());
        }
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
        public static readonly ShooterClientReconciliationResult None = new ShooterClientReconciliationResult(
            ShooterSnapshotApplyResult.Ignored,
            0,
            0u,
            0,
            0u,
            0u,
            false,
            0,
            0,
            0u,
            0,
            0,
            0);

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
    }
}
