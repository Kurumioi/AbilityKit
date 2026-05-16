using AbilityKit.Ability.Host;
using AbilityKit.Ability.Share.Impl.Moba.CreateWorld;
using AbilityKit.Ability.Share.Impl.Moba.Struct;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Protocol.Moba.StateSync;

namespace AbilityKit.Game.Flow.Snapshot
{
    public sealed class MobaFrameSnapshotDeserializer : IFrameSnapshotDeserializer
    {
        public bool TryDeserializeEnterGame(in WorldStateSnapshot snap, out EnterMobaGameRes enterGame)
        {
            if (snap.OpCode != (int)MobaOpCode.EnterGameSnapshot || snap.Payload == null || snap.Payload.Length == 0)
            {
                enterGame = default;
                return false;
            }

            enterGame = EnterMobaGameCodec.DeserializeRes(snap.Payload);
            return true;
        }

        public bool TryDeserializeActorTransform(in WorldStateSnapshot snap, out MobaActorTransformSnapshotEntry[] entries)
        {
            if (snap.OpCode != (int)MobaOpCode.ActorTransformSnapshot || snap.Payload == null || snap.Payload.Length == 0)
            {
                entries = null;
                return false;
            }

            entries = MobaActorTransformSnapshotCodec.Deserialize(snap.Payload);
            return true;
        }

        public bool TryDeserializeStateHash(in WorldStateSnapshot snap, out MobaStateHashSnapshotPayload payload)
        {
            if (snap.OpCode != (int)MobaOpCode.StateHashSnapshot || snap.Payload == null || snap.Payload.Length == 0)
            {
                payload = default;
                return false;
            }

            payload = MobaStateHashSnapshotCodec.Deserialize(snap.Payload);
            return true;
        }
    }
}
