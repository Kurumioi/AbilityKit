using System;

namespace AbilityKit.Demo.Moba.Share.Config
{
    [Serializable]
    public sealed class ContinuousModifierDTO
    {
        public int TargetKind;
        public int TargetId;
        public int AttrTypeId;
        public int Op;
        public float Value;
        public int Priority;

        // 0/Fixed keeps existing configs compatible. Other values map to AbilityKit.Modifiers.MagnitudeSourceType.
        public int MagnitudeSourceType;
        public float MagnitudeBaseValue;
        public float MagnitudeCoefficient;
        public float MagnitudeDuration;
        public int MagnitudeDecayType;
        public int MagnitudeAttributeTypeId;
        public string MagnitudeContextKey;
        public float[] MagnitudeCurve;

        // 0/Realtime recalculates from the source whenever the target value is recomputed.
        // 1/OnApplySnapshot evaluates once when the continuous modifier is projected, then stores a fixed value.
        public int EvaluationPolicy;
    }

    [Serializable]
    public sealed class BuffDTO
    {
        public int Id;
        public string Name;
        public int DurationMs;

        public int[] OnAddEffects;
        public int[] OnRemoveEffects;
        public int[] OnIntervalEffects;
        public int IntervalMs;
        public int PresentationTemplateId;
        public int StackingPolicy;
        public int RefreshPolicy;
        public int MaxStacks;
        public int[] TriggerIds;
        public int ContinuousTagTemplateId;
        public int[] Tags;
        public ContinuousModifierDTO[] Modifiers;
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
