using AbilityKit.Core.Mathematics;
using AbilityKit.Core.Eventing;
using AbilityKit.Demo.Moba.Services;

namespace AbilityKit.Demo.Moba
{
    using AbilityKit.Demo.Moba;

    public static class MobaEventIds
    {
        public static class Common
        {
            public const string DamageApplied = "common.damage_applied";
        }

        public static class Ability
        {
            public const string SkillCastStarted = "ability.skill_cast_started";
            public const string SkillStage = "ability.skill_stage";
            public const string SkillCastEnded = "ability.skill_cast_ended";
            public const string SkillHitConfirmed = "ability.skill_hit_confirmed";
        }
    }

    public static class MobaPresentationTriggering
    {
        public static class Events
        {
            public const string Play = "presentation.play";
            public const string Stop = "presentation.stop";
        }
    }

    public static class MobaRuntimeKindNames
    {
        public const string Skill = "Skill";
        public const string SkillPipeline = "SkillPipeline";
        public const string Effect = "Effect";
        public const string Action = "Action";
        public const string Buff = "Buff";
        public const string Projectile = "Projectile";
        public const string ProjectileHit = "ProjectileHit";
        public const string Area = "Area";
        public const string AreaEnter = "AreaEnter";
        public const string Summon = "Summon";
        public const string Presentation = "Presentation";
        public const string Unit = "Unit";
        public const string UnitDeath = "UnitDeath";
        public const string DamageAttack = "DamageAttack";
        public const string DamageCalc = "DamageCalc";
        public const string DamageResult = "DamageResult";
        public const string Actor = "Actor";
    }

    public static class MobaStrongEvents
    {
        public static class Buff
        {
            public static readonly EventKey<BuffAddedArgs> Added = new EventKey<BuffAddedArgs>(MobaBuffTriggering.Events.Added);
            public static readonly EventKey<BuffRemovedArgs> Removed = new EventKey<BuffRemovedArgs>(MobaBuffTriggering.Events.Removed);
            public static readonly EventKey<BuffStackChangedArgs> StackChanged = new EventKey<BuffStackChangedArgs>(MobaBuffTriggering.Events.StackChanged);
            public static readonly EventKey<BuffTickArgs> Tick = new EventKey<BuffTickArgs>(MobaBuffTriggering.Events.Tick);
            public static readonly EventKey<BuffEffectTickArgs> EffectTick = new EventKey<BuffEffectTickArgs>(MobaBuffTriggering.Events.EffectTick);
        }

        public static class Common
        {
            public static readonly EventKey<DamageAppliedArgs> DamageApplied = new EventKey<DamageAppliedArgs>(MobaEventIds.Common.DamageApplied);
        }

        public static class Ability
        {
            public static readonly EventKey<SkillCastStartedArgs> SkillCastStarted = new EventKey<SkillCastStartedArgs>(MobaEventIds.Ability.SkillCastStarted);
            public static readonly EventKey<SkillStageArgs> SkillStage = new EventKey<SkillStageArgs>(MobaEventIds.Ability.SkillStage);
            public static readonly EventKey<SkillCastEndedArgs> SkillCastEnded = new EventKey<SkillCastEndedArgs>(MobaEventIds.Ability.SkillCastEnded);

            public static readonly EventKey<SkillHitConfirmedArgs> SkillHitConfirmed = new EventKey<SkillHitConfirmedArgs>(MobaEventIds.Ability.SkillHitConfirmed);
        }

    }

    public enum SkillCastEndReason
    {
        None = 0,
        Complete = 1,
        Cancel = 2,
        Interrupted = 3,
        Fail = 4,
    }

    public enum BuffRemoveReason
    {
        None = 0,
        Expire = 1,
        Dispel = 2,
        Override = 3,
        Death = 4,
        Clear = 5,
    }

    public readonly struct BuffAddedArgs
    {
        public readonly int OwnerActorId;
        public readonly int BuffId;
        public readonly int BuffInstanceId;
        public readonly int StackCount;
        public readonly float DurationSeconds;

        public BuffAddedArgs(int ownerActorId, int buffId, int buffInstanceId, int stackCount, float durationSeconds)
        {
            OwnerActorId = ownerActorId;
            BuffId = buffId;
            BuffInstanceId = buffInstanceId;
            StackCount = stackCount;
            DurationSeconds = durationSeconds;
        }
    }

    public readonly struct BuffRemovedArgs
    {
        public readonly int OwnerActorId;
        public readonly int BuffId;
        public readonly int BuffInstanceId;
        public readonly BuffRemoveReason Reason;

        public BuffRemovedArgs(int ownerActorId, int buffId, int buffInstanceId, BuffRemoveReason reason)
        {
            OwnerActorId = ownerActorId;
            BuffId = buffId;
            BuffInstanceId = buffInstanceId;
            Reason = reason;
        }
    }

    public readonly struct BuffStackChangedArgs
    {
        public readonly int OwnerActorId;
        public readonly int BuffId;
        public readonly int BuffInstanceId;
        public readonly int OldStack;
        public readonly int NewStack;

        public BuffStackChangedArgs(int ownerActorId, int buffId, int buffInstanceId, int oldStack, int newStack)
        {
            OwnerActorId = ownerActorId;
            BuffId = buffId;
            BuffInstanceId = buffInstanceId;
            OldStack = oldStack;
            NewStack = newStack;
        }
    }

    public readonly struct BuffTickArgs
    {
        public readonly int OwnerActorId;
        public readonly int BuffId;
        public readonly int BuffInstanceId;
        public readonly int StackCount;
        public readonly float DeltaSeconds;
        public readonly int TickIndex;

        public BuffTickArgs(int ownerActorId, int buffId, int buffInstanceId, int stackCount, float deltaSeconds, int tickIndex)
        {
            OwnerActorId = ownerActorId;
            BuffId = buffId;
            BuffInstanceId = buffInstanceId;
            StackCount = stackCount;
            DeltaSeconds = deltaSeconds;
            TickIndex = tickIndex;
        }
    }

    public readonly struct BuffEffectTickArgs
    {
        public readonly int OwnerActorId;
        public readonly int BuffId;
        public readonly int BuffInstanceId;
        public readonly int EffectId;
        public readonly int StackCount;
        public readonly float DeltaSeconds;
        public readonly int TickIndex;

        public BuffEffectTickArgs(int ownerActorId, int buffId, int buffInstanceId, int effectId, int stackCount, float deltaSeconds, int tickIndex)
        {
            OwnerActorId = ownerActorId;
            BuffId = buffId;
            BuffInstanceId = buffInstanceId;
            EffectId = effectId;
            StackCount = stackCount;
            DeltaSeconds = deltaSeconds;
            TickIndex = tickIndex;
        }
    }

    public readonly struct DamageAppliedArgs
    {
        public readonly int SourceActorId;
        public readonly int TargetActorId;
        public readonly int FinalDamage;
        public readonly int DamageTypeId;
        public readonly bool IsCritical;
        public readonly int ShieldAbsorbed;
        public readonly int HpBefore;
        public readonly int HpAfter;

        public DamageAppliedArgs(int sourceActorId, int targetActorId, int finalDamage, int damageTypeId, bool isCritical, int shieldAbsorbed, int hpBefore, int hpAfter)
        {
            SourceActorId = sourceActorId;
            TargetActorId = targetActorId;
            FinalDamage = finalDamage;
            DamageTypeId = damageTypeId;
            IsCritical = isCritical;
            ShieldAbsorbed = shieldAbsorbed;
            HpBefore = hpBefore;
            HpAfter = hpAfter;
        }
    }

    public readonly struct SkillCastStartedArgs
    {
        public readonly int CasterActorId;
        public readonly int SkillId;
        public readonly int CastContextId;
        public readonly int TargetActorId;
        public readonly Vec3 AimPos;
        public readonly Vec3 AimDir;

        public SkillCastStartedArgs(int casterActorId, int skillId, int castContextId, int targetActorId, in Vec3 aimPos, in Vec3 aimDir)
        {
            CasterActorId = casterActorId;
            SkillId = skillId;
            CastContextId = castContextId;
            TargetActorId = targetActorId;
            AimPos = aimPos;
            AimDir = aimDir;
        }
    }

    public readonly struct SkillStageArgs
    {
        public readonly int CasterActorId;
        public readonly int SkillId;
        public readonly int CastContextId;
        public readonly int StageId;
        public readonly int TimeMs;
        public readonly int StageIndex;

        public SkillStageArgs(int casterActorId, int skillId, int castContextId, int stageId, int timeMs, int stageIndex)
        {
            CasterActorId = casterActorId;
            SkillId = skillId;
            CastContextId = castContextId;
            StageId = stageId;
            TimeMs = timeMs;
            StageIndex = stageIndex;
        }
    }

    public readonly struct SkillCastEndedArgs
    {
        public readonly int CasterActorId;
        public readonly int SkillId;
        public readonly int CastContextId;
        public readonly SkillCastEndReason EndReason;
        public readonly int ElapsedMs;

        public SkillCastEndedArgs(int casterActorId, int skillId, int castContextId, SkillCastEndReason endReason, int elapsedMs)
        {
            CasterActorId = casterActorId;
            SkillId = skillId;
            CastContextId = castContextId;
            EndReason = endReason;
            ElapsedMs = elapsedMs;
        }
    }

    public readonly struct SkillHitConfirmedArgs
    {
        public readonly int CasterActorId;
        public readonly int TargetActorId;
        public readonly int SkillId;
        public readonly int CastContextId;
        public readonly int HitIndex;
        public readonly int ProjectileInstanceId;
        public readonly Vec3 HitPos;

        public SkillHitConfirmedArgs(int casterActorId, int targetActorId, int skillId, int castContextId, int hitIndex, int projectileInstanceId, in Vec3 hitPos)
        {
            CasterActorId = casterActorId;
            TargetActorId = targetActorId;
            SkillId = skillId;
            CastContextId = castContextId;
            HitIndex = hitIndex;
            ProjectileInstanceId = projectileInstanceId;
            HitPos = hitPos;
        }
    }
}
