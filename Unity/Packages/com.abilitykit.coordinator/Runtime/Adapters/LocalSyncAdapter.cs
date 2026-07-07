using System;
using System.Collections.Generic;
using System.Diagnostics;
using AbilityKit.Ability.World.Abstractions;

namespace AbilityKit.Coordinator
{
    /// <summary>
    /// 本地同步适配器（锁步模式）。
    ///
    /// 设计：
    /// - 所有客户端在本地运行相同模拟。
    /// - 输入共享且保持确定性。
    /// - 不需要服务端权威。
    ///
    /// 适用场景：
    /// - 单人玩法。
    /// - 局域网多人玩法。
    /// - 离线模式。
    /// </summary>
    public sealed class LocalSyncAdapter : ILocalSyncAdapter
    {
        private readonly IWorld _world;
        private readonly SessionConfig _config;
        private readonly SessionRuntimePolicy _runtimePolicy;
        private ISessionCoordinator _coordinator;
        private ILogicWorldDriverBridge _driverHost;

        private double _lastTickTime;
        private double _renderTime;
        private int _currentFrame;
        private double _logicTime;
        private readonly List<PlayerInput> _pendingInputs = new();

        // ============== ISyncAdapter 实现 ==============

        public Core.SyncMode Mode => Core.SyncMode.Lockstep;

        public bool IsConnected => true; // 本地模式始终已连接。

        public int CurrentFrame => _driverHost?.CurrentFrame ?? _currentFrame;

        public double LogicTimeSeconds => _driverHost?.LogicTimeSeconds ?? _logicTime;

        public double RenderTimeSeconds => _renderTime;

        public int LocalPlayerId => _config.LocalPlayerId;

        public event Action<int, double> OnFrameSync;

        public LocalSyncAdapter(IWorld world, in SessionConfig config)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _config = config;
            _runtimePolicy = config.ResolveRuntimePolicy();
            _lastTickTime = GetTimeSeconds();
            _renderTime = 0;
            _currentFrame = 0;
            _logicTime = 0;
        }

        public void Attach(ISessionCoordinator coordinator)
        {
            _coordinator = coordinator;
        }

        public void Attach(ISessionCoordinator coordinator, ILogicWorldDriverBridge driverHost)
        {
            _coordinator = coordinator;
            _driverHost = driverHost;
        }

        public void SetLogicWorldDriver(ILogicWorldDriverBridge driverHost)
        {
            _driverHost = driverHost;
        }

        public void Tick(float deltaTime)
        {
            // 更新渲染时间。
            _renderTime += deltaTime;

            // 检查是否到达下一逻辑帧时间。
            double currentTime = GetTimeSeconds();
            double frameInterval = 1.0 / _config.TickRate;

            if (currentTime - _lastTickTime >= frameInterval)
            {
                ProcessLogicFrame(deltaTime);
                _lastTickTime = currentTime;
            }
        }

        public void SubmitInput(PlayerInput input)
        {
            lock (_pendingInputs)
            {
                _pendingInputs.Add(input);
            }
        }

        public SnapshotEntityState[] GetAllEntityStates()
        {
            if (_driverHost != null)
            {
                return _driverHost.GetAllEntityStates();
            }
            return Array.Empty<SnapshotEntityState>();
        }

        public void Dispose()
        {
            _coordinator = null;
            _driverHost = null;
            _pendingInputs.Clear();
            OnFrameSync = null;
        }

        // ============== 私有方法 ==============

        private void ProcessLogicFrame(float deltaTime)
        {
            // 刷新待提交输入。
            PlayerInput[] inputsToSubmit;
            lock (_pendingInputs)
            {
                inputsToSubmit = _pendingInputs.ToArray();
                _pendingInputs.Clear();
            }

            if (!CanDriveLogicWorld(deltaTime))
            {
                return;
            }

            // 通过驱动宿主提交输入。
            if (_driverHost != null && inputsToSubmit.Length > 0)
            {
                _driverHost.SubmitInputs(inputsToSubmit);
            }

            if (_driverHost != null)
            {
                if (!_driverHost.IsRunning)
                {
                    _driverHost.Start();
                }

                _driverHost.AdvanceFrame(deltaTime);
            }
            else
            {
                _currentFrame++;
                _logicTime += deltaTime;
            }

            // 触发帧同步事件。
            OnFrameSync?.Invoke(CurrentFrame, LogicTimeSeconds);
        }

        private bool CanDriveLogicWorld(float deltaTime)
        {
            if (_world?.Services != null && _world.Services.TryResolve<ILogicWorldDriveGate>(out var gate) && gate != null)
            {
                return gate.CanDriveLogicWorld(deltaTime);
            }

            return !_runtimePolicy.RequireLogicWorldDriveGate;
        }

        private static double GetTimeSeconds()
        {
            return Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
        }
    }
}
