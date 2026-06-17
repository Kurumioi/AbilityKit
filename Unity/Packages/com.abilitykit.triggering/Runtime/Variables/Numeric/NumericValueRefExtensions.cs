using System;
using AbilityKit.Triggering.Runtime.Plan;
using AbilityKit.Triggering.Variables.Numeric.Expression;

namespace AbilityKit.Triggering.Variables.Numeric
{
    /// <summary>
    /// NumericValueRef 解析扩展方法
    /// </summary>
    public static class NumericValueRefExtensions
    {
        /// <summary>
        /// 解析 NumericValueRef 为 double 值
        /// </summary>
        public static double Resolve(this in NumericValueRef ref_, object ctx)
        {
            if (TryResolve(in ref_, ctx, out var value))
                return value;

            throw new InvalidOperationException("Failed to resolve numeric value reference: " + Describe(in ref_));
        }

        public static bool TryResolve(this in NumericValueRef ref_, object ctx, out double value)
        {
            value = 0.0;
            return ref_.Kind switch
            {
                ENumericValueRefKind.Const => TryResolveConst(in ref_, out value),
                ENumericValueRefKind.Blackboard => TryResolveBlackboard(in ref_, ctx, out value),
                ENumericValueRefKind.PayloadField => TryResolvePayloadField(in ref_, ctx, out value),
                ENumericValueRefKind.Var => TryResolveVar(in ref_, ctx, out value),
                ENumericValueRefKind.Expr => TryResolveExpr(in ref_, ctx, out value),
                _ => false
            };
        }

        private static bool TryResolveConst(in NumericValueRef ref_, out double value)
        {
            value = ref_.ConstValue;
            return true;
        }

        private static bool TryResolveBlackboard(in NumericValueRef ref_, object ctx, out double value)
        {
            value = 0.0;
            return ctx is IBlackboardResolvable resolvable &&
                   resolvable.TryResolveBlackboardValue(ref_.BoardId, ref_.KeyId, out value);
        }

        private static bool TryResolvePayloadField(in NumericValueRef ref_, object ctx, out double value)
        {
            value = 0.0;
            return ctx is Runtime.Executable.IHasPayload payload &&
                   payload.TryGetPayloadDouble(ref_.FieldId, out value);
        }

        private static bool TryResolveVar(in NumericValueRef ref_, object ctx, out double value)
        {
            return TryResolveVar(ctx, ref_.DomainId, ref_.Key, out value);
        }

        private static bool TryResolveExpr(in NumericValueRef ref_, object ctx, out double value)
        {
            value = 0.0;
            if (string.IsNullOrEmpty(ref_.ExprText))
                return false;

            if (!NumericExpressionCompiler.TryCompileCached(ref_.ExprText, out var program) || program == null)
                return false;

            return NumericRpnTokenEvaluator.TryEvaluate(
                program,
                (string domainId, string key, out double resolved) => TryResolveVar(ctx, domainId, key, out resolved),
                DefaultNumericRpnFunctionRegistry.Instance,
                out value);
        }

        private static bool TryResolveVar(object ctx, string domainId, string key, out double value)
        {
            value = 0.0;
            if (string.IsNullOrEmpty(domainId) || string.IsNullOrEmpty(key))
                return false;

            return ctx is IVarResolvable varResolvable &&
                   varResolvable.TryResolveVarValue(domainId, key, out value);
        }

        private static string Describe(in NumericValueRef ref_)
        {
            return ref_.Kind switch
            {
                ENumericValueRefKind.Const => $"Const({ref_.ConstValue})",
                ENumericValueRefKind.Blackboard => $"Blackboard(boardId={ref_.BoardId}, keyId={ref_.KeyId})",
                ENumericValueRefKind.PayloadField => $"PayloadField(fieldId={ref_.FieldId})",
                ENumericValueRefKind.Var => $"Var(domainId='{ref_.DomainId}', key='{ref_.Key}')",
                ENumericValueRefKind.Expr => "Expr(" + ref_.ExprText + ")",
                _ => "Unsupported(" + ref_.Kind + ")"
            };
        }
    }

    /// <summary>
    /// 支持黑板值解析的上下文接口
    /// </summary>
    public interface IBlackboardResolvable
    {
        bool TryResolveBlackboardValue(int boardId, int keyId, out double value);
    }

    /// <summary>
    /// 支持变量值解析的上下文接口
    /// </summary>
    public interface IVarResolvable
    {
        bool TryResolveVarValue(string domainId, string key, out double value);
    }
}
