using System;
using AbilityKit.Ability.Host;
using AbilityKit.Demo.Moba.Share;
using AbilityKit.Protocol.Moba;
using AbilityKit.Protocol.Moba.StateSync;

namespace ET.Logic
{
    public static class ETBattleWorldSnapshotAdapter
    {
        public static bool TryConvert(in WorldStateSnapshot snapshot, int frameIndex, double timestamp, out FrameSnapshotData frameSnapshot)
        {
            switch (snapshot.OpCode)
            {
                case MobaOpCodes.Snapshot.ActorTransform:
                    return TryConvertActorTransform(in snapshot, frameIndex, timestamp, out frameSnapshot);

                case MobaOpCodes.Snapshot.ActorSpawn:
                    return TryConvertActorSpawn(in snapshot, frameIndex, timestamp, out frameSnapshot);

                case MobaOpCodes.Snapshot.DamageEvent:
                    return TryConvertDamageEvent(in snapshot, frameIndex, timestamp, out frameSnapshot);

                case MobaOpCodes.Snapshot.PresentationCue:
                    return TryConvertPresentationCue(in snapshot, frameIndex, timestamp, out frameSnapshot);

                case MobaOpCodes.Snapshot.StateHash:
                    return TryConvertStateHash(in snapshot, frameIndex, timestamp, out frameSnapshot);

                default:
                    frameSnapshot = default;
                    return false;
            }
        }

        private static bool TryConvertActorTransform(in WorldStateSnapshot snapshot, int frameIndex, double timestamp, out FrameSnapshotData frameSnapshot)
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

        private static bool TryConvertActorSpawn(in WorldStateSnapshot snapshot, int frameIndex, double timestamp, out FrameSnapshotData frameSnapshot)
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

        private static bool TryConvertDamageEvent(in WorldStateSnapshot snapshot, int frameIndex, double timestamp, out FrameSnapshotData frameSnapshot)
        {
            var entries = MobaDamageEventSnapshotCodec.Deserialize(snapshot.Payload);
            if (entries.Length == 0)
            {
                frameSnapshot = default;
                return false;
            }

            var damageEvents = new DamageEventData[entries.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                damageEvents[i] = new DamageEventData(
                    attackerId: entry.AttackerActorId,
                    targetId: entry.TargetActorId,
                    sourceId: entry.ReasonParam,
                    damageType: entry.DamageType,
                    damageValue: (int)MathF.Round(entry.Value),
                    targetHpAfter: (int)MathF.Round(entry.TargetHp),
                    isKill: entry.TargetHp <= 0f);
            }

            frameSnapshot = new FrameSnapshotData(
                frameIndex: frameIndex,
                timestamp: timestamp,
                type: SnapshotType.Delta,
                damageEvents: damageEvents);
            return true;
        }

        private static bool TryConvertPresentationCue(in WorldStateSnapshot snapshot, int frameIndex, double timestamp, out FrameSnapshotData frameSnapshot)
        {
            var entries = MobaPresentationCueSnapshotCodec.Deserialize(snapshot.Payload);
            if (entries.Length == 0)
            {
                frameSnapshot = default;
                return false;
            }

            frameSnapshot = new FrameSnapshotData(
                frameIndex: frameIndex,
                timestamp: timestamp,
                type: SnapshotType.Delta,
                presentationCues: PresentationCueSnapshotMapper.Map(entries));
            return true;
        }

        private static bool TryConvertStateHash(in WorldStateSnapshot snapshot, int frameIndex, double timestamp, out FrameSnapshotData frameSnapshot)
        {
            var payload = MobaStateHashSnapshotCodec.Deserialize(snapshot.Payload);
            if (payload.Version != MobaStateHashSnapshotCodec.Version || payload.Frame < 0 || payload.Hash == 0)
            {
                frameSnapshot = default;
                return false;
            }

            frameSnapshot = new FrameSnapshotData(
                frameIndex: frameIndex,
                timestamp: timestamp,
                type: SnapshotType.Delta,
                stateHash: new StateHashData(payload.Frame, payload.Hash));
            return true;
        }
    }
}
