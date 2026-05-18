using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Share.Impl.Moba.Struct;
using AbilityKit.Core.Math;
using AbilityKit.Demo.Moba.Console.Bootstrap;
using AbilityKit.Demo.Moba.Console.Core.Battle.Context;
using AbilityKit.Demo.Moba.Console.Core.Input;
using AbilityKit.Demo.Moba.Console.Battle.Snapshot;
using AbilityKit.Demo.Moba.Console.Events;
using AbilityKit.Demo.Moba.Share;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Demo.Moba.Console.Simulation
{
    /// <summary>
    /// Console 战斗会话接口（模拟逻辑层）
    ///
    /// 【模拟层】此接口用于演示目的，封装简化的逻辑层运行时
    /// 生产环境应使用真正的 AbilityKit.Ability.Runtime
    /// </summary>
    public interface ISimulatedBattleSession : IDisposable
    {
        /// <summary>
        /// 提交输入命令到逻辑层
        /// </summary>
        void SubmitInput(FrameIndex frame, IReadOnlyList<PlayerInputCommand> inputs);

        /// <summary>
        /// 获取本地玩家 ActorId
        /// </summary>
        int LocalActorId { get; }

        /// <summary>
        /// 帧推进（更新冷却等）
        /// </summary>
        void Step(float deltaTime);

        /// <summary>
        /// 初始化模拟逻辑层（注册实体、设置初始状态）
        /// </summary>
        void Initialize();

        /// <summary>
        /// 获取所有角色的当前状态（用于视图渲染）
        /// </summary>
        IEnumerable<ActorState> GetAllActorStates();
    }

    /// <summary>
    /// Console 战斗会话实现（模拟逻辑层）
    ///
    /// 【逻辑层】此实现使用配置驱动的技能执行器
    ///
    /// 职责边界（逻辑层）：
    /// - ✅ 处理输入命令
    /// - ✅ 技能执行（配置驱动）
    /// - ✅ 伤害计算（配置驱动）
    /// - ✅ 通过 ConsoleFrameSnapshotDispatcher 分发快照事件
    /// - ✅ 持有角色数据（ConsoleActorRepository）
    /// - ❌ 不做渲染
    /// - ❌ 不持有 UI 引用
    /// </summary>
    public sealed class SimulatedBattleSession : ISimulatedBattleSession
    {
        private readonly EC.IECWorld _world;
        private readonly int _localActorId;
        private IConsoleSkillExecutor? _skillExecutor; // 非 readonly，允许延迟初始化
        private ConsoleFrameSnapshotDispatcher? _snapshotDispatcher;
        private bool _disposed;
        private bool _initialized;

        /// <summary>
        /// 角色数据存储（权威数据源）
        /// 由 SimulatedBattleSession 持有和管理
        /// </summary>
        public readonly ConsoleActorRepository ActorRepository;

        public EC.IECWorld World => _world;
        public int LocalActorId => _localActorId;

        public SimulatedBattleSession(EC.IECWorld world, int localActorId, IConsoleSkillExecutor? skillExecutor)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _localActorId = localActorId;
            _skillExecutor = skillExecutor; // 允许为空，后续通过 SetSkillExecutor 设置

            // 创建角色数据仓库
            ActorRepository = new ConsoleActorRepository();
        }

        /// <summary>
        /// 设置技能执行器（用于延迟初始化）
        /// </summary>
        public void SetSkillExecutor(IConsoleSkillExecutor executor)
        {
            _skillExecutor = executor ?? throw new ArgumentNullException(nameof(executor));
            Platform.Log.Skill("[Session] SkillExecutor set");
        }

        /// <summary>
        /// 设置快照分发器（在 Bootstrapper 中连接）
        /// </summary>
        public void SetSnapshotDispatcher(ConsoleFrameSnapshotDispatcher dispatcher)
        {
            _snapshotDispatcher = dispatcher;
        }

        /// <summary>
        /// 初始化逻辑层
        /// </summary>
        public void Initialize()
        {
            if (_initialized) return;
            if (_skillExecutor is IConsoleSkillExecutor executor)
            {
                executor.Initialize();
            }
            _initialized = true;
            Platform.Log.System("[Session] Initialized");
        }

        public void SubmitInput(FrameIndex frame, IReadOnlyList<PlayerInputCommand> inputs)
        {
            if (inputs == null || inputs.Count == 0) return;

            for (int i = 0; i < inputs.Count; i++)
            {
                HandleCommand(inputs[i]);
            }
        }

        private void HandleCommand(PlayerInputCommand cmd)
        {
            switch (cmd.OpCode)
            {
                case ConsoleOpCode.Move:
                    HandleMove(cmd);
                    break;
                case ConsoleOpCode.SkillInput:
                    HandleSkillInput(cmd);
                    break;
            }
        }

        private void HandleMove(PlayerInputCommand cmd)
        {
            if (cmd.Payload == null || cmd.Payload.Length == 0) return;

            DeserializeMove(cmd.Payload, out var dx, out var dz);
            var actorId = ParsePlayerId(cmd.Player);

            Platform.Log.Input($"[SimSession] Move: Actor#{actorId} dx={dx:F2} dz={dz:F2}");
        }

        private void HandleSkillInput(PlayerInputCommand cmd)
        {
            if (cmd.Payload == null || cmd.Payload.Length == 0) return;

            var actorId = ParsePlayerId(cmd.Player);
            var (slot, phase, aimPos) = DeserializeSkillInput(cmd.Payload);

            // 释放技能，获取执行结果
            var castResult = _skillExecutor.CastBySlot(actorId, slot, aimPos, Vec3.Forward);

            if (!castResult.Success)
            {
                Platform.Log.Skill($"[Session] Skill failed: Actor#{actorId} Slot{slot} - {castResult.FailReason}");
                return;
            }

            Platform.Log.Skill($"[Session] Skill cast: Actor#{actorId} Slot{slot} Skill#{castResult.SkillId} " +
                              $"Target#{castResult.TargetId}");

            // 发布技能释放事件（由逻辑层处理伤害计算）
            BattleEventBus.Publish(new SkillCastEvent
            {
                CasterId = castResult.CasterId,
                SkillId = castResult.SkillId,
                TargetId = castResult.TargetId,
                Slot = slot
            });
        }

        public void Step(float deltaTime)
        {
            _skillExecutor.Step(deltaTime);
        }

        /// <inheritdoc />
        public IEnumerable<ActorState> GetAllActorStates()
        {
            return ActorRepository.GetAllActors();
        }

        private static int ParsePlayerId(PlayerId player)
        {
            if (int.TryParse(player.Value, out var id))
            {
                return id;
            }
            return 0;
        }

        private static void DeserializeMove(byte[] payload, out float dx, out float dz)
        {
            dx = 0f;
            dz = 0f;

            try
            {
                var json = System.Text.Encoding.UTF8.GetString(payload);
                ParseJsonPair(json, "dx", ref dx);
                ParseJsonPair(json, "dz", ref dz);
            }
            catch (Exception ex)
            {
                Platform.Log.Warn($"[SimSession] Failed to deserialize move: {ex.Message}");
            }
        }

        private static (int slot, SkillInputPhase phase, Vec3 aimPos) DeserializeSkillInput(byte[] payload)
        {
            int slot = 0;
            var phase = SkillInputPhase.Press;
            var aimPos = Vec3.Zero;

            try
            {
                var json = System.Text.Encoding.UTF8.GetString(payload);

                ParseJsonPair(json, "slot", ref slot);
                ParseJsonPair(json, "phase", ref phase);

                float aimX = 0, aimZ = 0;
                ParseJsonPair(json, "aimX", ref aimX);
                ParseJsonPair(json, "aimZ", ref aimZ);
                aimPos = new Vec3(aimX, 0, aimZ);
            }
            catch (Exception ex)
            {
                Platform.Log.Warn($"[SimSession] Failed to deserialize skill input: {ex.Message}");
            }

            return (slot, phase, aimPos);
        }

        private static void ParseJsonPair(string json, string key, ref int value)
        {
            var search = $"\"{key}\":";
            var idx = json.IndexOf(search, StringComparison.Ordinal);
            if (idx < 0) return;

            var start = idx + search.Length;
            var end = start;
            while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '-')) end++;

            if (end > start && int.TryParse(json.Substring(start, end - start), out var result))
            {
                value = result;
            }
        }

        private static void ParseJsonPair(string json, string key, ref float value)
        {
            var search = $"\"{key}\":";
            var idx = json.IndexOf(search, StringComparison.Ordinal);
            if (idx < 0) return;

            var start = idx + search.Length;
            var end = start;
            while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '.' || json[end] == '-')) end++;

            if (end > start && float.TryParse(json.Substring(start, end - start), out var result))
            {
                value = result;
            }
        }

        private static void ParseJsonPair(string json, string key, ref SkillInputPhase phase)
        {
            int value = 0;
            ParseJsonPair(json, key, ref value);
            phase = (SkillInputPhase)value;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_skillExecutor is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
