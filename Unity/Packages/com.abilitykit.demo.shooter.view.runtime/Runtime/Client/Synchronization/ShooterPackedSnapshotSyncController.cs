using System;
using AbilityKit.Demo.Shooter.Runtime;

namespace AbilityKit.Demo.Shooter.View
{
    public sealed class ShooterPackedSnapshotSyncController
    {
        private readonly IShooterBattleRuntimePort _runtime;
        private readonly ShooterPresentationFacade _presentation;
        private readonly ShooterGatewaySnapshotDecoder _decoder;

        public ShooterPackedSnapshotSyncController(IShooterBattleRuntimePort runtime, ShooterPresentationFacade presentation)
            : this(runtime, presentation, new ShooterGatewaySnapshotDecoder())
        {
        }

        public ShooterPackedSnapshotSyncController(IShooterBattleRuntimePort runtime, ShooterPresentationFacade presentation, ShooterGatewaySnapshotDecoder decoder)
        {
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            _presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
            _decoder = decoder ?? throw new ArgumentNullException(nameof(decoder));
        }

        private bool _hasAppliedSnapshot;

        public int LastAppliedFrame { get; private set; }

        public uint LastAppliedStateHash { get; private set; }

        public uint LastAppliedSnapshotFlags { get; private set; }

        public int LastIgnoredFrame { get; private set; } = -1;

        public ShooterSnapshotApplyResult TryApplyGatewayPush(uint opCode, ArraySegment<byte> payload)
        {
            if (!_decoder.IsSnapshotPush(opCode))
            {
                return ShooterSnapshotApplyResult.Ignored;
            }

            var snapshot = _decoder.Decode(payload);
            return ApplyGatewaySnapshot(in snapshot);
        }

        public ShooterSnapshotApplyResult ApplyGatewaySnapshot(in ShooterGatewaySnapshot snapshot)
        {
            var snapshotFrame = snapshot.PackedSnapshot.HasValue
                ? snapshot.PackedSnapshot.Value.Frame
                : snapshot.Frame;
            if (_hasAppliedSnapshot && snapshotFrame <= LastAppliedFrame)
            {
                LastIgnoredFrame = snapshotFrame;
                return ShooterSnapshotApplyResult.IgnoredStaleSnapshot;
            }

            // Authoritative snapshots should not overwrite the locally predicted actor when the
            // prediction path is already healthy; the runtime publish that follows reconciliation
            // will refresh the local actor from the corrected simulation state.
            if (!snapshot.PackedSnapshot.HasValue)
            {
                _presentation.ApplyInterpolatedGatewaySnapshot(in snapshot);
                LastAppliedFrame = snapshot.Frame;
                LastAppliedStateHash = 0;
                LastAppliedSnapshotFlags = 0;
                _hasAppliedSnapshot = true;
                return ShooterSnapshotApplyResult.AppliedActorSnapshot;
            }

            var packed = snapshot.PackedSnapshot.Value;
            if (!_runtime.ImportPackedSnapshot(in packed))
            {
                return ShooterSnapshotApplyResult.ImportFailed;
            }

            LastAppliedFrame = packed.Frame;
            LastAppliedStateHash = packed.StateHash;
            LastAppliedSnapshotFlags = packed.SnapshotFlags;
            _hasAppliedSnapshot = true;
            _presentation.ApplyInterpolatedGatewaySnapshot(in snapshot);
            return ShooterSnapshotApplyResult.AppliedPackedSnapshot;
        }
    }

    public enum ShooterSnapshotApplyResult
    {
        Ignored = 0,
        AppliedActorSnapshot = 1,
        AppliedPackedSnapshot = 2,
        ImportFailed = 3,
        IgnoredStaleSnapshot = 4
    }
}
