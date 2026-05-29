using System;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Demo.Moba.Share;

namespace ET.Logic
{
    /// <summary>
    /// Runtime actor spawn snapshot handler.
    /// </summary>
    [SnapshotHandler(SnapshotType.Delta)]
    public sealed class ActorSpawnSnapshotHandler : ISnapshotHandler
    {
        public SnapshotType SnapshotType => SnapshotType.Delta;

        public bool CanHandle(in FrameSnapshotData snapshot)
        {
            return snapshot.ActorSpawns != null && snapshot.ActorSpawns.Count > 0;
        }

        public void Handle(ETMobaBattleDriver driver, in FrameSnapshotData snapshot)
        {
            Log.Info($"[ActorSpawnSnapshotHandler] Frame={snapshot.FrameIndex}, Count={snapshot.ActorSpawns?.Count ?? 0}");

            driver.ViewSink?.OnEnterGameSnapshot(in snapshot);
        }
    }
}
