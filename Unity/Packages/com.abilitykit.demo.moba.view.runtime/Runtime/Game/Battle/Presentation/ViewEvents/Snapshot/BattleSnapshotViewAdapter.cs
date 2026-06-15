using System;
using AbilityKit.Ability.Host;
using AbilityKit.Core.Snapshots.Routing;
using AbilityKit.Demo.Moba.Share;
using AbilityKit.Protocol.Moba;
using AbilityKit.Protocol.Moba.StateSync;
using FrameSnapshotDispatcher = AbilityKit.Core.Snapshots.Routing.FrameSnapshotDispatcher;

namespace AbilityKit.Game.Flow.Battle.ViewEvents.Snapshot
{
    public sealed class BattleSnapshotViewAdapter : IDisposable
    {
        private readonly FrameSnapshotDispatcher _snapshots;
        private readonly IBattleViewEventSink _sink;
        private readonly BattleSubscriptionGroup _subscriptions = new BattleSubscriptionGroup(6);

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
            _subscriptions.Add(_snapshots.Subscribe<MobaPresentationCueSnapshotEntry[]>(
                MobaOpCodes.Snapshot.PresentationCue,
                OnPresentationCueSnapshot));
        }

        private void OnPresentationCueSnapshot(ISnapshotEnvelope packet, MobaPresentationCueSnapshotEntry[] entries)
        {
            _sink.OnPresentationCueSnapshot(packet, PresentationCueSnapshotMapper.Map(entries));
        }

        public void Dispose()
        {
            _subscriptions.Clear();
        }
    }
}
