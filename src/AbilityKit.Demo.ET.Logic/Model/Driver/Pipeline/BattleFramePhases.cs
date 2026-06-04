using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Ability.World.Services;
using AbilityKit.Demo.Moba.Attributes;
using AbilityKit.Demo.Moba.Share;
using AbilityKit.Pipeline;
using AbilityKit.Protocol.Moba.StateSync;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Config.Core;

namespace ET.Logic
{
    /// <summary>
    /// 阶段 ID 常量
    /// </summary>
    public static class BattleFramePhaseIds
    {
        public const string PreTick = "PreTick";
        public const string ProcessETInput = "ProcessETInput";
        public const string DriveWorld = "DriveWorld";
        public const string CollectSnapshot = "CollectSnapshot";
        public const string DispatchSnapshot = "DispatchSnapshot";
        public const string PostTick = "PostTick";
    }

    /// <summary>
    /// 阶段 1: 预处理
    /// 更新帧号和时间
    /// </summary>
    public sealed class PreTickPhase : AbilityInstantPhaseBase<BattleFrameContext>
    {
        public PreTickPhase() : base(BattleFramePhaseIds.PreTick) { }

        protected override void OnInstantExecute(BattleFrameContext ctx)
        {
            ctx.CurrentFrame++;
            ctx.LogicTimeSeconds += ctx.DeltaTime;
            ctx.FrameSnapshots.Clear();
            ctx.SnapshotDispatched = false;
        }
    }

    /// <summary>
    /// 阶段 2: 处理 ET 输入
    /// 从 ETInputComponent 读取命令并提交到战斗输入端口
    ///
    /// 设计说明：
    /// - ET.Logic 层的输入先存入 ETInputComponent 缓冲
    /// - AutoTest/SkillTest 在此阶段之前生成命令到 ETInputComponent
    /// - 这里读取缓冲并转换为 PlayerInputCommand 提交到 moba.core
    /// </summary>
    public sealed class ProcessETInputPhase : AbilityInstantPhaseBase<BattleFrameContext>
    {
        public ProcessETInputPhase() : base(BattleFramePhaseIds.ProcessETInput) { }

        protected override void OnInstantExecute(BattleFrameContext ctx)
        {
            var inputComponent = ctx.GetInputComponent();
            if (inputComponent == null)
            {
                return;
            }

            // 获取帧的输入
            var commands = inputComponent.GetInputsForFrame(ctx.CurrentFrame);
            if (commands == null || commands.Count == 0)
            {
                return;
            }

            // 获取战斗逻辑层输入端口
            if (!ctx.Driver.TryResolve(out IMobaBattleInputPort inputPort) || inputPort == null)
            {
                Log.Warning("[ProcessETInputPhase] IMobaBattleInputPort not resolved");
                return;
            }

            var frameIndex = new FrameIndex(ctx.CurrentFrame);
            var playerCommands = new List<PlayerInputCommand>(commands.Count);
            foreach (var command in commands)
            {
                if (ETInputCommandConverterRegistry.TryConvert(command, frameIndex, out var playerCommand))
                {
                    playerCommands.Add(playerCommand);
                }
            }

            // 提交到战斗逻辑层输入端口
            if (playerCommands.Count > 0)
            {
                PlayerInputCommand first = playerCommands[0];
                Log.Info($"[ProcessETInputPhase] Submit: Frame={ctx.CurrentFrame}, Count={playerCommands.Count}, FirstPlayer={first.Player.Value}, FirstOp={first.OpCode}");
                inputPort.Submit(new FrameIndex(ctx.CurrentFrame), playerCommands);
                LogRuntimeInputState(ctx, first);
            }
        }

        private static void LogRuntimeInputState(BattleFrameContext ctx, PlayerInputCommand first)
        {
            ctx.Driver.TryResolve(out MobaGamePhaseService phase);
            ctx.Driver.TryResolve(out MobaPlayerActorMapService playerActorMap);
            ctx.Driver.TryResolve(out MobaActorRegistry registry);
            ctx.Driver.TryResolve(out MobaConfigDatabase config);

            var inGame = phase != null && phase.InGame;
            var actorId = 0;
            var hasActor = playerActorMap != null && playerActorMap.TryGetActorId(first.Player, out actorId);
            var hasEntity = false;
            var dx = 0f;
            var dz = 0f;
            var speed = 0f;
            var hasConfig = config != null;

            if (hasActor && registry != null && registry.TryGet(actorId, out var entity) && entity != null)
            {
                hasEntity = true;
                if (entity.hasMoveInput)
                {
                    dx = entity.moveInput.Dx;
                    dz = entity.moveInput.Dz;
                }

                if (entity.hasAttributeGroup)
                {
                    speed = new MobaAttrs(entity).MoveSpeed;
                }
            }

            Log.Info($"[ProcessETInputPhase] Runtime input state: Frame={ctx.CurrentFrame}, InGame={inGame}, HasPlayerMap={playerActorMap != null}, Player={first.Player.Value}, HasActor={hasActor}, ActorId={(hasActor ? actorId : 0)}, HasEntity={hasEntity}, Move=({dx:F3},{dz:F3}), Speed={speed:F3}, HasConfig={hasConfig}");
        }
    }

    /// <summary>
    /// 阶段 3: 驱动世界
    /// 驱动 ECS 世界执行所有系统
    /// </summary>
    public sealed class DriveWorldPhase : AbilityInstantPhaseBase<BattleFrameContext>
    {
        private int _sampleLogCount;

        public DriveWorldPhase() : base(BattleFramePhaseIds.DriveWorld) { }

        protected override void OnInstantExecute(BattleFrameContext ctx)
        {
            if (ctx.Driver == null || ctx.Driver.World == null)
            {
                return;
            }

            // 驱动 World Tick
            ctx.Driver.World.Tick(ctx.DeltaTime);

            _sampleLogCount++;
            if (_sampleLogCount <= 5 || _sampleLogCount % 60 == 0)
            {
                LogRuntimeActorState(ctx);
            }
        }

        private static void LogRuntimeActorState(BattleFrameContext ctx)
        {
            if (!ctx.Driver.TryResolve(out MobaActorRegistry registry) || registry == null)
            {
                Log.Info($"[DriveWorldPhase] Runtime actor registry not resolved: Frame={ctx.CurrentFrame}");
                return;
            }

            ctx.Driver.TryResolve(out IWorldClock clock);

            foreach (var kv in registry.Entries)
            {
                var actorId = kv.Key;
                var e = kv.Value;
                if (e == null) continue;

                var x = 0f;
                var y = 0f;
                var z = 0f;
                if (e.hasTransform)
                {
                    var p = e.transform.Value.Position;
                    x = p.X;
                    y = p.Y;
                    z = p.Z;
                }

                var dx = e.hasMoveInput ? e.moveInput.Dx : 0f;
                var dz = e.hasMoveInput ? e.moveInput.Dz : 0f;
                var speed = 0f;
                if (e.hasAttributeGroup)
                {
                    speed = new MobaAttrs(e).MoveSpeed;
                }

                Log.Info($"[DriveWorldPhase] Runtime actor: Frame={ctx.CurrentFrame}, ActorId={actorId}, Pos=({x:F3},{y:F3},{z:F3}), HasMoveInput={e.hasMoveInput}, Move=({dx:F3},{dz:F3}), HasMotion={e.hasMotion}, MotionInit={(e.hasMotion && e.motion.Initialized)}, Speed={speed:F3}, ClockDt={(clock != null ? clock.DeltaTime : -1f):F4}");
                break;
            }
        }

        public override bool ShouldExecute(BattleFrameContext ctx)
        {
            return ctx.IsRunning && ctx.DeltaTime > 0f;
        }
    }

    /// <summary>
    /// 阶段 4: 收集快照
    /// 从 Runtime 输出端口收集状态快照
    /// </summary>
    public sealed class CollectSnapshotPhase : AbilityInstantPhaseBase<BattleFrameContext>
    {
        private readonly List<WorldStateSnapshot> _runtimeSnapshots = new List<WorldStateSnapshot>(32);

        public CollectSnapshotPhase() : base(BattleFramePhaseIds.CollectSnapshot) { }

        protected override void OnInstantExecute(BattleFrameContext ctx)
        {
            if (ctx.Driver == null)
            {
                return;
            }

            if (!ctx.Driver.TryResolve(out IMobaBattleOutputPort outputPort) || outputPort == null)
            {
                Log.Warning("[CollectSnapshotPhase] IMobaBattleOutputPort not resolved");
                return;
            }

            _runtimeSnapshots.Clear();
            outputPort.CollectSnapshots(new FrameIndex(ctx.CurrentFrame), _runtimeSnapshots);

            for (int i = 0; i < _runtimeSnapshots.Count; i++)
            {
                var runtimeSnapshot = _runtimeSnapshots[i];
                Log.Info($"[CollectSnapshotPhase] Runtime snapshot: Frame={ctx.CurrentFrame}, OpCode={runtimeSnapshot.OpCode}");
                if (ETBattleWorldSnapshotAdapter.TryConvert(
                    in runtimeSnapshot,
                    ctx.CurrentFrame,
                    ctx.LogicTimeSeconds,
                    out var frameSnapshot))
                {
                    ctx.FrameSnapshots.Add(frameSnapshot);
                }
            }
        }
    }

    /// <summary>
    /// 阶段 5: 分发快照
    /// 将快照分发给视图层
    /// </summary>
    public sealed class DispatchSnapshotPhase : AbilityInstantPhaseBase<BattleFrameContext>
    {
        public DispatchSnapshotPhase() : base(BattleFramePhaseIds.DispatchSnapshot) { }

        protected override void OnInstantExecute(BattleFrameContext ctx)
        {
            if (ctx.SnapshotDispatched)
            {
                return;
            }

            if (ctx.FrameSnapshots.Count == 0)
            {
                return;
            }

            for (int i = 0; i < ctx.FrameSnapshots.Count; i++)
            {
                var snapshot = ctx.FrameSnapshots[i];
                ctx.Driver?.HandleSnapshot(in snapshot);
            }

            ctx.SnapshotDispatched = true;
        }

        public override bool ShouldExecute(BattleFrameContext ctx)
        {
            return ctx.FrameSnapshots.Count > 0 && !ctx.SnapshotDispatched;
        }
    }

    /// <summary>
    /// 阶段 6: 后处理
    /// 清理和日志记录
    /// </summary>
    public sealed class PostTickPhase : AbilityInstantPhaseBase<BattleFrameContext>
    {
        public PostTickPhase() : base(BattleFramePhaseIds.PostTick) { }

        protected override void OnInstantExecute(BattleFrameContext ctx)
        {
            // 可以在此添加调试日志或性能统计
            // 当前阶段主要用于扩展点，不执行实际逻辑
        }
    }
}
