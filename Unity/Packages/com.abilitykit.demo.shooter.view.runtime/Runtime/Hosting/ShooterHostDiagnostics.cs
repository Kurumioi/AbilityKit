#nullable enable

using System;
using System.Collections.Generic;
using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Network.Runtime.Conditioning;
using AbilityKit.Network.Runtime.Sync;
using AbilityKit.Protocol.Shooter;

namespace AbilityKit.Demo.Shooter.View.Hosting
{
    /// <summary>
    /// Host-neutral diagnostics snapshot for Shooter demo shells. Editor windows, PlayMode hosts and
    /// attach-mode observers can project the same runtime/session state without owning collection logic.
    /// </summary>
    public readonly struct ShooterHostDiagnosticsSnapshot
    {
        public ShooterHostDiagnosticsSnapshot(
            int frame,
            int playerCount,
            int bulletCount,
            IReadOnlyList<ShooterEventSnapshot> recentEvents,
            int totalEvents,
            double maxDivergence,
            IReadOnlyList<ShooterWorldDivergence> divergences,
            NetworkConditioningStats? carrierNetworkStats,
            ShooterSnapshotApplyResult? lastCarrierSnapshotApplyResult,
            SyncTimeAnchor lastCarrierTimeAnchor,
            ShooterLagCompensationTelemetry? lagCompensationTelemetry)
        {
            Frame = frame;
            PlayerCount = playerCount;
            BulletCount = bulletCount;
            RecentEvents = recentEvents ?? Array.Empty<ShooterEventSnapshot>();
            TotalEvents = totalEvents;
            MaxDivergence = maxDivergence;
            Divergences = divergences ?? Array.Empty<ShooterWorldDivergence>();
            CarrierNetworkStats = carrierNetworkStats;
            LastCarrierSnapshotApplyResult = lastCarrierSnapshotApplyResult;
            LastCarrierTimeAnchor = lastCarrierTimeAnchor;
            LagCompensationTelemetry = lagCompensationTelemetry;
        }

        public int Frame { get; }

        public int PlayerCount { get; }

        public int BulletCount { get; }

        public IReadOnlyList<ShooterEventSnapshot> RecentEvents { get; }

        public int TotalEvents { get; }

        public double MaxDivergence { get; }

        public IReadOnlyList<ShooterWorldDivergence> Divergences { get; }

        public NetworkConditioningStats? CarrierNetworkStats { get; }

        public ShooterSnapshotApplyResult? LastCarrierSnapshotApplyResult { get; }

        public SyncTimeAnchor LastCarrierTimeAnchor { get; }

        public ShooterLagCompensationTelemetry? LagCompensationTelemetry { get; }
    }

    public static class ShooterHostDiagnosticsProjector
    {
        public static ShooterHostDiagnosticsSnapshot ProjectFromSession(
            ShooterAcceptanceSession session,
            in ShooterStateSnapshotPayload snapshot,
            int previousTotalEvents)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            var recentEvents = snapshot.Events ?? Array.Empty<ShooterEventSnapshot>();
            var comparison = session.HasAuthoritativeWorld
                ? session.CompareWorlds()
                : new ShooterWorldComparison(snapshot.Frame, 0, Array.Empty<ShooterWorldDivergence>());

            return new ShooterHostDiagnosticsSnapshot(
                snapshot.Frame,
                snapshot.Players?.Length ?? 0,
                snapshot.Bullets?.Length ?? 0,
                recentEvents,
                previousTotalEvents + recentEvents.Length,
                comparison.MaxDistance,
                comparison.Divergences,
                session.CarrierNetworkStats,
                session.LastCarrierSnapshotApplyResult,
                session.LastCarrierTimeAnchor,
                session.LagCompensationTelemetry);
        }

        public static ShooterHostDiagnosticsSnapshot ProjectFromFrame(
            in ShooterHostPresentationFrame frame,
            int previousTotalEvents)
        {
            var batch = frame.ClientBatch;
            var recentEvents = batch.Events ?? Array.Empty<ShooterEventSnapshot>();

            return new ShooterHostDiagnosticsSnapshot(
                batch.Frame,
                CountEntities(batch.EntityChanges, ShooterViewEntityKind.Player),
                CountEntities(batch.EntityChanges, ShooterViewEntityKind.Bullet),
                recentEvents,
                previousTotalEvents + recentEvents.Count,
                0d,
                Array.Empty<ShooterWorldDivergence>(),
                frame.CarrierNetworkStats,
                frame.LastCarrierSnapshotApplyResult,
                frame.LastCarrierTimeAnchor,
                frame.LagCompensationTelemetry);
        }

        private static int CountEntities(IReadOnlyList<ShooterViewEntityChange> changes, ShooterViewEntityKind kind)
        {
            var count = 0;
            for (var i = 0; i < changes.Count; i++)
            {
                if (changes[i].Kind == kind && changes[i].Alive)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
