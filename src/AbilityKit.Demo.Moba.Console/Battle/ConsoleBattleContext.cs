using System;
using System.Collections.Generic;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Console.MobaCore;
using AbilityKit.World.ECS;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Demo.Moba.Console.Battle
{
    /// <summary>
    /// ?????
    /// ???????????
    /// </summary>
    public sealed class ConsoleBattleContext : Flow.IModuleContext, IDisposable
    {
        /// <summary>
        /// ECS ??
        /// </summary>
        public EC.EntityWorld EcsWorld { get; private set; }

        /// <summary>
        /// moba.core ?????
        /// </summary>
        public MobaCoreContext MobaCore { get; private set; } = new();

        /// <summary>
        /// ?????
        /// </summary>
        public EC.IEntity EntityNode { get; private set; }

        /// <summary>
        /// ?????
        /// </summary>
        public BattleEntityLookup EntityLookup { get; private set; }

        /// <summary>
        /// ????
        /// </summary>
        public BattleEntityFactory EntityFactory { get; private set; }

        /// <summary>
        /// ????
        /// </summary>
        public BattleStartPlan Plan { get; set; }

        /// <summary>
        /// ???? ID
        /// </summary>
        public int LocalActorId { get; set; }

        /// <summary>
        /// ????
        /// </summary>
        public int PlayerCount { get; set; }

        /// <summary>
        /// ???
        /// </summary>
        public int LastFrame { get; set; }

        /// <summary>
        /// ???????
        /// </summary>
        public double LogicTimeSeconds { get; set; }

        /// <summary>
        /// HUD ????
        /// </summary>
        public float HudMoveDx { get; set; }
        public float HudMoveDz { get; set; }
        public bool HudHasMove { get; set; }

        /// <summary>
        /// HUD ??????
        /// </summary>
        public int HudSkillClickSlot { get; set; }

        /// <summary>
        /// HUD ??????
        /// </summary>
        public bool HudSkillAiming { get; set; }
        public int HudSkillAimSlot { get; set; }
        public float HudSkillAimDx { get; set; }
        public float HudSkillAimDz { get; set; }

        /// <summary>
        /// HUD ????????
        /// </summary>
        public bool HudSkillAimSubmit { get; set; }
        public int HudSkillAimSubmitSlot { get; set; }
        public float HudSkillAimSubmitDx { get; set; }
        public float HudSkillAimSubmitDz { get; set; }

        /// <summary>
        /// ????
        /// </summary>
        public BattleState State { get; set; } = BattleState.Idle;

        /// <summary>
        /// ??????
        /// </summary>
        public bool IsInitialized { get; set; }

        /// <summary>
        /// ??? ECS ??
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
        /// ?? HUD ????
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
        /// ?????
        /// </summary>
        public void Reset()
        {
            Plan = default;
            LocalActorId = 0;
            PlayerCount = 0;
            LastFrame = 0;
            LogicTimeSeconds = 0d;
            ResetHudState();
            State = BattleState.Idle;
            IsInitialized = false;

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
    /// ????
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

    /// <summary>
    /// Console ?????????? moba.core
    /// </summary>
    public sealed class MobaCoreContext
    {
        /// <summary>
        /// moba.core World ?????
        /// </summary>
        public IWorldResolver WorldServices { get; set; }

        /// <summary>
        /// ?????
        /// </summary>
        public MobaActorRegistry ActorRegistry { get; set; }

        /// <summary>
        /// ?????
        /// </summary>
        public IMobaSkillPipelineLibrary SkillPipelineLibrary { get; set; }

        /// <summary>
        /// moba.core ?????
        /// </summary>
        public MobaCoreSkillExecutor SkillExecutor { get; set; }
    }
}
