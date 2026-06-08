using System;
using AbilityKit.Ability.Host;
using AbilityKit.Protocol.Moba;
using AbilityKit.Protocol.Moba.StateSync;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleHudSnapshotController : IDisposable
    {
        private readonly BattleSubscriptionGroup _subscriptions = new BattleSubscriptionGroup(2);

        private Action<EnterMobaGameRes> _enterGameReceived;
        private Action<MobaDamageEventSnapshotEntry[]> _damageEventsReceived;

        public void Bind(
            BattleContext ctx,
            Action<EnterMobaGameRes> enterGameReceived,
            Action<MobaDamageEventSnapshotEntry[]> damageEventsReceived)
        {
            Clear();

            _enterGameReceived = enterGameReceived;
            _damageEventsReceived = damageEventsReceived;

            if (ctx == null) return;
            if (!ctx.TryGetFrameSnapshots(out var snapshots)) return;

            _subscriptions.Add(snapshots.Subscribe<EnterMobaGameRes>(
                MobaOpCodes.Snapshot.EnterGame,
                OnEnterGameSnapshot));
            _subscriptions.Add(snapshots.Subscribe<MobaDamageEventSnapshotEntry[]>(
                MobaOpCodes.Snapshot.DamageEvent,
                OnDamageEventSnapshot));
        }

        public void Dispose()
        {
            Clear();
        }

        private void Clear()
        {
            _subscriptions.Clear();
            _enterGameReceived = null;
            _damageEventsReceived = null;
        }

        private void OnEnterGameSnapshot(ISnapshotEnvelope packet, EnterMobaGameRes res)
        {
            _enterGameReceived?.Invoke(res);
        }

        private void OnDamageEventSnapshot(ISnapshotEnvelope packet, MobaDamageEventSnapshotEntry[] entries)
        {
            _damageEventsReceived?.Invoke(entries);
        }
    }
}
