using System;
using AbilityKit.Triggering.Runtime.Abstractions;
using AbilityKit.Triggering.Runtime.Context;
using AbilityKit.Triggering.Runtime.Plan;
using AbilityKit.Triggering.Variables.Numeric.Expression;

namespace AbilityKit.Triggering.Variables.Numeric
{
    /// <summary>
    /// NumericValueRef 针对 ActionContext 的解析扩展
    /// 提供 Resolve(ActionContext) 方法，替代原有的 Resolve(object ctx)
    /// </summary>
    public static class NumericValueRefContextExtensions
    {
        /// <summary>
        /// 在 ActionContext 中解析数值引用
        /// </summary>
        public static double Resolve(this in NumericValueRef valueRef, ActionContext context)
        {
            if (TryResolve(in valueRef, context, out var value))
                return value;

            throw new InvalidOperationException("Failed to resolve numeric value reference from ActionContext: " + Describe(in valueRef));
        }

        public static bool TryResolve(this in NumericValueRef valueRef, ActionContext context, out double value)
        {
            value = 0.0;
            if (context == null) throw new ArgumentNullException(nameof(context));

            return valueRef.Kind switch
            {
                ENumericValueRefKind.Const => TryResolveConst(in valueRef, out value),
                ENumericValueRefKind.Blackboard => TryResolveBlackboard(in valueRef, context, out value),
                ENumericValueRefKind.PayloadField => TryResolvePayloadField(in valueRef, context, out value),
                ENumericValueRefKind.Var => TryResolveVar(in valueRef, context, out value),
                ENumericValueRefKind.Expr => TryResolveExpr(in valueRef, context, out value),
                _ => false
            };
        }

        private static bool TryResolveConst(in NumericValueRef valueRef, out double value)
        {
            value = valueRef.ConstValue;
            return true;
        }

        private static bool TryResolveBlackboard(in NumericValueRef valueRef, ActionContext context, out double value)
        {
            value = 0.0;
            var resolver = context.Blackboard;
            if (resolver == null)
                return false;

            return resolver.TryResolve(valueRef.BoardId, out var board) &&
                   board != null &&
                   board.TryGetDouble(valueRef.KeyId, out value);
        }

        private static bool TryResolvePayloadField(in NumericValueRef valueRef, ActionContext context, out double value)
        {
            value = 0.0;
            var accessor = context.Payloads;
            if (accessor == null)
                return false;

            var payloadService = context.GetService<IPayloadAccessor>();
            var payloadArgs = payloadService?.Target;
            if (payloadArgs == null)
                return false;

            object args = payloadArgs;
            return accessor.TryGetPayloadDouble(in args, valueRef.FieldId, out value);
        }

        private static bool TryResolveVar(in NumericValueRef valueRef, ActionContext context, out double value)
        {
            return TryResolveActionContextVar(context, valueRef.DomainId, valueRef.Key, out value);
        }

        private static bool TryResolveExpr(in NumericValueRef valueRef, ActionContext context, out double value)
        {
            value = 0.0;
            if (string.IsNullOrEmpty(valueRef.ExprText))
                return false;

            if (!NumericExpressionCompiler.TryCompileCached(valueRef.ExprText, out var program) || program == null)
                return false;

            return NumericRpnTokenEvaluator.TryEvaluate(
                program,
                (string domainId, string key, out double resolved) => TryResolveActionContextVar(context, domainId, key, out resolved),
                DefaultNumericRpnFunctionRegistry.Instance,
                out value);
        }

        private static bool TryResolveActionContextVar(ActionContext context, string domainId, string key, out double value)
        {
            value = 0.0;
            if (context?.Variables == null || string.IsNullOrEmpty(domainId) || string.IsNullOrEmpty(key))
                return false;

            value = context.Variables.GetNumeric(domainId, key);
            return true;
        }

        private static string Describe(in NumericValueRef valueRef)
        {
            return valueRef.Kind switch
            {
                ENumericValueRefKind.Const => $"Const({valueRef.ConstValue})",
                ENumericValueRefKind.Blackboard => $"Blackboard(boardId={valueRef.BoardId}, keyId={valueRef.KeyId})",
                ENumericValueRefKind.PayloadField => $"PayloadField(fieldId={valueRef.FieldId})",
                ENumericValueRefKind.Var => $"Var(domainId='{valueRef.DomainId}', key='{valueRef.Key}')",
                ENumericValueRefKind.Expr => "Expr(" + valueRef.ExprText + ")",
                _ => "Unsupported(" + valueRef.Kind + ")"
            };
        }
    }
}
