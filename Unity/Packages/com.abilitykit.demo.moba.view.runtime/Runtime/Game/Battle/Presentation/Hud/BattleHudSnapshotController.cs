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
        private Action<MobaSkillStateSnapshotEntry[]> _skillStatesReceived;

        public BattleHudSnapshotController(BattleHudSnapshotControllerFactory factory = null)
        {
            _factory = factory ?? new BattleHudSnapshotControllerFactory();
            _subscriptions = _factory.CreateSubscriptions();
        }

        public bool IsBound { get; private set; }

        public bool Bind(
            BattleContext ctx,
            Action<EnterMobaGameRes> enterGameReceived,
            Action<MobaDamageEventSnapshotEntry[]> damageEventsReceived,
            Action<MobaSkillStateSnapshotEntry[]> skillStatesReceived)
        {
            ClearSubscriptions();

            _enterGameReceived = enterGameReceived;
            _damageEventsReceived = damageEventsReceived;
            _skillStatesReceived = skillStatesReceived;

            if (ctx == null) return false;
            if (!ctx.TryGetFrameSnapshots(out var snapshots)) return false;

            _factory.BindSnapshots(
                _subscriptions,
                snapshots,
                OnEnterGameSnapshot,
                OnDamageEventSnapshot,
                OnSkillStateSnapshot);
            IsBound = true;
            return true;
        }

        public void Dispose()
        {
            Clear();
        }

        private void Clear()
        {
            ClearSubscriptions();
            _enterGameReceived = null;
            _damageEventsReceived = null;
            _skillStatesReceived = null;
        }

        private void ClearSubscriptions()
        {
            _subscriptions.Clear();
            IsBound = false;
        }

        private void OnEnterGameSnapshot(ISnapshotEnvelope packet, EnterMobaGameRes res)
        {
            _enterGameReceived?.Invoke(res);
        }

        private void OnDamageEventSnapshot(ISnapshotEnvelope packet, MobaDamageEventSnapshotEntry[] entries)
        {
            _damageEventsReceived?.Invoke(entries);
        }

        private void OnSkillStateSnapshot(ISnapshotEnvelope packet, MobaSkillStateSnapshotEntry[] entries)
        {
            _skillStatesReceived?.Invoke(entries);
        }
    }

    internal sealed class BattleHudSnapshotControllerFactory
    {
        public BattleSubscriptionGroup CreateSubscriptions()
        {
            return new BattleSubscriptionGroup(3);
        }

        public void BindSnapshots(
            BattleSubscriptionGroup subscriptions,
            AbilityKit.Core.Snapshots.Routing.FrameSnapshotDispatcher snapshots,
            Action<ISnapshotEnvelope, EnterMobaGameRes> enterGameReceived,
            Action<ISnapshotEnvelope, MobaDamageEventSnapshotEntry[]> damageEventsReceived,
            Action<ISnapshotEnvelope, MobaSkillStateSnapshotEntry[]> skillStatesReceived)
        {
            if (subscriptions == null) return;
            if (snapshots == null) return;

            subscriptions.Add(snapshots.Subscribe<EnterMobaGameRes>(
                MobaOpCodes.Snapshot.EnterGame,
                enterGameReceived));
            subscriptions.Add(snapshots.Subscribe<MobaDamageEventSnapshotEntry[]>(
                MobaOpCodes.Snapshot.DamageEvent,
                damageEventsReceived));
            subscriptions.Add(snapshots.Subscribe<MobaSkillStateSnapshotEntry[]>(
                MobaOpCodes.Snapshot.SkillState,
                skillStatesReceived));
        }
    }
}
