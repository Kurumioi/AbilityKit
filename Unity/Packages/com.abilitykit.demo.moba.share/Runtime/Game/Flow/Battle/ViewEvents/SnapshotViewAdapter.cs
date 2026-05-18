using System;
using System.Collections.Generic;

namespace AbilityKit.Demo.Moba.Share
{
    /// <summary>
    /// 快照视图事件适配器
    /// 将帧快照分发器的事件适配到视图事件接收器
    /// 参考 view.runtime 的 BattleSnapshotViewAdapter 实现
    /// </summary>
    public sealed class SnapshotViewAdapter : IDisposable
    {
        private readonly FrameSnapshotDispatcher _snapshots;
        private readonly IBattleViewEventSink _sink;

        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();

        /// <summary>
        /// 构造快照视图适配器
        /// </summary>
        /// <param name="snapshots">快照分发器</param>
        /// <param name="sink">视图事件接收器</param>
        public SnapshotViewAdapter(FrameSnapshotDispatcher snapshots, IBattleViewEventSink sink)
        {
            _snapshots = snapshots;
            _sink = sink;

            if (_snapshots == null || _sink == null) return;

            Subscribe();
        }

        private void Subscribe()
        {
            _subscriptions.Add(_snapshots.Subscribe<EnterGameData>((int)MobaOpCode.EnterGameSnapshot, OnEnterGameSnapshot));
            _subscriptions.Add(_snapshots.Subscribe<ActorTransformData[]>((int)MobaOpCode.ActorTransformSnapshot, OnActorTransformSnapshot));
            _subscriptions.Add(_snapshots.Subscribe<ProjectileEventData[]>((int)MobaOpCode.ProjectileEventSnapshot, OnProjectileEventSnapshot));
            _subscriptions.Add(_snapshots.Subscribe<AreaEventData[]>((int)MobaOpCode.AreaEventSnapshot, OnAreaEventSnapshot));
            _subscriptions.Add(_snapshots.Subscribe<DamageEventData[]>((int)MobaOpCode.DamageEventSnapshot, OnDamageEventSnapshot));
            _subscriptions.Add(_snapshots.Subscribe<StateHashData>((int)MobaOpCode.StateHashSnapshot, OnStateHashSnapshot));
        }

        private void OnEnterGameSnapshot(int frameIndex, EnterGameData data)
        {
            var snapshot = new FrameSnapshotData(
                frameIndex, 
                timestamp: 0, 
                SnapshotType.Full, 
                enterGame: data);
            _sink.OnEnterGameSnapshot(in snapshot);
        }

        private void OnActorTransformSnapshot(int frameIndex, ActorTransformData[] data)
        {
            var snapshot = new FrameSnapshotData(
                frameIndex, 
                timestamp: 0, 
                SnapshotType.Delta, 
                actorTransforms: data);
            _sink.OnActorTransformSnapshot(in snapshot);
        }

        private void OnProjectileEventSnapshot(int frameIndex, ProjectileEventData[] data)
        {
            var snapshot = new FrameSnapshotData(
                frameIndex, 
                timestamp: 0, 
                SnapshotType.Delta, 
                projectileEvents: data);
            _sink.OnProjectileEventSnapshot(in snapshot);
        }

        private void OnAreaEventSnapshot(int frameIndex, AreaEventData[] data)
        {
            var snapshot = new FrameSnapshotData(
                frameIndex, 
                timestamp: 0, 
                SnapshotType.Delta, 
                areaEvents: data);
            _sink.OnAreaEventSnapshot(in snapshot);
        }

        private void OnDamageEventSnapshot(int frameIndex, DamageEventData[] data)
        {
            var snapshot = new FrameSnapshotData(
                frameIndex, 
                timestamp: 0, 
                SnapshotType.Delta, 
                damageEvents: data);
            _sink.OnDamageEventSnapshot(in snapshot);
        }

        private void OnStateHashSnapshot(int frameIndex, StateHashData data)
        {
            var snapshot = new FrameSnapshotData(
                frameIndex, 
                timestamp: 0, 
                SnapshotType.Delta, 
                stateHash: data);
            _sink.OnStateHashSnapshot(in snapshot);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            for (int i = 0; i < _subscriptions.Count; i++)
            {
                _subscriptions[i]?.Dispose();
            }
            _subscriptions.Clear();
        }
    }
}
