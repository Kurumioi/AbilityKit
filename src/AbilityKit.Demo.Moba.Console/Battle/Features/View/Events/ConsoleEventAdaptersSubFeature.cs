using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Share;
using AbilityKit.Demo.Moba.Console.Platform;
using AbilityKit.Demo.Moba.Console.View;
using ShareFrameSnapshotDispatcher = AbilityKit.Demo.Moba.Share.FrameSnapshotDispatcher;

namespace AbilityKit.Demo.Moba.Console.Battle.Features
{
    /// <summary>
    /// Event source mode for Console
    /// </summary>
    public enum ConsoleViewEventSourceMode
    {
        SnapshotOnly,
        TriggerOnly,
        Hybrid
    }

    /// <summary>
    /// Event adapters Module
    /// Manages snapshot and trigger event adapters
    /// 对齐 Unity EventAdaptersModule
    /// </summary>
    public sealed class ConsoleEventAdaptersModule : IConsoleViewModule
    {
        private const string ModuleId = "view_event_adapters";
        private ConsoleSnapshotViewAdapter? _snapshotAdapter;

        public string Id => ModuleId;
        public string[]? Dependencies => null;

        public void OnAttach(IConsoleViewFeatureModulesHost host)
        {
            var dispatcher = host.Context?.FrameSnapshots as ShareFrameSnapshotDispatcher;

            if (dispatcher != null)
            {
                _snapshotAdapter = new ConsoleSnapshotViewAdapter(dispatcher, host.EventSink);
                Platform.Log.View("[EventAdaptersModule] Created SnapshotViewAdapter");
            }
            else
            {
                Platform.Log.Warn("[EventAdaptersModule] Failed to create adapters: missing dispatcher");
            }
        }

        public void OnDetach(IConsoleViewFeatureModulesHost host)
        {
            _snapshotAdapter?.Dispose();
            _snapshotAdapter = null;
        }

        public void Tick(IConsoleViewFeatureModulesHost host, float deltaTime)
        {
        }

        public void Rebind(IConsoleViewFeatureModulesHost host)
        {
        }
    }

    /// <summary>
    /// Console snapshot view adapter
    /// Bridges FrameSnapshotDispatcher with ConsoleBattleViewEventSink
    /// Uses FrameSnapshotData for compatibility with BaseBattleViewEventSink
    /// </summary>
    public sealed class ConsoleSnapshotViewAdapter : IDisposable
    {
        private readonly ShareFrameSnapshotDispatcher _dispatcher;
        private readonly ConsoleBattleViewEventSink _sink;
        private bool _disposed;

        public ConsoleSnapshotViewAdapter(ShareFrameSnapshotDispatcher dispatcher, ConsoleBattleViewEventSink sink)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));

            Subscribe();
        }

        private void Subscribe()
        {
            _dispatcher.Subscribe(MobaOpCode.EnterGameSnapshot, (int frame, EnterGameData data) =>
            {
                var snapshot = new FrameSnapshotData(frame, 0, SnapshotType.Full, enterGame: data);
                _sink.OnEnterGameSnapshot(in snapshot);
            });

            _dispatcher.Subscribe(MobaOpCode.ActorSpawnSnapshot, (int frame, ActorSpawnData[] data) =>
            {
                var spawnList = new List<ActorSpawnData>(data);
                var snapshot = new FrameSnapshotData(frame, 0, SnapshotType.Full, actorSpawns: spawnList);
                _sink.OnActorSpawnSnapshot(in snapshot);
            });

            _dispatcher.Subscribe(MobaOpCode.ActorTransformSnapshot, (int frame, ActorTransformData[] data) =>
            {
                var transformList = new List<ActorTransformData>(data);
                var snapshot = new FrameSnapshotData(frame, 0, SnapshotType.Full, actorTransforms: transformList);
                _sink.OnActorTransformSnapshot(in snapshot);
            });

            _dispatcher.Subscribe(MobaOpCode.ProjectileEventSnapshot, (int frame, ProjectileEventData[] data) =>
            {
                var eventList = new List<ProjectileEventData>(data);
                var snapshot = new FrameSnapshotData(frame, 0, SnapshotType.Full, projectileEvents: eventList);
                _sink.OnProjectileEventSnapshot(in snapshot);
            });

            _dispatcher.Subscribe(MobaOpCode.AreaEventSnapshot, (int frame, AreaEventData[] data) =>
            {
                var eventList = new List<AreaEventData>(data);
                var snapshot = new FrameSnapshotData(frame, 0, SnapshotType.Full, areaEvents: eventList);
                _sink.OnAreaEventSnapshot(in snapshot);
            });

            _dispatcher.Subscribe(MobaOpCode.DamageEventSnapshot, (int frame, DamageEventData[] data) =>
            {
                var eventList = new List<DamageEventData>(data);
                var snapshot = new FrameSnapshotData(frame, 0, SnapshotType.Full, damageEvents: eventList);
                _sink.OnDamageEventSnapshot(in snapshot);
            });

            _dispatcher.Subscribe(MobaOpCode.StateHashSnapshot, (int frame, StateHashData data) =>
            {
                var snapshot = new FrameSnapshotData(frame, 0, SnapshotType.Full, stateHash: data);
                _sink.OnStateHashSnapshot(in snapshot);
            });

            Platform.Log.View("[SnapshotViewAdapter] Subscribed to all snapshot types");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _dispatcher.Clear();
            Platform.Log.View("[SnapshotViewAdapter] Disposed");
        }
    }
}
