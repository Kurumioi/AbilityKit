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

            // 获取所有到期输入，避免本地桥接输入因错过精确帧而永久滞留。
            var commands = inputComponent.GetInputsUpToFrame(ctx.CurrentFrame);
            if (commands == null || commands.Count == 0)
            {
                return;
            }

            if (!ctx.Driver.TryResolve(out IMobaBattleRuntimePort runtime) || runtime == null)
            {
                Log.Warning("[ProcessETInputPhase] IMobaBattleRuntimePort not resolved");
                return;
            }

            if (!runtime.Status.Has(MobaBattleRuntimeCapability.Input))
            {
                Log.Warning($"[ProcessETInputPhase] Runtime input capability is not ready. {runtime.Status}");
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

            // 提交到战斗运行时统一端口
            if (playerCommands.Count > 0)
            {
                PlayerInputCommand first = playerCommands[0];
                Log.Info($"[ProcessETInputPhase] Submit: Frame={ctx.CurrentFrame}, Count={playerCommands.Count}, FirstPlayer={first.Player.Value}, FirstOp={first.OpCode}");
                var result = runtime.Submit(new FrameIndex(ctx.CurrentFrame), playerCommands);
                if (!result.Succeeded)
                {
                    Log.Warning($"[ProcessETInputPhase] Submit rejected. {result}");
                    return;
                }

                inputComponent.ClearProcessedInputs(ctx.CurrentFrame);
            }
        }
    }

    /// <summary>
    /// 阶段 3: 驱动世界
    /// 驱动 ECS 世界执行所有系统
    /// </summary>
    public sealed class DriveWorldPhase : AbilityInstantPhaseBase<BattleFrameContext>
    {
        public DriveWorldPhase() : base(BattleFramePhaseIds.DriveWorld) { }

        protected override void OnInstantExecute(BattleFrameContext ctx)
        {
            if (ctx.Driver == null || ctx.Driver.World == null)
            {
                return;
            }

            // 驱动 World Tick
            ctx.Driver.World.Tick(ctx.DeltaTime);

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

            if (!ctx.Driver.TryResolve(out IMobaBattleRuntimePort runtime) || runtime == null)
            {
                Log.Warning("[CollectSnapshotPhase] IMobaBattleRuntimePort not resolved");
                return;
            }

            if (!runtime.Status.Has(MobaBattleRuntimeCapability.SnapshotOutput))
            {
                Log.Warning($"[CollectSnapshotPhase] Runtime snapshot capability is not ready. {runtime.Status}");
                return;
            }

            _runtimeSnapshots.Clear();
            runtime.CollectSnapshots(new FrameIndex(ctx.CurrentFrame), _runtimeSnapshots);

            for (int i = 0; i < _runtimeSnapshots.Count; i++)
            {
                var runtimeSnapshot = _runtimeSnapshots[i];
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
