using System;

namespace ET.AbilityKit.Demo.ET.Share
{
    /// <summary>
    /// ET Demo 常量
    /// </summary>
    public static class DemoConstants
    {
        public const string Name = "ETDemo";
        public const string Version = "1.0.0";
    }

    /// <summary>
    /// ET View 事件 Sink 接口 - 逻辑层通知视图层
    /// </summary>
    public interface IETViewEventSink
    {
        // 单位事件
        void OnActorSpawn(ActorSpawnEvent evt);
        void OnActorDead(ActorDeadEvent evt);
        void OnActorMove(ActorMoveEvent evt);
        void OnActorDamage(ActorDamageEvent evt);
        void OnActorAttributeChange(ActorAttributeChangeEvent evt);

        // 技能事件
        void OnSkillCast(SkillCastEvent evt);
        void OnSkillHit(SkillHitEvent evt);

        // 特效事件
        void OnVfxSpawn(VfxSpawnEvent evt);
        void OnFloatingText(FloatingTextEvent evt);

        // 战斗事件
        void OnBattleStart(BattleStartEvent evt);
        void OnBattleEnd(BattleEndEvent evt);
        void OnFrameTick(FrameTickEvent evt);
        void OnFrameSyncComplete(int frame);
    }

    /// <summary>
    /// ET 输入 Sink 接口 - 输入层提交到逻辑层
    /// </summary>
    public interface IETInputSink
    {
        void SubmitMoveInput(int frame, long actorId, float x, float y);
        void SubmitSkillInput(int frame, long actorId, int skillSlot, float targetX, float targetY);
        void SubmitStopInput(int frame, long actorId);
    }

    /// <summary>
    /// 战斗上下文 Sink 接口 - 提供战斗上下文数据访问
    /// </summary>
    public interface IETBattleContextSink
    {
        int CurrentFrame { get; }
        float LogicTimeSeconds { get; }
        long LocalActorId { get; }
        BattleState State { get; }
        ActorData? GetActor(long actorId);
    }

    /// <summary>
    /// Actor 基础数据
    /// </summary>
    public struct ActorData
    {
        public long ActorId;
        public string Name;
        public ActorKind Kind;
        public float X;
        public float Y;
        public float Rotation;
        public float Hp;
        public float MaxHp;
        public float Attack;
        public float Defense;
        public float MoveSpeed;
        public bool IsDead;
        public bool IsLocalPlayer;

        public ActorData(long actorId, string name, ActorKind kind, float x, float y)
        {
            ActorId = actorId;
            Name = name;
            Kind = kind;
            X = x;
            Y = y;
            Rotation = 0;
            Hp = 100f;
            MaxHp = 100f;
            Attack = 10f;
            Defense = 5f;
            MoveSpeed = 5f;
            IsDead = false;
            IsLocalPlayer = false;
        }
    }

    /// <summary>
    /// 技能数据
    /// </summary>
    public struct SkillData
    {
        public int SkillId;
        public string Name;
        public float Cooldown;
        public float Range;
        public float Damage;

        public SkillData(int skillId, string name, float cooldown, float range, float damage)
        {
            SkillId = skillId;
            Name = name;
            Cooldown = cooldown;
            Range = range;
            Damage = damage;
        }
    }

    /// <summary>
    /// 移动命令
    /// </summary>
    public struct MoveCommand
    {
        public int Frame;
        public int ActorId;  // 逻辑层 EntityCode
        public float TargetX;
        public float TargetY;

        public MoveCommand(int frame, int actorId, float targetX, float targetY)
        {
            Frame = frame;
            ActorId = actorId;
            TargetX = targetX;
            TargetY = targetY;
        }
    }

    /// <summary>
    /// 技能命令
    /// </summary>
    public struct SkillCommand
    {
        public int Frame;
        public int ActorId;  // 逻辑层 EntityCode
        public int SkillSlot;
        public float TargetX;
        public float TargetY;

        public SkillCommand(int frame, int actorId, int skillSlot, float targetX, float targetY)
        {
            Frame = frame;
            ActorId = actorId;
            SkillSlot = skillSlot;
            TargetX = targetX;
            TargetY = targetY;
        }
    }

    /// <summary>
    /// 停止命令
    /// </summary>
    public struct StopCommand
    {
        public int Frame;
        public int ActorId;  // 逻辑层 EntityCode

        public StopCommand(int frame, int actorId)
        {
            Frame = frame;
            ActorId = actorId;
        }
    }
}
