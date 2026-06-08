using System;
using AbilityKit.Core.Common.SnapshotRouting;
using AbilityKit.Game.Flow;
using AbilityKit.Protocol.Moba;
using AbilityKit.Protocol.Moba.StateSync;

namespace AbilityKit.Game.Flow.Battle.ViewEvents.Snapshot
{
    public sealed class BattleSnapshotViewAdapter : IDisposable
    {
        private readonly FrameSnapshotDispatcher _snapshots;
        private readonly IBattleViewEventSink _sink;
        private readonly BattleSubscriptionGroup _subscriptions = new BattleSubscriptionGroup(5);

        public BattleSnapshotViewAdapter(FrameSnapshotDispatcher snapshots, IBattleViewEventSink sink)
        {
            _snapshots = snapshots;
            _sink = sink;

            if (_snapshots == null || _sink == null) return;

            _subscriptions.Add(_snapshots.Subscribe<EnterMobaGameRes>(
                MobaOpCodes.Snapshot.EnterGame,
                _sink.OnEnterGameSnapshot));
            _subscriptions.Add(_snapshots.Subscribe<MobaActorTransformSnapshotEntry[]>(
                MobaOpCodes.Snapshot.ActorTransform,
                _sink.OnActorTransformSnapshot));
            _subscriptions.Add(_snapshots.Subscribe<MobaProjectileEventSnapshotEntry[]>(
                MobaOpCodes.Snapshot.ProjectileEvent,
                _sink.OnProjectileEventSnapshot));
            _subscriptions.Add(_snapshots.Subscribe<MobaAreaEventSnapshotEntry[]>(
                MobaOpCodes.Snapshot.AreaEvent,
                _sink.OnAreaEventSnapshot));
            _subscriptions.Add(_snapshots.Subscribe<MobaDamageEventSnapshotEntry[]>(
                MobaOpCodes.Snapshot.DamageEvent,
                _sink.OnDamageEventSnapshot));
        }

        public void Dispose()
        {
            _subscriptions.Clear();
        }
    }
}
