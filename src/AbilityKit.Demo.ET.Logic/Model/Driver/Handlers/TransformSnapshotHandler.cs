using System;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Demo.Moba.Share;

namespace ET.Logic
{
    /// <summary>
    /// 角色变换快照处理器
    /// </summary>
    [SnapshotHandler(SnapshotType.Delta)]
    public sealed class TransformSnapshotHandler : ISnapshotHandler
    {
        public SnapshotType SnapshotType => SnapshotType.Delta;

        public bool CanHandle(in FrameSnapshotData snapshot)
        {
            return snapshot.ActorTransforms != null && snapshot.ActorTransforms.Count > 0;
        }

        public void Handle(ETMobaBattleDriver driver, in FrameSnapshotData snapshot)
        {
            Log.Info($"[TransformSnapshotHandler] Frame={snapshot.FrameIndex}, Count={snapshot.ActorTransforms?.Count ?? 0}");

            driver.ViewSink?.OnActorTransformSnapshot(in snapshot);
        }
    }
}
