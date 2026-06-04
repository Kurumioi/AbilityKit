using System;
using AbilityKit.Protocol.Moba;
using AbilityKit.Protocol.Moba;
using AbilityKit.Core.Common.SnapshotRouting;
using AbilityKit.Protocol.Moba.StateSync;

namespace AbilityKit.Game.Flow.Battle.ViewEvents.Snapshot
{
    public sealed class BattleSnapshotViewAdapter : IDisposable
    {
        private readonly FrameSnapshotDispatcher _snapshots;
        private readonly IBattleViewEventSink _sink;

        private IDisposable _subEnterGame;
        private IDisposable _subActorTransform;
        private IDisposable _subProjectileEvents;
        private IDisposable _subAreaEvents;
        private IDisposable _subDamageEvents;

        public BattleSnapshotViewAdapter(FrameSnapshotDispatcher snapshots, IBattleViewEventSink sink)
        {
            _snapshots = snapshots;
            _sink = sink;

            if (_snapshots == null || _sink == null) return;

            _subEnterGame = _snapshots.Subscribe<EnterMobaGameRes>(MobaOpCodes.Snapshot.EnterGame, _sink.OnEnterGameSnapshot);
            _subActorTransform = _snapshots.Subscribe<MobaActorTransformSnapshotEntry[]>(MobaOpCodes.Snapshot.ActorTransform, _sink.OnActorTransformSnapshot);
            _subProjectileEvents = _snapshots.Subscribe<MobaProjectileEventSnapshotEntry[]>(MobaOpCodes.Snapshot.ProjectileEvent, _sink.OnProjectileEventSnapshot);
            _subAreaEvents = _snapshots.Subscribe<MobaAreaEventSnapshotEntry[]>(MobaOpCodes.Snapshot.AreaEvent, _sink.OnAreaEventSnapshot);
            _subDamageEvents = _snapshots.Subscribe<MobaDamageEventSnapshotEntry[]>(MobaOpCodes.Snapshot.DamageEvent, _sink.OnDamageEventSnapshot);
        }

        public void Dispose()
        {
            _subEnterGame?.Dispose();
            _subActorTransform?.Dispose();
            _subProjectileEvents?.Dispose();
            _subAreaEvents?.Dispose();
            _subDamageEvents?.Dispose();

            _subEnterGame = null;
            _subActorTransform = null;
            _subProjectileEvents = null;
            _subAreaEvents = null;
            _subDamageEvents = null;
        }
    }
}

