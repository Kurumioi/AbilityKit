using AbilityKit.Ability.Host;
using AbilityKit.Demo.Moba.Share;
using AbilityKit.Protocol.Moba.StateSync;

namespace ET.Logic
{
    [RuntimeSnapshotConverter(MobaOpCode.ActorTransformSnapshot)]
    public sealed class ActorTransformSnapshotConverter : IRuntimeSnapshotConverter
    {
        public int OpCode => MobaOpCode.ActorTransformSnapshot;

        public bool TryConvert(in WorldStateSnapshot snapshot, int frameIndex, double timestamp, out FrameSnapshotData frameSnapshot)
        {
            var entries = MobaActorTransformSnapshotCodec.Deserialize(snapshot.Payload);
            if (entries.Length == 0)
            {
                frameSnapshot = default;
                return false;
            }

            var transforms = new ActorTransformData[entries.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                transforms[i] = new ActorTransformData(
                    actorId: entry.ActorId,
                    x: entry.X,
                    y: entry.Z,
                    z: entry.Y,
                    rotationY: 0f,
                    scale: 1f);
            }

            frameSnapshot = new FrameSnapshotData(
                frameIndex: frameIndex,
                timestamp: timestamp,
                type: SnapshotType.Delta,
                actorTransforms: transforms);
            return true;
        }
    }
}
