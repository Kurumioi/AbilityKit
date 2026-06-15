using System;
using System.Collections.Generic;
using AbilityKit.Core.Pooling;
using AbilityKit.Demo.Moba.Console.Battle.Config;
using AbilityKit.Demo.Moba.Console.Battle.ECS;
using AbilityKit.Demo.Moba.Console.Battle.ECS.Entities;
using AbilityKit.Demo.Moba.Console.Battle.Flow;
using AbilityKit.Demo.Moba.Console.Battle.Input;
using AbilityKit.Demo.Moba.Console.Battle.Session;
using EC = AbilityKit.World.ECS;
using ConsoleIBattleEntityQuery = AbilityKit.Demo.Moba.Console.Battle.ECS.IBattleEntityQuery;

namespace AbilityKit.Demo.Moba.Console.Battle.Context
{
    /// <summary>
    /// 战斗上下文
    /// 对齐 Unity BattleContext，管理表现层状态
    /// </summary>
    public sealed class ConsoleBattleContext : IModuleContext, IDisposable
    {
        private static readonly ObjectPool<ConsoleBattleContext> Pool = Pools.GetPool(
            key: "ConsoleBattleContext",
            createFunc: () => new ConsoleBattleContext(),
            defaultCapacity: 1,
            maxSize: 8);

        /// <summary>
        /// 战斗逻辑会话（对齐 Unity BattleContext.Session）
        /// Console 版本使用 SyncAdapter 替代
        /// </summary>
        public object Session { get; set; }

        /// <summary>
        /// 会话钩子（对齐 Unity BattleContext.Hooks）
        /// </summary>
        public ConsoleSessionHooks? Hooks { get; set; }

        /// <summary>
        /// 战斗启动计划
        /// </summary>
        public BattleStartPlan Plan { get; set; }

        /// <summary>
        /// 当前帧
        /// </summary>
        public int LastFrame { get; set; }

        /// <summary>
        /// 逻辑时间（秒）
        /// </summary>
        public double LogicTimeSeconds { get; set; }

        /// <summary>
        /// 本地玩家 ID
        /// </summary>
        public int LocalActorId { get; set; }

        /// <summary>
        /// 玩家数量
        /// </summary>
        public int PlayerCount { get; set; }

        /// <summary>
        /// 帧快照分发器（对齐 Unity BattleContext.FrameSnapshots）
        /// Console 版本使用 ConsoleBattleViewEventSink
        /// </summary>
        public object FrameSnapshots { get; set; }

        /// <summary>
        /// 快照管道（对齐 Unity BattleContext.SnapshotPipeline）
        /// </summary>
        public object SnapshotPipeline { get; set; }

        /// <summary>
        /// 命令处理器（对齐 Unity BattleContext.CmdHandler）
        /// </summary>
        public object CmdHandler { get; set; }

        /// <summary>
        /// 输入录制器（对齐 Unity BattleContext.InputRecordWriter）
        /// </summary>
        public object InputRecordWriter { get; set; }

        /// <summary>
        /// 本地输入队列
        /// </summary>
        public BattleLocalInputQueue LocalInputQueue { get; set; }

        /// <summary>
        /// 运行时世界 ID（对齐 Unity BattleContext.RuntimeWorldId）
        /// </summary>
        public string RuntimeWorldId { get; set; }

        /// <summary>
        /// 是否有运行时世界 ID（对齐 Unity BattleContext.HasRuntimeWorldId）
        /// </summary>
        public bool HasRuntimeWorldId { get; set; }

        /// <summary>
        /// ECS 世界
        /// </summary>
        public EC.IECWorld EcsWorld { get; set; }

        /// <summary>
        /// 实体节点
        /// </summary>
        public EC.IEntity EntityNode { get; set; }

        /// <summary>
        /// 实体查找器
        /// </summary>
        public BattleEntityLookup EntityLookup { get; set; }

        /// <summary>
        /// 实体工厂
        /// </summary>
        public BattleEntityFactory EntityFactory { get; set; }

        /// <summary>
        /// 脏实体列表（需要更新的实体）
        /// </summary>
        public List<EC.IEntityId> DirtyEntities { get; set; }

        /// <summary>
        /// 实体查询器
        /// </summary>
        public ConsoleIBattleEntityQuery EntityQuery { get; set; }

        /// <summary>
        /// HUD 移动输入
        /// </summary>
        public float HudMoveDx { get; set; }
        public float HudMoveDz { get; set; }
        public bool HudHasMove { get; set; }

        /// <summary>
        /// HUD 技能点击输入
        /// </summary>
        public int HudSkillClickSlot { get; set; }

        /// <summary>
        /// HUD 技能瞄准输入
        /// </summary>
        public bool HudSkillAiming { get; set; }
        public int HudSkillAimSlot { get; set; }
        public float HudSkillAimDx { get; set; }
        public float HudSkillAimDz { get; set; }

        /// <summary>
        /// HUD 技能瞄准释放输入
        /// </summary>
        public bool HudSkillAimSubmit { get; set; }
        public int HudSkillAimSubmitSlot { get; set; }
        public float HudSkillAimSubmitDx { get; set; }
        public float HudSkillAimSubmitDz { get; set; }

        /// <summary>
        /// 战斗状态
        /// </summary>
        public BattleState State { get; set; } = BattleState.Idle;

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized { get; set; }

        /// <summary>
        /// 从对象池获取实例
        /// </summary>
        public static ConsoleBattleContext Rent()
        {
            return Pool.Get();
        }

        /// <summary>
        /// 归还实例到对象池
        /// </summary>
        public static void Return(ConsoleBattleContext ctx)
        {
            if (ctx == null) return;
            Pool.Release(ctx);
        }

        /// <summary>
        /// 初始化 ECS 世界
        /// </summary>
        public void InitializeEcsWorld()
        {
            EcsWorld = new EC.EntityWorld();
            EntityNode = EcsWorld.Create("BattleEntities");
            EntityLookup = new BattleEntityLookup();
            EntityFactory = new BattleEntityFactory(EcsWorld, EntityLookup, EntityNode);
            EntityQuery = new BattleEntityQuery(EcsWorld, EntityLookup);

            Platform.Log.Entity("ECS World initialized");
        }

        /// <summary>
        /// 重置 HUD 状态
        /// </summary>
        public void ResetHudState()
        {
            HudMoveDx = 0f;
            HudMoveDz = 0f;
            HudHasMove = false;
            HudSkillClickSlot = 0;
            HudSkillAiming = false;
            HudSkillAimSlot = 0;
            HudSkillAimDx = 0f;
            HudSkillAimDz = 0f;
            HudSkillAimSubmit = false;
            HudSkillAimSubmitSlot = 0;
            HudSkillAimSubmitDx = 0f;
            HudSkillAimSubmitDz = 0f;
        }

        /// <summary>
        /// 重置上下文
        /// </summary>
        public void Reset()
        {
            Session = null;
            Hooks = null;
            Plan = default;
            LastFrame = 0;
            LogicTimeSeconds = 0d;
            LocalActorId = 0;
            PlayerCount = 0;

            FrameSnapshots = null;
            SnapshotPipeline = null;
            CmdHandler = null;

            InputRecordWriter = null;

            LocalInputQueue = null;

            RuntimeWorldId = null;
            HasRuntimeWorldId = false;

            ResetHudState();
            State = BattleState.Idle;
            IsInitialized = false;

            DirtyEntities?.Clear();
            DirtyEntities = null;

            EntityQuery = null;
            EntityLookup?.Clear();
            EntityFactory = null;
            EntityNode = default;
            EcsWorld = null;
        }

        public void Dispose()
        {
            Reset();
        }
    }

    /// <summary>
    /// 战斗状态
    /// </summary>
    public enum BattleState
    {
        Idle = 0,
        Prepare = 1,
        Connect = 2,
        CreateOrJoinWorld = 3,
        LoadAssets = 4,
        InMatch = 5,
        End = 6
    }
}
