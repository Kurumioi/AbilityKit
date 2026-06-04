using System;
using AbilityKit.Ability.Host;
using AbilityKit.Demo.Moba.Share;
using AbilityKit.Protocol.Moba;
using AbilityKit.Protocol.Moba.StateSync;

namespace ET.Logic
{
    [RuntimeSnapshotConverter(MobaOpCodes.Snapshot.DamageEvent)]
    public sealed class DamageEventSnapshotConverter : IRuntimeSnapshotConverter
    {
        public int OpCode => MobaOpCodes.Snapshot.DamageEvent;

        public bool TryConvert(in WorldStateSnapshot snapshot, int frameIndex, double timestamp, out FrameSnapshotData frameSnapshot)
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
    }
}
