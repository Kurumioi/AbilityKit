using System;
using System.Collections.Generic;
using AbilityKit.Core.Continuous;
using AbilityKit.Modifiers;

namespace AbilityKit.Demo.Moba.Services
{
    public readonly struct MobaContinuousModifierEntry
    {
        public MobaContinuousModifierEntry(IContinuous continuous, IMobaContinuousProjectionConfig projection, IMobaContinuousModifierSpec spec, int stack)
        {
            Continuous = continuous;
            Projection = projection;
            Spec = spec;
            Stack = stack < 1 ? 1 : stack;
        }

        public IContinuous Continuous { get; }
        public IMobaContinuousProjectionConfig Projection { get; }
        public IMobaContinuousModifierSpec Spec { get; }
        public int Stack { get; }
    }

    public static class MobaContinuousModifierMath
    {
        public static float EvaluateNumeric(float baseValue, IReadOnlyList<MobaContinuousModifierEntry> entries)
        {
            if (entries == null || entries.Count == 0) return baseValue;

            var sorted = new List<MobaContinuousModifierEntry>(entries);
            sorted.Sort(ComparePriority);

            var value = baseValue;
            for (int i = 0; i < sorted.Count; i++)
            {
                var entry = sorted[i];
                var spec = entry.Spec;
                if (spec == null) continue;

                var amount = spec.Value * entry.Stack;
                var op = ToModifierOp(spec.Op);
                if (op == ModifierOp.Mul)
                {
                    value *= amount;
                }
                else if (op == ModifierOp.Override)
                {
                    value = amount;
                }
                else if (op == ModifierOp.PercentAdd)
                {
                    value += baseValue * amount;
                }
                else
                {
                    value += amount;
                }
            }

            return value;
        }

        public static ModifierOp ToModifierOp(int op)
        {
            if (op == (int)ModifierOp.Mul) return ModifierOp.Mul;
            if (op == (int)ModifierOp.Override) return ModifierOp.Override;
            if (op == (int)ModifierOp.PercentAdd) return ModifierOp.PercentAdd;
            return ModifierOp.Add;
        }

        private static int ComparePriority(MobaContinuousModifierEntry x, MobaContinuousModifierEntry y)
        {
            var xp = x.Spec?.Priority ?? 0;
            var yp = y.Spec?.Priority ?? 0;
            return xp.CompareTo(yp);
        }
    }
}
