using System;
using System.Collections.Generic;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.DI;
using AbilityKit.Coordinator.Core;

namespace AbilityKit.Coordinator
{
    /// <summary>
    /// Session Coordinator Implementation
    ///
    /// Design:
    /// - Manages session lifecycle and coordination
    /// - Coordinates World, SyncAdapter, and SubFeatures
    /// - Provides unified access to session resources
    /// </summary>
    public sealed class SessionCoordinator : ISessionCoordinator
    {
        // ============== State ==============

        private SessionConfig _config;
        private ISessionCoordinatorHost _host;
        private SessionState _state = SessionState.Idle;

        // ============== World ==============

        private IWorldHost _worldHost;
        private IWorld _world;
        private IWorldResolver _worldResolver;

        // ============== Sync ==============

        private ISyncAdapter _syncAdapter;
        private IBattleDriverHost _driverHost;
        private Timeline.IViewTimeline _viewTimeline;

        // ============== View ==============

        private IViewEventSink _viewEventSink;

        // ============== SubFeatures ==============

        private readonly List<SubFeatures.ISessionSubFeature> _subFeatures = new List<SubFeatures.ISessionSubFeature>();

        // ============== Hooks ==============

        private readonly SessionHooks _hooks = new SessionHooks();

        // ============== Properties ==============

        public SessionId SessionId => _config.SessionId;
        public SessionConfig Config => _config;
        public SessionState State => _state;

        public IWorldHost WorldHost => _worldHost;
        public IWorld World => _world;
        public IWorldResolver WorldResolver => _worldResolver;

        public ISyncAdapter SyncAdapter => _syncAdapter;
        public Timeline.IViewTimeline ViewTimeline => _viewTimeline;

        public IBattleDriverHost? DriverHost => _driverHost;
        public IViewEventSink? ViewEventSink => _viewEventSink;

        public SessionHooks Hooks => _hooks;

        // ============== Constructor ==============

        public SessionCoordinator()
        {
            _config = SessionConfig.Default;
            _state = SessionState.Idle;
        }

        // ============== Lifecycle ==============

        public void Initialize(SessionConfig config, ISessionCoordinatorHost host)
        {
            if (_state != SessionState.Idle)
            {
                throw new InvalidOperationException($"Cannot initialize session in state {_state}");
            }

            _state = SessionState.Initializing;
            _config = config;
            _host = host;

            try
            {
                // Create WorldHost
                _worldHost = host.CreateWorldHost(config);

                // Create World
                var worldOptions = CreateWorldOptions(config);
                _world = _worldHost.CreateWorld(worldOptions);
                _worldResolver = _world.Services;

                // Load config
                host.LoadConfig(_world, config);

                // Register services
                host.RegisterServices(_world, config);

                // Create ViewTimeline
                _viewTimeline = new Timeline.ViewTimeline();

                // Create SyncAdapter
                _syncAdapter = SyncAdapterFactory.Create(_world, config);
                _syncAdapter.Attach(this);

                // Attach driver host if available
                if (_driverHost != null)
                {
                    _syncAdapter.SetDriverHost(_driverHost);
                }

                // Invoke hooks
                _hooks.InvokeSessionStarting(config);

                _state = SessionState.Idle;
            }
            catch (Exception ex)
            {
                _state = SessionState.Error;
                _hooks.InvokeSessionFailed(ex);
                throw;
            }
        }

        public void Start()
        {
            if (_state != SessionState.Idle)
            {
                throw new InvalidOperationException($"Cannot start session in state {_state}");
            }

            _state = SessionState.Running;

            // Create player spawns
            var spawns = _host.CreatePlayerSpawnData(_config);
            CreatePlayerSpawns(spawns);

            // Start SyncAdapter
            _syncAdapter?.Attach(this, _driverHost);

            // Invoke hooks
            _hooks.InvokeSessionStarted(_config);
            _hooks.InvokeFirstFrameReceived();
        }

        public void Stop()
        {
            if (_state != SessionState.Running)
            {
                return;
            }

            _state = SessionState.Stopping;
            _hooks.InvokeSessionStopping();

            // Detach subfeatures
            foreach (var sf in _subFeatures)
            {
                sf.OnDetach();
            }
            _subFeatures.Clear();

            _state = SessionState.Stopped;
            _hooks.InvokeSessionStopped();
        }

        public void Destroy()
        {
            Stop();

            // Dispose SyncAdapter
            _syncAdapter?.Dispose();
            _syncAdapter = null;

            // Dispose ViewTimeline
            _viewTimeline?.Dispose();
            _viewTimeline = null;

            // Dispose World
            if (_worldHost != null && _world != null)
            {
                _worldHost.DestroyWorld(_world.Id);
            }
            _world?.Dispose();
            _world = null;
            _worldHost = null;
            _worldResolver = null;

            // Clear hooks
            _hooks.Clear();

            _state = SessionState.Idle;
        }

        // ============== Driver & View ==============

        public void SetDriverHost(IBattleDriverHost driverHost)
        {
            _driverHost = driverHost;
            if (_syncAdapter != null)
            {
                _syncAdapter.SetDriverHost(driverHost);
            }
        }

        public void SetViewEventSink(IViewEventSink sink)
        {
            _viewEventSink = sink;
        }

        /// <summary>
        /// Notify view event sink of battle start
        /// Called by sync adapters or directly by application
        /// </summary>
        public void NotifyBattleStart(int frame)
        {
            _viewEventSink?.OnBattleStart(frame);
        }

        /// <summary>
        /// Notify view event sink of battle end
        /// Called by sync adapters or directly by application
        /// </summary>
        public void NotifyBattleEnd(int frame, int winTeamId)
        {
            _viewEventSink?.OnBattleEnd(frame, winTeamId);
        }

        /// <summary>
        /// Notify view event sink of frame sync complete
        /// Called by sync adapters after each frame
        /// </summary>
        public void NotifyFrameSyncComplete(int frame)
        {
            _viewEventSink?.OnFrameSyncComplete(frame);
        }

        /// <summary>
        /// Notify view event sink of enter game snapshot
        /// Called when entering game with initial state
        /// </summary>
        public void NotifyEnterGameSnapshot(in FrameSnapshotData snapshot)
        {
            _viewEventSink?.OnEnterGameSnapshot(in snapshot);
        }

        /// <summary>
        /// Notify view event sink of actor transform snapshot
        /// Called when actor positions change
        /// </summary>
        public void NotifyActorTransformSnapshot(in FrameSnapshotData snapshot)
        {
            _viewEventSink?.OnActorTransformSnapshot(in snapshot);
        }

        /// <summary>
        /// Notify view event sink of damage events
        /// Called when damage occurs
        /// </summary>
        public void NotifyDamageSnapshot(in FrameSnapshotData snapshot)
        {
            _viewEventSink?.OnDamageEventSnapshot(in snapshot);
        }

        /// <summary>
        /// Notify view event sink of custom event
        /// Called for game-specific events
        /// </summary>
        public void NotifyCustomEvent(string eventType, int entityId, byte[] customData)
        {
            _viewEventSink?.OnCustomEvent(eventType, entityId, customData);
        }

        // ============== Input ==============

        public void SubmitLocalInput(PlayerInput input)
        {
            _syncAdapter?.SubmitInput(input);
        }

        // ============== Service Resolution ==============

        public T Resolve<T>() where T : class
        {
            if (_worldResolver == null)
            {
                throw new InvalidOperationException("World not initialized");
            }
            return _worldResolver.Resolve<T>();
        }

        public bool TryResolve<T>(out T service) where T : class
        {
            service = default;
            if (_worldResolver == null)
            {
                return false;
            }
            return _worldResolver.TryResolve(out service);
        }

        // ============== Tick ==============

        public void Tick(float deltaTime)
        {
            if (_state != SessionState.Running)
            {
                return;
            }

            _hooks.InvokePreTick(deltaTime);

            // SubFeature PreTick
            foreach (var sf in _subFeatures)
            {
                if (sf is SubFeatures.ISessionPreTickSubFeature preTick)
                {
                    preTick.OnPreTick(deltaTime);
                }
            }

            // SyncAdapter Tick
            _syncAdapter?.Tick(deltaTime);

            // World Tick
            _worldHost?.Tick(deltaTime);

            // SubFeature PostTick
            foreach (var sf in _subFeatures)
            {
                if (sf is SubFeatures.ISessionPostTickSubFeature postTick)
                {
                    postTick.OnPostTick(deltaTime);
                }
            }

            _hooks.InvokePostTick(deltaTime);
        }

        // ============== SubFeature Management ==============

        public void AddSubFeature(SubFeatures.ISessionSubFeature subFeature)
        {
            if (subFeature == null) return;
            _subFeatures.Add(subFeature);
        }

        public void RemoveSubFeature(SubFeatures.ISessionSubFeature subFeature)
        {
            if (subFeature == null) return;
            subFeature.OnDetach();
            _subFeatures.Remove(subFeature);
        }

        // ============== Private Methods ==============

        private WorldCreateOptions CreateWorldOptions(SessionConfig config)
        {
            return new WorldCreateOptions
            {
                Id = new WorldId(config.WorldId.ToString()),
                WorldType = "BattleWorld"
            };
        }

        private void CreatePlayerSpawns(PlayerSpawnData[] spawns)
        {
            if (spawns == null || spawns.Length == 0)
            {
                return;
            }

            // Get spawn service from world if available
            // TODO: Implement player spawn logic
        }

        // ============== IDisposable ==============

        public void Dispose()
        {
            Destroy();
        }
    }
}
