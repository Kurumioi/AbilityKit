using System;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Variables.Numeric;

namespace AbilityKit.Triggering.Runtime.Plan
{
    /// <summary>
    /// Formal NumericValueRef resolver for plan/action schema code using ExecCtx<TCtx>.
    /// </summary>
    public static class NumericValueRefResolver
    {
        public static bool TryResolve<TArgs, TCtx>(in NumericValueRef valueRef, in TArgs args, in ExecCtx<TCtx> ctx, out double value)
        {
            return ActionSchemaRegistry.TryResolveNumericRef(in valueRef, in args, in ctx, out value);
        }

        public static double Resolve<TArgs, TCtx>(in NumericValueRef valueRef, in TArgs args, in ExecCtx<TCtx> ctx)
        {
            if (TryResolve(in valueRef, in args, in ctx, out var value))
                return value;

            throw new InvalidOperationException("Failed to resolve numeric value reference.");
        }
    }
}
