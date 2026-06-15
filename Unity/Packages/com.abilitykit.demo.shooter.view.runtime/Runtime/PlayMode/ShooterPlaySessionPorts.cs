#nullable enable

using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Network.Runtime.Conditioning;
using AbilityKit.Network.Runtime.Sync;

namespace AbilityKit.Demo.Shooter.View.PlayMode
{
    public readonly struct ShooterPlayFrameInput
    {
        public ShooterPlayFrameInput(float moveX, float moveY, float aimX, float aimY, bool fire)
        {
            MoveX = moveX;
            MoveY = moveY;
            AimX = aimX;
            AimY = aimY;
            Fire = fire;
        }

        public float MoveX { get; }
        public float MoveY { get; }
        public float AimX { get; }
        public float AimY { get; }
        public bool Fire { get; }
    }

    public readonly struct ShooterPlayPresentationFrame
    {
        public ShooterPlayPresentationFrame(
            ShooterSnapshotViewBatch clientBatch,
            ShooterSnapshotViewBatch authorityBatch,
            bool hasAuthorityBatch,
            int controlledPlayerId,
            float worldScale,
            NetworkConditioningStats? carrierNetworkStats,
            ShooterSnapshotApplyResult? lastCarrierSnapshotApplyResult,
            SyncTimeAnchor lastCarrierTimeAnchor,
            ShooterLagCompensationTelemetry? lagCompensationTelemetry)
        {
            ClientBatch = clientBatch;
            AuthorityBatch = authorityBatch;
            HasAuthorityBatch = hasAuthorityBatch;
            ControlledPlayerId = controlledPlayerId;
            WorldScale = worldScale;
            CarrierNetworkStats = carrierNetworkStats;
            LastCarrierSnapshotApplyResult = lastCarrierSnapshotApplyResult;
            LastCarrierTimeAnchor = lastCarrierTimeAnchor;
            LagCompensationTelemetry = lagCompensationTelemetry;
        }

        public ShooterSnapshotViewBatch ClientBatch { get; }
        public ShooterSnapshotViewBatch AuthorityBatch { get; }
        public bool HasAuthorityBatch { get; }
        public int ControlledPlayerId { get; }
        public float WorldScale { get; }
        public NetworkConditioningStats? CarrierNetworkStats { get; }
        public ShooterSnapshotApplyResult? LastCarrierSnapshotApplyResult { get; }
        public SyncTimeAnchor LastCarrierTimeAnchor { get; }
        public ShooterLagCompensationTelemetry? LagCompensationTelemetry { get; }
    }

    public interface IShooterPlayInputSource
    {
        ShooterPlayFrameInput ReadInput(int controlledPlayerId);
    }

    public interface IShooterPlayViewSink
    {
        void Render(in ShooterPlayPresentationFrame frame);
        void Clear();
    }
}
