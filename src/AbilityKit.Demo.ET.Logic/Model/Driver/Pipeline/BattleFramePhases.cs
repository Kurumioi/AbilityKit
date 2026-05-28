using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Ability.World.Services;
using AbilityKit.Demo.Moba.Share;
using AbilityKit.Pipeline;
using AbilityKit.Protocol.Moba.StateSync;
using AbilityKit.Protocol.Moba.FrameSync;
using AbilityKit.Core.Math;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Ability.Share.Impl.Moba.Struct;
using SkillInputCodec = AbilityKit.Demo.Moba.Services.SkillInputCodec;
using MobaEntityManager = AbilityKit.Demo.Moba.Services.EntityManager.MobaEntityManager;
using OpCode = AbilityKit.Demo.Moba.Services.MobaOpCode;

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
            ctx.TransformSnapshots.Clear();
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

            // 转换为 PlayerInputCommand 并提交
            var playerCommands = new List<PlayerInputCommand>(commands.Count);
            foreach (var cmd in commands)
            {
                switch (cmd)
                {
                    case MoveCommand move:
                        // 使用移动方向向量 (dx, dz)
                        var movePayload = MobaMoveCodec.Serialize(move.Dx, move.Dz);
                        var movePlayerId = new PlayerId(move.PlayerId);
                        playerCommands.Add(new PlayerInputCommand(
                            new FrameIndex(ctx.CurrentFrame),
                            movePlayerId,
                            (int)OpCode.Move,
                            movePayload));
                        Log.Debug($"[ProcessETInputPhase] Move: PlayerId={move.PlayerId}, Dir=({move.Dx:F2}, {move.Dz:F2})");
                        break;

                    case SkillCommand skill:
                        var skillEvt = new SkillInputEvent(
                            slot: skill.SkillSlot,
                            phase: SkillInputPhase.Press,
                            targetActorId: 0,
                            aimPos: new Vec3(skill.TargetX, 0, skill.TargetY));
                        var skillPayload = SkillInputCodec.Serialize(in skillEvt);
                        var skillPlayerId = new PlayerId(skill.PlayerId);
                        playerCommands.Add(new PlayerInputCommand(
                            new FrameIndex(ctx.CurrentFrame),
                            skillPlayerId,
                            (int)OpCode.SkillInput,
                            skillPayload));
                        Log.Debug($"[ProcessETInputPhase] Skill: PlayerId={skill.PlayerId}, Slot={skill.SkillSlot}");
                        break;

                    case StopCommand stop:
                        // Stop 命令使用 InputOpCodes.Stop
                        var stopPlayerId = new PlayerId(stop.PlayerId);
                        playerCommands.Add(new PlayerInputCommand(
                            new FrameIndex(ctx.CurrentFrame),
                            stopPlayerId,
                            InputOpCodes.Stop,
                            null));
                        break;
                }
            }

            // 提交到战斗逻辑层输入端口
            if (playerCommands.Count > 0)
            {
                inputPort.Submit(new FrameIndex(ctx.CurrentFrame), playerCommands);
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
    /// 从 moba.core 收集状态快照
    /// </summary>
    public sealed class CollectSnapshotPhase : AbilityInstantPhaseBase<BattleFrameContext>
    {
        public CollectSnapshotPhase() : base(BattleFramePhaseIds.CollectSnapshot) { }

        protected override void OnInstantExecute(BattleFrameContext ctx)
        {
            if (ctx.Driver == null)
            {
                return;
            }

            var scene = ctx.GetScene();
            if (scene == null)
            {
                return;
            }

            var unitComponent = ctx.GetUnitComponent();
            if (unitComponent == null || ETUnitComponentSystem.UnitCount(unitComponent) == 0)
            {
                return;
            }

            // 收集变换数据
            foreach (var unit in ETUnitComponentSystem.GetAllUnits(unitComponent))
            {
                if (unit == null || unit.IsDead)
                {
                    continue;
                }

                float x = unit.X;
                float y = unit.Y;
                float rotationY = unit.Rotation;

                // 从 moba.core 读取实际位置
                if (ctx.Driver.TryResolve(out MobaEntityManager entityManager)
                    && entityManager.TryGetActorEntity(unit.LogicActorId, out var actorEntity)
                    && actorEntity != null)
                {
                    if (actorEntity.hasTransform)
                    {
                        var transform = actorEntity.transform.Value;
                        x = transform.Position.X;
                        y = transform.Position.Z;
                        rotationY = ExtractRotationY(transform.Rotation);
                    }
                }

                ctx.TransformSnapshots.Add(new ActorTransformData(
                    actorId: unit.LogicActorId,
                    x: x,
                    y: y,
                    z: 0f,
                    rotationY: rotationY,
                    scale: 1f));
            }
        }

        private static float ExtractRotationY(in Quat rotation)
        {
            return MathF.Atan2(
                2f * (rotation.W * rotation.Y + rotation.X * rotation.Z),
                1f - 2f * (rotation.Y * rotation.Y + rotation.Z * rotation.Z));
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

            if (ctx.TransformSnapshots.Count == 0)
            {
                return;
            }

            // 构建 FrameSnapshotData
            var snapshotData = new FrameSnapshotData(
                ctx.CurrentFrame,
                0,
                SnapshotType.Delta,
                actorTransforms: ctx.TransformSnapshots);

            // 通过 ViewSink 分发给视图层
            ctx.Driver?.ViewSink?.OnActorTransformSnapshot(in snapshotData);
            ctx.SnapshotDispatched = true;
        }

        public override bool ShouldExecute(BattleFrameContext ctx)
        {
            // 仅在有快照且未分发时执行
            return ctx.TransformSnapshots.Count > 0 && !ctx.SnapshotDispatched;
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
