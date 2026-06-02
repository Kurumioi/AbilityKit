using System.Collections.Generic;
using AbilityKit.GameplayTags;
using AbilityKit.Modifiers;

namespace AbilityKit.Demo.Moba.Services
{
    /// <summary>
    /// MOBA continuous config decoration for gameplay tag projection.
    /// </summary>
    public interface IMobaContinuousTagConfig
    {
        ContinuousTagRequirements TagRequirements { get; }
    }

    /// <summary>
    /// MOBA continuous config decoration for runtime state modifier projection.
    /// </summary>
    public interface IMobaContinuousModifierConfig
    {
        IReadOnlyList<IMobaContinuousModifierSpec> Modifiers { get; }
    }

    /// <summary>
    /// MOBA continuous config decoration for periodic behavior projection.
    /// </summary>
    public interface IMobaContinuousPeriodicConfig
    {
        float IntervalSeconds { get; }
        IReadOnlyList<int> IntervalEffectIds { get; }
    }

    public static class MobaContinuousModifierTargetKind
    {
        public const int Attribute = 1;
        public const int StateMachineParameter = 2;
        public const int StateFlag = 3;
        public const int Custom = 255;
    }

    public static class MobaContinuousModifierEvaluationPolicy
    {
        public const int Realtime = 0;
        public const int OnApplySnapshot = 1;
    }

    /// <summary>
    /// MOBA-side state modifier declaration held by a continuous config.
    /// </summary>
    public interface IMobaContinuousModifierSpec
    {
        int TargetKind { get; }
        int TargetId { get; }
        int Op { get; }
        float Value { get; }
        MagnitudeSource Magnitude { get; }
        int EvaluationPolicy { get; }
        int Priority { get; }
    }

    /// <summary>
    /// Runtime projection metadata used when binding continuous config decorations.
    /// </summary>
    public interface IMobaContinuousProjectionConfig
    {
        int OwnerActorId { get; }
        int ModifierSourceId { get; }
        GameplayTagSource TagSource { get; }
    }
}
