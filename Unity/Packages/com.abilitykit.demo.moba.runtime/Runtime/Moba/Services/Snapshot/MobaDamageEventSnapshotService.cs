using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.World.Services;
using AbilityKit.Protocol.Moba.StateSync;

namespace AbilityKit.Demo.Moba.Services
{
    public sealed class MobaDamageEventSnapshotService : IService
    {
        private readonly MobaGamePhaseService _phase;

        private FrameIndex _lastFrame;

        private readonly List<MobaDamageEventSnapshotEntry> _events = new List<MobaDamageEventSnapshotEntry>(64);
        private readonly List<MobaDamageEventSnapshotEntry> _drain = new List<MobaDamageEventSnapshotEntry>(64);

        public MobaDamageEventSnapshotService(MobaGamePhaseService phase)
        {
            _phase = phase ?? throw new ArgumentNullException(nameof(phase));
            _lastFrame = new FrameIndex(-999999);
        }

        public void ReportDamage(int attackerActorId, int targetActorId, int damageType, float value, int reasonKind, int reasonParam, float targetHp, float targetMaxHp)
        {
            if (targetActorId <= 0) return;
            if (value == 0f) return;
            _events.Add(new MobaDamageEventSnapshotEntry
            {
                Kind = (int)DamageEventKind.Damage,
                AttackerActorId = attackerActorId,
                TargetActorId = targetActorId,
                DamageType = damageType,
                Value = value,
                ReasonKind = reasonKind,
                ReasonParam = reasonParam,
                TargetHp = targetHp,
                TargetMaxHp = targetMaxHp
            });
        }

        public void ReportHeal(int healerActorId, int targetActorId, int healType, float value, int reasonKind, int reasonParam, float targetHp, float targetMaxHp)
        {
            if (targetActorId <= 0) return;
            if (value == 0f) return;
            _events.Add(new MobaDamageEventSnapshotEntry
            {
                Kind = (int)DamageEventKind.Heal,
                AttackerActorId = healerActorId,
                TargetActorId = targetActorId,
                DamageType = healType,
                Value = value,
                ReasonKind = reasonKind,
                ReasonParam = reasonParam,
                TargetHp = targetHp,
                TargetMaxHp = targetMaxHp
            });
        }

        public bool TryGetSnapshot(FrameIndex frame, out WorldStateSnapshot snapshot)
        {
            if (!_phase.InGame)
            {
                snapshot = default;
                return false;
            }

            if (frame.Value == _lastFrame.Value)
            {
                snapshot = default;
                return false;
            }
            _lastFrame = frame;

            if (_events.Count == 0)
            {
                snapshot = default;
                return false;
            }

            _drain.Clear();
            _drain.AddRange(_events);
            _events.Clear();

            var payload = MobaDamageEventSnapshotCodec.Serialize(_drain.ToArray());
            snapshot = new WorldStateSnapshot((int)MobaOpCode.DamageEventSnapshot, payload);
            return true;
        }

        public void Dispose()
        {
        }
    }
}
