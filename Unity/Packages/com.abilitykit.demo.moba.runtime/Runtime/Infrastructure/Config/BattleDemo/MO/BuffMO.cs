using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Share.Config;

namespace AbilityKit.Demo.Moba.Config.BattleDemo.MO
{
    public sealed class ContinuousModifierMO : IMobaContinuousModifierSpec
    {
        public ContinuousModifierMO(ContinuousModifierDTO dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            TargetKind = dto.TargetKind != 0 ? dto.TargetKind : MobaContinuousModifierTargetKind.Attribute;
            TargetId = dto.TargetId != 0 ? dto.TargetId : dto.AttrTypeId;
            Op = dto.Op;
            Value = dto.Value;
            Priority = dto.Priority;
        }

        public int TargetKind { get; }
        public int TargetId { get; }
        public int Op { get; }
        public float Value { get; }
        public int Priority { get; }
    }

    public sealed class BuffMO
    {
        public int Id { get; }
        public string Name { get; }
        public int DurationMs { get; }

        public IReadOnlyList<int> OnAddEffects { get; }
        public IReadOnlyList<int> OnRemoveEffects { get; }
        public IReadOnlyList<int> OnIntervalEffects { get; }
        public int IntervalMs { get; }
        public BuffStackingPolicy StackingPolicy { get; }
        public BuffRefreshPolicy RefreshPolicy { get; }
        public int MaxStacks { get; }
        public IReadOnlyList<int> TriggerIds { get; }
        public int ContinuousTagTemplateId { get; }
        public IReadOnlyList<int> Tags { get; }
        public IReadOnlyList<ContinuousModifierMO> Modifiers { get; }

        public BuffMO(BuffDTO dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            Id = dto.Id;
            Name = dto.Name;
            DurationMs = dto.DurationMs;

            OnAddEffects = dto.OnAddEffects ?? Array.Empty<int>();
            OnRemoveEffects = dto.OnRemoveEffects ?? Array.Empty<int>();
            OnIntervalEffects = dto.OnIntervalEffects ?? Array.Empty<int>();
            IntervalMs = dto.IntervalMs;
            StackingPolicy = (BuffStackingPolicy)dto.StackingPolicy;
            RefreshPolicy = (BuffRefreshPolicy)dto.RefreshPolicy;
            MaxStacks = dto.MaxStacks;
            TriggerIds = dto.TriggerIds ?? Array.Empty<int>();
            ContinuousTagTemplateId = dto.ContinuousTagTemplateId;
            Tags = dto.Tags ?? Array.Empty<int>();
            Modifiers = CreateModifiers(dto.Modifiers);
        }

        private static IReadOnlyList<ContinuousModifierMO> CreateModifiers(ContinuousModifierDTO[] modifiers)
        {
            if (modifiers == null || modifiers.Length == 0) return Array.Empty<ContinuousModifierMO>();

            var list = new ContinuousModifierMO[modifiers.Length];
            for (int i = 0; i < modifiers.Length; i++)
            {
                list[i] = new ContinuousModifierMO(modifiers[i]);
            }

            return list;
        }
    }
}
