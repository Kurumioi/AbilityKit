using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Coordinator;
using AbilityKit.Core.Common.Log;
using AbilityKit.Protocol.Moba.StateSync;
using MobaOpCode = AbilityKit.Demo.Moba.Services.MobaOpCode;
using MobaSnapshotRouter = AbilityKit.Demo.Moba.Services.MobaSnapshotRouter;

namespace AbilityKit.Demo.Moba.Session
{
    /// <summary>
    /// MOBA 战斗驱动 Host（跨平台复用）
    /// 将 Coordinator 输入提交到 moba.core 逻辑世界
    ///
    /// 设计原则（单一入口）：
    /// - 只通过服务接口与 moba.core 交互
    /// - 输入：通过 IWorldInputSink.Submit() 提交
    /// - 位置查询：通过 MobaSnapshotRouter 快照事件获取
    /// - 禁止直接访问 MobaEntityManager 或实体
    /// </summary>
    public sealed class MobaBattleDriverHost : IBattleDriverHost
    {
        private IWorld _world;
        private HostRuntime _hostRuntime;
        private ISessionCoordinator _coordinator;
        private IWorldInputSink _inputSink;
        private MobaSnapshotRouter _snapshotRouter;
        private int _currentFrame;
        private double _logicTimeSeconds;
        private bool _isRunning;

        // 位置快照事件回调（由 ET Logic 层设置）
        private System.Action<int, MobaActorTransformSnapshotEntry[]>? _onTransformSnapshot;

        public void Bind(IWorld world, HostRuntime hostRuntime, ISessionCoordinator coordinator)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _hostRuntime = hostRuntime;
            _coordinator = coordinator;

            // 获取服务引用
            if (_world?.Services != null)
            {
                _world.Services.TryResolve(out _inputSink);
                _world.Services.TryResolve(out _snapshotRouter);
            }

            // 订阅快照事件（通过 HostRuntime 的事件机制）
            SubscribeToSnapshots();
        }

        /// <summary>
        /// 订阅快照事件
        /// 在 HostRuntime 的帧循环中，通过 IWorldStateSnapshotProvider 获取快照
        /// </summary>
        private void SubscribeToSnapshots()
        {
            // TODO: 实现快照订阅机制
            // 方案：1. 通过 HostRuntimeOptions 的 PreTick/PostTick 钩子
            //       2. 或通过事件系统订阅快照
        }

        /// <summary>
        /// 设置位置快照回调（由 ET Logic 层调用）
        /// </summary>
        public void SetTransformSnapshotCallback(System.Action<int, MobaActorTransformSnapshotEntry[]>? callback)
        {
            _onTransformSnapshot = callback;
        }

        public void SetEntityStates(IEnumerable<EntityState> states)
        {
            // 此方法保留用于初始快照
            // 实际位置同步通过快照事件机制
        }

        public int CurrentFrame => _currentFrame;

        public double LogicTimeSeconds => _logicTimeSeconds;

        public bool IsRunning => _isRunning;

        public void Start()
        {
            _isRunning = true;
            _currentFrame = 0;
            _logicTimeSeconds = 0;
            Log.Info("[MobaBattleDriverHost] Started");
        }

        public void Stop()
        {
            _isRunning = false;
            Log.Info("[MobaBattleDriverHost] Stopped");
        }

        public void SubmitInputs(PlayerInput[] inputs)
        {
            if (!_isRunning || inputs == null || inputs.Length == 0 || _inputSink == null)
            {
                return;
            }

            Log.Info($"[MobaBattleDriverHost] SubmitInputs: {inputs.Length} inputs");

            // 将 Coordinator.PlayerInput 转换为 PlayerInputCommand
            var commands = new List<PlayerInputCommand>(inputs.Length);
            foreach (var input in inputs)
            {
                var playerId = new PlayerId(input.PlayerId.ToString());
                commands.Add(new PlayerInputCommand(
                    new FrameIndex(input.Frame),
                    playerId,
                    input.OpCode,
                    input.Payload));
            }

            // 通过 IWorldInputSink 提交输入（唯一入口）
            _inputSink.Submit(new FrameIndex(_currentFrame), commands);
        }

        public EntityState[] GetAllEntityStates()
        {
            // 位置查询通过快照事件机制，不再直接查询
            // 如果需要初始快照，返回之前设置的状态
            return Array.Empty<EntityState>();
        }

        public void AdvanceFrame(float deltaTime)
        {
            if (!_isRunning)
            {
                return;
            }

            _currentFrame++;
            _logicTimeSeconds += deltaTime;

            // 驱动 moba.core Tick
            _hostRuntime?.Tick(deltaTime);

            // 通过 Coordinator 协调
            if (_coordinator is SessionCoordinator sessionCoordinator)
            {
                sessionCoordinator.Tick(deltaTime);
            }

            // 从快照路由获取位置快照（通过服务接口）
            TryGetTransformSnapshot();
        }

        /// <summary>
        /// 从 MobaSnapshotRouter 获取位置快照（服务接口）
        /// </summary>
        private void TryGetTransformSnapshot()
        {
            if (_snapshotRouter == null)
            {
                return;
            }

            // 通过 IWorldStateSnapshotProvider 接口获取快照
            if (_world?.Services?.TryResolve<IWorldStateSnapshotProvider>(out var provider) != true)
            {
                return;
            }

            var frameIndex = new FrameIndex(_currentFrame);
            if (provider.TryGetSnapshot(frameIndex, out var snapshot))
            {
                // 解析快照类型
                if (snapshot.OpCode == (int)MobaOpCode.ActorTransformSnapshot)
                {
                    var entries = MobaActorTransformSnapshotCodec.Deserialize(snapshot.Payload);
                    _onTransformSnapshot?.Invoke(_currentFrame, entries);
                    Log.Info($"[MobaBattleDriverHost] Transform snapshot: {entries?.Length ?? 0} entities");
                }
            }
        }

        private void HandleSkillInput(PlayerInput input)
        {
            if (!input.TryGetSkillTarget(out int slot, out float x, out float z))
            {
                return;
            }

            Log.Info($"[MobaBattleDriverHost] Skill input: player={input.PlayerId} slot={slot} target=({x},{z})");
            // Skill execution is handled by moba.core logic layer during HostRuntime.Tick
        }

        private void HandleMoveInput(PlayerInput input)
        {
            // 移动输入通过 SubmitInputs -> IWorldInputSink.Submit() 处理
            // 不在这里直接操作实体
            if (!input.TryGetMoveTarget(out float x, out float z))
            {
                return;
            }

            Log.Info($"[MobaBattleDriverHost] Move input queued: player={input.PlayerId} target=({x},{z})");
        }

        private void HandleStopInput(PlayerInput input)
        {
            Log.Info($"[MobaBattleDriverHost] Stop input: player={input.PlayerId}");
        }
    }
}
