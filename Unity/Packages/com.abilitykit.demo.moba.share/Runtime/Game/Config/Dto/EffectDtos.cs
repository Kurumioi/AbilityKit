using System;

namespace AbilityKit.Demo.Moba.Share.Config
{
    [Serializable]
    public sealed class BuffDTO
    {
        public int Id;
        public string Name;
        public int DurationMs;

        public int OngoingEffectId;

        public int[] OnAddEffects;
        public int[] OnRemoveEffects;
        public int[] OnIntervalEffects;
        public int IntervalMs;
        public int StackingPolicy;
        public int RefreshPolicy;
        public int MaxStacks;
        public int[] TriggerIds;
        public int[] Tags;
    }

    [Serializable]
    public sealed class OngoingEffectDTO
    {
        public int Id;
        public string Name;

        public int DurationMs;
        public int PeriodMs;

        public int OnApplyEffectId;
        public int OnTickEffectId;
        public int OnRemoveEffectId;
    }

    public enum SkillEffectType
    {
        Damage = 1,
        AddBuff = 2,
    }

    [Serializable]
    public sealed class SkillEffectDTO
    {
        public int Type;
        public DamageEffectDTO Damage;
        public AddBuffEffectDTO AddBuff;
    }

    [Serializable]
    public sealed class DamageEffectDTO
    {
        public int FormulaType;
        public float Value;
        public float Scale;
        public int AttrTypeId;
        public int DamageType;
    }

    [Serializable]
    public sealed class AddBuffEffectDTO
    {
        public int BuffId;
        public int DurationMsOverride;
    }
}
