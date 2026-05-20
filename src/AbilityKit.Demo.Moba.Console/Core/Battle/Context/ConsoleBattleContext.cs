using System;
using System.Collections.Generic;
using AbilityKit.Core.Common.Pool;
using AbilityKit.Demo.Moba.Console.Battle;
using AbilityKit.Demo.Moba.Console.Core.Battle.ECS.Entities;
using AbilityKit.Demo.Moba.Console.Core.Input;
using AbilityKit.Demo.Moba.Console.Flow;
using AbilityKit.Demo.Moba.Console.Simulation;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Demo.Moba.Console.Core.Battle.Context
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
        /// 帧快照分发器（占位，对齐 Unity 结构）
        /// </summary>
        public object FrameSnapshots { get; set; }

        /// <summary>
        /// 快照管道（占位，对齐 Unity 结构）
        /// </summary>
        public object SnapshotPipeline { get; set; }

        /// <summary>
        /// 命令处理器（占位，对齐 Unity 结构）
        /// </summary>
        public object CmdHandler { get; set; }

        /// <summary>
        /// 输入录制器（占位，对齐 Unity 结构）
        /// </summary>
        public object InputRecordWriter { get; set; }

        /// <summary>
        /// 本地输入队列
        /// </summary>
        public BattleLocalInputQueue LocalInputQueue { get; set; }

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

            ResetHudState();
            State = BattleState.Idle;
            IsInitialized = false;

            DirtyEntities?.Clear();
            DirtyEntities = null;

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
