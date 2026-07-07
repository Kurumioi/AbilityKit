using System;
using System.Collections.Generic;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.DI;
using AbilityKit.Coordinator.Core;
using AbilityKit.Core.Logging;

namespace AbilityKit.Coordinator
{
    /// <summary>
    /// 会话协调器实现。
    ///
    /// 设计：
    /// - 管理会话生命周期和协调流程。
    /// - 协调 World、SyncAdapter 和 SubFeatures。
    /// - 提供会话资源的统一访问入口。
    /// </summary>
    public sealed class SessionCoordinator : ISessionCoordinator
    {
        // ============== 状态 ==============

        private SessionConfig _config;
        private SessionRuntimePolicy _runtimePolicy;
        private ISessionCoordinatorHost _host;
        private SessionState _state = SessionState.Idle;

        // ============== 世界 ==============

        private IWorldHost _worldHost;
        private IWorld _world;
        private IWorldResolver _worldResolver;

        // ============== 同步 ==============

        private ISyncAdapter _syncAdapter;
        private ILogicWorldDriverBridge _driverHost;
        private Timeline.IViewTimeline _viewTimeline;

        // ============== 视图 ==============

        private IViewEventSink _viewEventSink;

        // ============== 子功能 ==============

        private readonly List<SubFeatures.ISessionSubFeature> _subFeatures = new List<SubFeatures.ISessionSubFeature>();

        // ============== 钩子 ==============

        private readonly SessionHooks _hooks = new SessionHooks();

        // ============== 属性 ==============

        public SessionId SessionId => _config.SessionId;
        public SessionConfig Config => _config;
        public SessionState State => _state;

        public IWorldHost WorldHost => _worldHost;
        public IWorld World => _world;
        public IWorldResolver WorldResolver => _worldResolver;

        public ISyncAdapter SyncAdapter => _syncAdapter;
        public Timeline.IViewTimeline ViewTimeline => _viewTimeline;

        public ILogicWorldDriverBridge? LogicWorldDriver => _driverHost;
        public IViewEventSink? ViewEventSink => _viewEventSink;

        public SessionHooks Hooks => _hooks;

        // ============== 构造 ==============

        public SessionCoordinator()
        {
            _config = SessionConfig.Default;
            _runtimePolicy = _config.ResolveRuntimePolicy();
            _state = SessionState.Idle;
        }

        // ============== 生命周期 ==============

        public void Initialize(SessionConfig config, ISessionCoordinatorHost host)
        {
            if (_state != SessionState.Idle)
            {
                throw new InvalidOperationException($"Cannot initialize session in state {_state}");
            }

            _state = SessionState.Initializing;
            _config = config;
            _host = host;
            if (_host is ISessionCoordinatorConfigPolicy policy)
            {
                policy.ConfigureSession(ref _config);
            }
            _runtimePolicy = _config.ResolveRuntimePolicy();

            try
            {
                // 创建 WorldHost。
                _worldHost = host.CreateWorldHost(_config);

                // 创建 World。
                var worldOptions = CreateWorldOptions(_config);
                _host.ConfigureWorldCreateOptions(in _config, worldOptions);
                _world = _worldHost.CreateWorld(worldOptions);
                _world.Initialize();
                _worldResolver = _world.Services;

                // 加载配置。
                host.LoadConfig(_world, _config);

                // 注册服务。
                host.RegisterServices(_world, _config);

                // 创建 ViewTimeline。
                _viewTimeline = new Timeline.ViewTimeline();

                // 创建 SyncAdapter。
                _syncAdapter = SyncAdapterFactory.Create(_world, _config);
                _syncAdapter.Attach(this);

                // 如果已设置驱动宿主，则挂接到同步适配器。
                if (_driverHost != null)
                {
                    _syncAdapter.SetLogicWorldDriver(_driverHost);
                }

                // 触发钩子。
                _hooks.InvokeSessionStarting(_config);

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

            if (_runtimePolicy.UseCoordinatorSpawnService)
            {
                var spawns = _host.CreatePlayerSpawnData(_config);
                CreatePlayerSpawns(spawns);
            }

            // 启动 SyncAdapter。
            _syncAdapter?.Attach(this, _driverHost);

            // 触发钩子。
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

            // 分离子功能。
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

            // 释放 SyncAdapter。
            _syncAdapter?.Dispose();
            _syncAdapter = null;

            // 释放 ViewTimeline。
            _viewTimeline?.Dispose();
            _viewTimeline = null;

            // 释放 World。
            if (_worldHost != null && _world != null)
            {
                _worldHost.DestroyWorld(_world.Id);
            }
            _world?.Dispose();
            _world = null;
            _worldHost = null;
            _worldResolver = null;

            // 清理钩子。
            _hooks.Clear();

            _state = SessionState.Idle;
        }

        // ============== 驱动与视图 ==============

        public void SetLogicWorldDriver(ILogicWorldDriverBridge driverHost)
        {
            _driverHost = driverHost;
            if (_syncAdapter != null)
            {
                _syncAdapter.SetLogicWorldDriver(driverHost);
            }
        }

        public void SetViewEventSink(IViewEventSink sink)
        {
            _viewEventSink = sink;
        }

        /// <summary>
        /// 通知视图事件接收器战斗开始。
        /// 由同步适配器或应用层直接调用。
        /// </summary>
        public void NotifyBattleStart(int frame)
        {
            _viewEventSink?.OnBattleStart(frame);
        }

        /// <summary>
        /// 通知视图事件接收器战斗结束。
        /// 由同步适配器或应用层直接调用。
        /// </summary>
        public void NotifyBattleEnd(int frame, int winTeamId)
        {
            _viewEventSink?.OnBattleEnd(frame, winTeamId);
        }

        /// <summary>
        /// 通知视图事件接收器帧同步完成。
        /// 由同步适配器在每帧结束后调用。
        /// </summary>
        public void NotifyFrameSyncComplete(int frame)
        {
            _viewEventSink?.OnFrameSyncComplete(frame);
        }

        /// <summary>
        /// 通知视图事件接收器收到进入游戏快照。
        /// 携带初始状态进入游戏时调用。
        /// </summary>
        public void NotifyEnterGameSnapshot(in FrameSnapshotData snapshot)
        {
            _viewEventSink?.OnEnterGameSnapshot(in snapshot);
        }

        /// <summary>
        /// 通知视图事件接收器收到 Actor 变换快照。
        /// Actor 位置变化时调用。
        /// </summary>
        public void NotifyActorTransformSnapshot(in FrameSnapshotData snapshot)
        {
            _viewEventSink?.OnActorTransformSnapshot(in snapshot);
        }

        /// <summary>
        /// 通知视图事件接收器收到伤害事件。
        /// 发生伤害时调用。
        /// </summary>
        public void NotifyDamageSnapshot(in FrameSnapshotData snapshot)
        {
            _viewEventSink?.OnDamageEventSnapshot(in snapshot);
        }

        /// <summary>
        /// 通知视图事件接收器收到自定义事件。
        /// 用于游戏专属事件。
        /// </summary>
        public void NotifyCustomEvent(string eventType, int entityId, byte[] customData)
        {
            _viewEventSink?.OnCustomEvent(eventType, entityId, customData);
        }

        // ============== 输入 ==============

        public void SubmitLocalInput(PlayerInput input)
        {
            _syncAdapter?.SubmitInput(input);
        }

        // ============== 服务解析 ==============

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

        // ============== 帧更新 ==============

        public void Tick(float deltaTime)
        {
            if (_state != SessionState.Running)
            {
                return;
            }

            _hooks.InvokePreTick(deltaTime);

            // 子功能 PreTick。
            foreach (var sf in _subFeatures)
            {
                if (sf is SubFeatures.ISessionPreTickSubFeature preTick)
                {
                    preTick.OnPreTick(deltaTime);
                }
            }

            // SyncAdapter 帧更新。
            _syncAdapter?.Tick(deltaTime);

            // World 帧更新。
            if (CanDriveLogicWorld(deltaTime))
            {
                _worldHost?.Tick(deltaTime);
            }

            // 子功能 PostTick。
            foreach (var sf in _subFeatures)
            {
                if (sf is SubFeatures.ISessionPostTickSubFeature postTick)
                {
                    postTick.OnPostTick(deltaTime);
                }
            }

            _hooks.InvokePostTick(deltaTime);
        }

        // ============== 子功能管理 ==============

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

        // ============== 私有方法 ==============

        private bool CanDriveLogicWorld(float deltaTime)
        {
            if (_worldResolver != null && _worldResolver.TryResolve<ILogicWorldDriveGate>(out var gate) && gate != null)
            {
                return gate.CanDriveLogicWorld(deltaTime);
            }

            return !_runtimePolicy.RequireLogicWorldDriveGate;
        }

        private WorldCreateOptions CreateWorldOptions(SessionConfig config)
        {
            string worldType = string.IsNullOrEmpty(config.WorldType) ? "battle" : config.WorldType;
            return new WorldCreateOptions
            {
                Id = new WorldId(config.WorldId > 0 ? config.WorldId.ToString() : "1"),
                WorldType = worldType
            };
        }

        private void CreatePlayerSpawns(PlayerSpawnData[] spawns)
        {
            if (spawns == null || spawns.Length == 0)
            {
                Log.Warning("[SessionCoordinator] No player spawns to create");
                return;
            }

            // 尝试通过 ISpawnService 创建玩家生成点
            if (_worldResolver != null && _worldResolver.TryResolve<ISpawnService>(out var spawnService))
            {
                if (spawnService.CreateSpawns(spawns))
                {
                    Log.Info($"[SessionCoordinator] Created {spawns.Length} player spawns via ISpawnService");
                    return;
                }
            }

            // 如果没有 ISpawnService，记录警告但继续启动
            Log.Warning($"[SessionCoordinator] ISpawnService not found, {spawns.Length} spawns not created");
        }

        // ============== 资源释放 ==============

        public void Dispose()
        {
            Destroy();
        }
    }
}
