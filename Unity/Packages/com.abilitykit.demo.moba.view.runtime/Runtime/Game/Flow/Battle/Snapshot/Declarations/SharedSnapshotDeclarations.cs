using AbilityKit.Ability.Host;
using AbilityKit.Core.Common.SnapshotRouting;
using AbilityKit.Protocol.Moba;
using AbilityKit.Protocol.Moba.StateSync;

namespace AbilityKit.Game.Flow.Snapshot
{
    internal static class SharedSnapshotDeclarations
    {
        [SnapshotDecoder("shared", MobaOpCodes.Snapshot.StateHash, typeof(MobaStateHashSnapshotPayload))]
        internal static bool DecodeStateHash(in WorldStateSnapshot snap, out MobaStateHashSnapshotPayload payload)
        {
            if (snap.Payload == null || snap.Payload.Length == 0)
            {
                payload = default;
                return false;
            }

            payload = MobaStateHashSnapshotCodec.Deserialize(snap.Payload);
            return true;
        }

        [SnapshotDecoder("shared", MobaOpCodes.Snapshot.ActorTransform, typeof(MobaActorTransformSnapshotEntry[]))]
        internal static bool DecodeActorTransform(in WorldStateSnapshot snap, out MobaActorTransformSnapshotEntry[] entries)
        {
            if (snap.Payload == null || snap.Payload.Length == 0)
            {
                entries = null;
                return false;
            }

            entries = MobaActorTransformSnapshotCodec.Deserialize(snap.Payload);
            return true;
        }

        [SnapshotDecoder("shared", MobaOpCodes.Snapshot.ProjectileEvent, typeof(MobaProjectileEventSnapshotEntry[]))]
        internal static bool DecodeProjectileEvents(in WorldStateSnapshot snap, out MobaProjectileEventSnapshotEntry[] entries)
        {
            if (snap.Payload == null || snap.Payload.Length == 0)
            {
                entries = null;
                return false;
            }

            entries = MobaProjectileEventSnapshotCodec.Deserialize(snap.Payload);
            return true;
        }

        [SnapshotDecoder("shared", MobaOpCodes.Snapshot.AreaEvent, typeof(MobaAreaEventSnapshotEntry[]))]
        internal static bool DecodeAreaEvents(in WorldStateSnapshot snap, out MobaAreaEventSnapshotEntry[] entries)
        {
            if (snap.Payload == null || snap.Payload.Length == 0)
            {
                entries = null;
                return false;
            }

            entries = MobaAreaEventSnapshotCodec.Deserialize(snap.Payload);
            return true;
        }

        [SnapshotDecoder("shared", MobaOpCodes.Snapshot.DamageEvent, typeof(MobaDamageEventSnapshotEntry[]))]
        internal static bool DecodeDamageEvents(in WorldStateSnapshot snap, out MobaDamageEventSnapshotEntry[] entries)
        {
            if (snap.Payload == null || snap.Payload.Length == 0)
            {
                entries = null;
                return false;
            }

            entries = MobaDamageEventSnapshotCodec.Deserialize(snap.Payload);
            return true;
        }
    }
}
