using System;

namespace AbilityKit.Demo.Moba.Share.Config
{
    [Serializable]
    public sealed class SkillDTO
    {
        public int Id;
        public string Name;
        public int CooldownMs;
        public int Range;
        public int IconId;
        public int Category;
        public int[] Tags;

        public int SkillButtonTemplateId;

        public int LevelTableId;
        public int PreCastFlowId;
        public int CastFlowId;
    }

    [Serializable]
    public sealed class SkillButtonTemplateDTO
    {
        public int Id;
        public string Name;

        public float LongPressSeconds;
        public float DragThreshold;
        public bool EnableAim;

        public int AimMode;
        public float AimMaxRadius;

        public int UsePointMode;
        public float SelectRange;
        public bool FaceToAim;
    }

    [Serializable]
    public sealed class PassiveSkillDTO
    {
        public int Id;
        public string Name;
        public int CooldownMs;
        public int[] TriggerIds;
    }

    [Serializable]
    public sealed class SkillFlowDTO
    {
        public int Id;
        public string Name;
        public int PipelineContinuousTagTemplateId;
        public SkillPhaseDTO[] Phases;
    }

    public enum SkillPhaseType
    {
        Checks = 1,
        Timeline = 2,
        Handlers = 3,
        RulePlan = 4,
        Sequence = 10,
        Parallel = 11,
        Repeat = 12,
        Delay = 13,
        WaitUntil = 14,
    }

    [Serializable]
    public sealed class SkillPhaseDTO
    {
        public int Type;
        public string PhaseId;
        public SkillChecksPhaseDTO Checks;
        public SkillTimelinePhaseDTO Timeline;
        public SkillFlowHandlerConfigDTO Handlers;
        public SkillRulePlanPhaseDTO RulePlan;
        public SkillPhaseDTO[] Children;
        public SkillRepeatPhaseDTO Repeat;
        public SkillDelayPhaseDTO Delay;
        public SkillWaitUntilPhaseDTO WaitUntil;
    }

    [Serializable]
    public sealed class SkillRulePlanPhaseDTO
    {
        public int[] TriggerIds;
        public bool AbortOnFailure = true;
        public string FailReason;
    }

    [Serializable]
    public sealed class SkillRepeatPhaseDTO
    {
        public int RepeatCount;
        public int IntervalMs;
        public SkillPhaseDTO Phase;
    }

    [Serializable]
    public sealed class SkillDelayPhaseDTO
    {
        public int DelayMs;
    }

    [Serializable]
    public sealed class SkillWaitUntilPhaseDTO
    {
        public string Condition;
        public int TimeoutMs;
        public bool CompleteOnTimeout = true;
        public int[] ObservedSlots;
    }

    [Serializable]
    public sealed class SkillChecksPhaseDTO
    {
        public bool CheckCooldown;
        public bool CheckCastingState;
        public int[] RequiredTags;
        public int[] BlockedTags;
    }

    [Serializable]
    public sealed class SkillTimelinePhaseDTO
    {
        public int DurationMs;
        public SkillTimelineEventDTO[] Events;
    }

    [Serializable]
    public sealed class SkillTimelineEventDTO
    {
        public int AtMs;
        public int EffectId;
        public int ExecuteMode;
        public string EventTag;
    }
}
