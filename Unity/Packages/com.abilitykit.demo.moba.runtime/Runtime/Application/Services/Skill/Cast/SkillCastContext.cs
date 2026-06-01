using AbilityKit.Ability.Share.ECS;
using AbilityKit.ECS;
using AbilityKit.Core.Math;
using AbilityKit.Ability.Triggering;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Triggering.Eventing;

namespace AbilityKit.Demo.Moba.Services
{
    public sealed class SkillCastContext : IMobaActorContextProvider
    {
        public int SkillId;
        public int SkillSlot;
        public int SkillLevel;

        public int Sequence;

        public MobaSkillCastRuntimeHandle RuntimeHandle;
        public long RuntimeId;
        public long SourceContextId;

        public string FailReason;

        public int CasterActorId;
        public int TargetActorId;

        public Vec3 AimPos;
        public Vec3 AimDir;

        public IWorldResolver WorldServices;
        public AbilityKit.Triggering.Eventing.IEventBus EventBus;
        public IUnitFacade CasterUnit;
        public IUnitFacade TargetUnit;

        public SkillCastContext()
        {
        }

        public SkillCastContext(
            int skillId,
            int skillSlot,
            int skillLevel,
            int sequence,
            int casterActorId,
            int targetActorId,
            in Vec3 aimPos,
            in Vec3 aimDir,
            IWorldResolver worldServices,
            AbilityKit.Triggering.Eventing.IEventBus eventBus,
            IUnitFacade casterUnit,
            IUnitFacade targetUnit)
        {
            Initialize(
                skillId,
                skillSlot,
                skillLevel,
                sequence,
                casterActorId,
                targetActorId,
                in aimPos,
                in aimDir,
                worldServices,
                eventBus,
                casterUnit,
                targetUnit);
        }

        public void Initialize(in SkillCastRequest req, int skillLevel, int sequence = 0)
        {
            Initialize(
                req.SkillId,
                req.SkillSlot,
                skillLevel,
                sequence,
                req.CasterActorId,
                req.TargetActorId,
                in req.AimPos,
                in req.AimDir,
                req.WorldServices,
                req.EventBus,
                req.CasterUnit,
                req.TargetUnit);
        }

        public void Initialize(
            int skillId,
            int skillSlot,
            int skillLevel,
            int sequence,
            int casterActorId,
            int targetActorId,
            in Vec3 aimPos,
            in Vec3 aimDir,
            IWorldResolver worldServices,
            AbilityKit.Triggering.Eventing.IEventBus eventBus,
            IUnitFacade casterUnit,
            IUnitFacade targetUnit)
        {
            SkillId = skillId;
            SkillSlot = skillSlot;
            SkillLevel = skillLevel;
            Sequence = sequence;
            RuntimeHandle = default;
            RuntimeId = 0L;
            SourceContextId = 0L;
            FailReason = null;
            CasterActorId = casterActorId;
            TargetActorId = targetActorId;
            AimPos = aimPos;
            AimDir = aimDir;
            WorldServices = worldServices;
            EventBus = eventBus;
            CasterUnit = casterUnit;
            TargetUnit = targetUnit;
        }

        public void Reset()
        {
            SkillId = 0;
            SkillSlot = 0;
            SkillLevel = 0;
            Sequence = 0;
            RuntimeHandle = default;
            RuntimeId = 0L;
            SourceContextId = 0L;
            FailReason = null;
            CasterActorId = 0;
            TargetActorId = 0;
            AimPos = Vec3.Zero;
            AimDir = Vec3.Forward;
            WorldServices = null;
            EventBus = null;
            CasterUnit = null;
            TargetUnit = null;
        }

        public static SkillCastContext FromRequest(in SkillCastRequest req, int skillLevel)
        {
            return SkillCastContextBuilder.Create()
                .FromRequest(in req)
                .WithSkillLevel(skillLevel)
                .Build();
        }

        public bool TryGetSourceActorId(out int actorId)
        {
            actorId = CasterActorId;
            return actorId > 0;
        }

        public bool TryGetTargetActorId(out int actorId)
        {
            actorId = TargetActorId;
            return actorId > 0;
        }
    }

    public readonly struct MobaSkillCastResult
    {
        public MobaSkillCastResult(bool success, string failReason, in MobaSkillCastRuntimeHandle runtimeHandle)
        {
            Success = success;
            FailReason = failReason;
            RuntimeHandle = runtimeHandle;
        }

        public bool Success { get; }
        public string FailReason { get; }
        public MobaSkillCastRuntimeHandle RuntimeHandle { get; }
        public long RuntimeId => RuntimeHandle.RuntimeId;

        public static MobaSkillCastResult Failed(string failReason)
        {
            return new MobaSkillCastResult(false, failReason, default);
        }

        public static MobaSkillCastResult From(bool success, string failReason, in MobaSkillCastRuntimeHandle runtimeHandle)
        {
            return new MobaSkillCastResult(success, failReason, in runtimeHandle);
        }
    }
}
