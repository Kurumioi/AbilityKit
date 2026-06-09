using System;
using AbilityKit.Ability.Host;
using AbilityKit.Protocol.Moba;
using AbilityKit.Protocol.Moba.StateSync;

namespace AbilityKit.Game.Flow
{
    internal sealed class BattleHudSnapshotController : IDisposable
    {
        private readonly BattleHudSnapshotControllerFactory _factory;
        private readonly BattleSubscriptionGroup _subscriptions;

        private Action<EnterMobaGameRes> _enterGameReceived;
        private Action<MobaDamageEventSnapshotEntry[]> _damageEventsReceived;

        public BattleHudSnapshotController(BattleHudSnapshotControllerFactory factory = null)
        {
            _factory = factory ?? new BattleHudSnapshotControllerFactory();
            _subscriptions = _factory.CreateSubscriptions();
        }

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

            _factory.BindSnapshots(
                _subscriptions,
                snapshots,
                OnEnterGameSnapshot,
                OnDamageEventSnapshot);
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

    internal sealed class BattleHudSnapshotControllerFactory
    {
        public BattleSubscriptionGroup CreateSubscriptions()
        {
            return new BattleSubscriptionGroup(2);
        }

        public void BindSnapshots(
            BattleSubscriptionGroup subscriptions,
            AbilityKit.Core.Common.SnapshotRouting.FrameSnapshotDispatcher snapshots,
            Action<ISnapshotEnvelope, EnterMobaGameRes> enterGameReceived,
            Action<ISnapshotEnvelope, MobaDamageEventSnapshotEntry[]> damageEventsReceived)
        {
            if (subscriptions == null) return;
            if (snapshots == null) return;

            subscriptions.Add(snapshots.Subscribe<EnterMobaGameRes>(
                MobaOpCodes.Snapshot.EnterGame,
                enterGameReceived));
            subscriptions.Add(snapshots.Subscribe<MobaDamageEventSnapshotEntry[]>(
                MobaOpCodes.Snapshot.DamageEvent,
                damageEventsReceived));
        }
    }
}
