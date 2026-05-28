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
using AbilityKit.Demo.Moba.Services;

namespace AbilityKit.Demo.Moba.Session
{
    /// <summary>
    /// MOBA 战斗驱动 Host（跨平台复用）
    /// 将 Coordinator 输入提交到 moba.core 逻辑世界
    ///
    /// 设计原则（单一入口）：
    /// - 只通过战斗逻辑层端口与 moba.core 交互
    /// - 输入：通过 IMobaBattleInputPort 提交
    /// - 输出：通过 IMobaBattleOutputPort 获取快照
    /// - 禁止直接访问 MobaEntityManager 或实体
    /// </summary>
    public sealed class MobaBattleDriverHost : IBattleDriverHost
    {
        private IWorld _world;
        private HostRuntime _hostRuntime;
        private ISessionCoordinator _coordinator;
        private IMobaBattleInputPort _input;
        private MobaPlayerInputCommandConverter _inputConverter;
        private MobaTransformSnapshotDispatcher _transformSnapshots;
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

            // 获取战斗逻辑层 IO 端口，外部模块不直接依赖内部输入/快照服务。
            if (_world?.Services != null)
            {
                _world.Services.TryResolve(out _input);
            }

            _inputConverter = new MobaPlayerInputCommandConverter();
            _transformSnapshots = new MobaTransformSnapshotDispatcher(_world);

            // 订阅快照事件（通过 HostRuntime 的事件机制）
            SubscribeToSnapshots();
        }

        /// <summary>
        /// 订阅快照事件
        ///
        /// 快照获取在 AdvanceFrame() 中进行，通过 TryGetTransformSnapshot() 方法
        /// 在每个逻辑帧结束后（HostRuntime.Tick 之后）获取最新的快照数据
        ///
        /// 这种设计确保：
        /// 1. ECS 系统已经执行完成，快照数据是最新的
        /// 2. 快照获取与帧推进同步，不会出现竞态条件
        /// 3. 不需要依赖 HostRuntimeOptions 的修改
        /// </summary>
        private void SubscribeToSnapshots()
        {
            Log.Info("[MobaBattleDriverHost] Snapshot subscription initialized (acquired per frame in AdvanceFrame)");
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
            if (!_isRunning || inputs == null || inputs.Length == 0 || _input == null)
            {
                return;
            }

            Log.Info($"[MobaBattleDriverHost] SubmitInputs: {inputs.Length} inputs");

            var commands = _inputConverter.Convert(inputs);

            // 通过战斗逻辑层输入端口提交，隔离具体输入处理服务。
            _input.Submit(new FrameIndex(_currentFrame), commands);
        }

        public EntityState[] GetAllEntityStates()
        {
            // 通过服务接口获取实体状态
            // 这是一个同步查询接口，用于获取当前帧的实体状态
            // 主要用于初始快照或调试目的

            if (_world?.Services == null)
            {
                return Array.Empty<EntityState>();
            }

            // 通过 MobaActorRegistry 获取所有 Actor 实体状态
            if (!_world.Services.TryResolve<MobaActorRegistry>(out var registry))
            {
                return Array.Empty<EntityState>();
            }

            var entries = registry.Entries;
            var states = new List<EntityState>(8);

            foreach (var kv in entries)
            {
                var actorId = kv.Key;
                var entity = kv.Value;
                if (entity == null) continue;

                var state = new EntityState(actorId);

                // 获取实体的 Transform 信息（核心位置数据）
                if (entity.hasTransform)
                {
                    var pos = entity.transform.Value.Position;
                    state.X = pos.X;
                    state.Y = pos.Y;
                    state.Z = pos.Z;
                }

                // Team 信息
                if (entity.hasTeam)
                {
                    state.TeamId = (int)entity.team.Value;
                }

                states.Add(state);
            }

            return states.ToArray();
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

            // 从战斗逻辑层输出端口获取位置快照。
            TryGetTransformSnapshot();
        }

        /// <summary>
        /// 从战斗逻辑层输出端口获取位置快照。
        /// </summary>
        private void TryGetTransformSnapshot()
        {
            _transformSnapshots?.TryDispatch(_currentFrame, _onTransformSnapshot);
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
            // 移动输入通过 SubmitInputs -> IMobaBattleInputPort.Submit() 处理
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
