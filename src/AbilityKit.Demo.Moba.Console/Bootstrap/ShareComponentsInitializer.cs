using System;
using AbilityKit.Demo.Moba.Console.Battle.Context;
using AbilityKit.Demo.Moba.Console.View;
using AbilityKit.Protocol.Moba;
using ShareSyncMode = AbilityKit.Demo.Moba.Share.SyncMode;
using ShareFrameSnapshotData = AbilityKit.Demo.Moba.Share.FrameSnapshotData;
using ShareSnapshotType = AbilityKit.Demo.Moba.Share.SnapshotType;
using ShareEnterGameData = AbilityKit.Demo.Moba.Share.EnterGameData;
using ShareActorTransformData = AbilityKit.Demo.Moba.Share.ActorTransformData;
using ShareProjectileEventData = AbilityKit.Demo.Moba.Share.ProjectileEventData;
using ShareAreaEventData = AbilityKit.Demo.Moba.Share.AreaEventData;
using ShareDamageEventData = AbilityKit.Demo.Moba.Share.DamageEventData;
using ShareStateHashData = AbilityKit.Demo.Moba.Share.StateHashData;
using ShareFrameSnapshotDispatcher = AbilityKit.Demo.Moba.Share.FrameSnapshotDispatcher;
using ShareActorSpawnData = AbilityKit.Demo.Moba.Share.ActorSpawnData;

namespace AbilityKit.Demo.Moba.Console.Bootstrap
{
    /// <summary>
    /// Share 层组件初始化器
    /// 负责初始化快照分发器、视图事件接收器和事件订阅
    /// </summary>
    public sealed class ShareComponentsInitializer : IDisposable
    {
        private readonly IConsoleBattleView _battleView;
        private readonly string _playerId;

        private ShareFrameSnapshotDispatcher? _snapshotDispatcher;
        private ConsoleBattleViewEventSink? _viewEventSink;

        public ShareFrameSnapshotDispatcher? SnapshotDispatcher => _snapshotDispatcher;
        public ConsoleBattleViewEventSink? ViewEventSink => _viewEventSink;

        public ShareComponentsInitializer(IConsoleBattleView battleView, string playerId)
        {
            _battleView = battleView ?? throw new ArgumentNullException(nameof(battleView));
            _playerId = playerId ?? throw new ArgumentNullException(nameof(playerId));
        }

        /// <summary>
        /// 初始化 Share 组件
        /// </summary>
        public void Initialize()
        {
            _snapshotDispatcher = new ShareFrameSnapshotDispatcher();
            _viewEventSink = new ConsoleBattleViewEventSink(_battleView, _playerId);

            Platform.Log.System("[Share] Share components initialized");
        }

        /// <summary>
        /// 订阅所有快照事件
        /// </summary>
        public void SubscribeAll()
        {
            if (_snapshotDispatcher == null || _viewEventSink == null)
            {
                Platform.Log.Warn("[Share] SnapshotDispatcher or ViewEventSink not initialized");
                return;
            }

            var dispatcher = _snapshotDispatcher;

            dispatcher.Subscribe(MobaOpCodes.Snapshot.EnterGame, (int frame, ShareEnterGameData data) =>
            {
                var snapshotData = new ShareFrameSnapshotData(frame, 0, ShareSnapshotType.Full, enterGame: data);
                _viewEventSink.OnEnterGameSnapshot(in snapshotData);
            });

            dispatcher.Subscribe(MobaOpCodes.Snapshot.ActorSpawn, (int frame, ShareActorSpawnData[] data) =>
            {
                var snapshotData = new ShareFrameSnapshotData(frame, 0, ShareSnapshotType.Full, actorSpawns: data);
                _viewEventSink.OnActorSpawnSnapshot(in snapshotData);
            });

            dispatcher.Subscribe(MobaOpCodes.Snapshot.ActorTransform, (int frame, ShareActorTransformData[] data) =>
            {
                var snapshotData = new ShareFrameSnapshotData(frame, 0, ShareSnapshotType.Full, actorTransforms: data);
                _viewEventSink.OnActorTransformSnapshot(in snapshotData);
            });

            dispatcher.Subscribe(MobaOpCodes.Snapshot.ProjectileEvent, (int frame, ShareProjectileEventData[] data) =>
            {
                var snapshotData = new ShareFrameSnapshotData(frame, 0, ShareSnapshotType.Full, projectileEvents: data);
                _viewEventSink.OnProjectileEventSnapshot(in snapshotData);
            });

            dispatcher.Subscribe(MobaOpCodes.Snapshot.AreaEvent, (int frame, ShareAreaEventData[] data) =>
            {
                var snapshotData = new ShareFrameSnapshotData(frame, 0, ShareSnapshotType.Full, areaEvents: data);
                _viewEventSink.OnAreaEventSnapshot(in snapshotData);
            });

            dispatcher.Subscribe(MobaOpCodes.Snapshot.DamageEvent, (int frame, ShareDamageEventData[] data) =>
            {
                var snapshotData = new ShareFrameSnapshotData(frame, 0, ShareSnapshotType.Full, damageEvents: data);
                _viewEventSink.OnDamageEventSnapshot(in snapshotData);
            });

            dispatcher.Subscribe(MobaOpCodes.Snapshot.StateHash, (int frame, ShareStateHashData data) =>
            {
                var snapshotData = new ShareFrameSnapshotData(frame, 0, ShareSnapshotType.Full, stateHash: data);
                _viewEventSink.OnStateHashSnapshot(in snapshotData);
            });

            Platform.Log.System("[Share] Snapshot subscriptions initialized");
        }

        /// <summary>
        /// 分发角色生成事件
        /// </summary>
        public void DispatchActorSpawn(int frame, ShareActorSpawnData[] spawnData)
        {
            _snapshotDispatcher?.DispatchActorSpawn(frame, spawnData);
        }

        public void Dispose()
        {
            _snapshotDispatcher?.Dispose();
            _viewEventSink?.Dispose();
            _snapshotDispatcher = null;
            _viewEventSink = null;
        }
    }
}
