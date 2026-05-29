using AbilityKit.Ability.Host;
using AbilityKit.Demo.Moba.Share;
using AbilityKit.Protocol.Moba.StateSync;

namespace ET.Logic
{
    [RuntimeSnapshotConverter(MobaOpCode.ActorSpawnSnapshot)]
    public sealed class ActorSpawnSnapshotConverter : IRuntimeSnapshotConverter
    {
        public int OpCode => MobaOpCode.ActorSpawnSnapshot;

        public bool TryConvert(in WorldStateSnapshot snapshot, int frameIndex, double timestamp, out FrameSnapshotData frameSnapshot)
        {
            var entries = MobaActorSpawnSnapshotCodec.Deserialize(snapshot.Payload);
            if (entries.Length == 0)
            {
                frameSnapshot = default;
                return false;
            }

            var spawns = new ActorSpawnData[entries.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                spawns[i] = new ActorSpawnData(
                    actorId: entry.NetId,
                    entityCode: entry.Code,
                    characterId: entry.Code,
                    name: string.Empty,
                    x: entry.X,
                    y: entry.Z,
                    z: entry.Y,
                    rotationY: 0f,
                    scale: 1f,
                    teamId: 0,
                    maxHp: 0f,
                    hp: 0f,
                    playerId: entry.OwnerNetId == 0 ? null : entry.OwnerNetId.ToString());
            }

            frameSnapshot = new FrameSnapshotData(
                frameIndex: frameIndex,
                timestamp: timestamp,
                type: SnapshotType.Delta,
                actorSpawns: spawns);
            return true;
        }
    }
}
