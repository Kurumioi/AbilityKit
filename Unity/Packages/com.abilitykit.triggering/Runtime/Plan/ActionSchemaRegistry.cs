using System;
using System.Collections.Generic;
using System.Linq;
using AbilityKit.Core.Logging;
using AbilityKit.Triggering.Blackboard;
using AbilityKit.Triggering.Eventing;
using AbilityKit.Triggering.Payload;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Variables.Numeric;
using AbilityKit.Triggering.Variables.Numeric.Expression;

namespace AbilityKit.Triggering.Runtime.Plan
{
    /// <summary>
    /// Action Schema 注册表
    /// 业务包启动时通过 PlanActionModule.Register() 注册各自的 Schema
    /// 运行时通过 ActionSchemaRegistry.ParseArgs(actionId, args, ctx) 将字典解析为强类型 struct
    /// </summary>
    public static class ActionSchemaRegistry
    {
        // 存储泛型 schema（内部）
        private static readonly Dictionary<ActionId, IActionSchema> _schemas = new Dictionary<ActionId, IActionSchema>();
        private static bool _frozen;

        /// <summary>
        /// 注册一个 Action Schema（通常由 PlanActionModule 在 Register() 中调用）
        /// </summary>
        /// <remarks>
        /// 注册发生在启动阶段（冻结前），支持重复注册（后面的覆盖前面的）。
        /// 运行时严禁修改。
        /// </remarks>
        public static void Register(IActionSchema schema)
        {
            if (schema == null) return;

            if (_frozen)
            {
                Log.Warning($"[ActionSchemaRegistry] Register called after freeze. Schema={schema.ActionId.Value}. This should not happen at runtime.");
                return;
            }

            _schemas[schema.ActionId] = schema;
        }

        /// <summary>
        /// 注册一个泛型 Action Schema
        /// </summary>
        public static void Register<TActionArgs, TCtx>(IActionSchema<TActionArgs, TCtx> schema)
        {
            Register(new GenericSchemaAdapter<TActionArgs, TCtx>(schema));
        }

        /// <summary>
        /// 冻结注册表，阻止后续注册（启动完成后调用）
        /// </summary>
        public static void Freeze()
        {
            _frozen = true;
        }

        /// <summary>
        /// 重置注册表（仅用于单元测试）
        /// </summary>
        internal static void Reset()
        {
            _schemas.Clear();
            _frozen = false;
        }

        /// <summary>
        /// 尝试获取指定 ActionId 的 Schema
        /// </summary>
        public static bool TryGet(ActionId id, out IActionSchema schema)
        {
            return _schemas.TryGetValue(id, out schema);
        }

        /// <summary>
        /// 尝试获取指定 ActionId 的泛型 Schema
        /// </summary>
        public static bool TryGet<TActionArgs, TCtx>(ActionId id, out IActionSchema<TActionArgs, TCtx> schema)
        {
            if (_schemas.TryGetValue(id, out var s) && s is GenericSchemaAdapter<TActionArgs, TCtx> adapter)
            {
                schema = adapter.Inner;
                return true;
            }
            schema = default;
            return false;
        }

        /// <summary>
        /// 通过 ActionId 获取已解析的强类型参数（内部用）
        /// </summary>
        internal static object GetParsedArgs<TActionArgs, TCtx>(ActionId id, Dictionary<string, ActionArgValue> namedArgs, ExecCtx<TCtx> ctx)
        {
            if (namedArgs == null || namedArgs.Count == 0)
                return null;

            if (_schemas.TryGetValue(id, out var s) && s is GenericSchemaAdapter<TActionArgs, TCtx> adapter)
            {
                // 验证
                var span = new ReadOnlySpan<KeyValuePair<string, ActionArgValue>>(namedArgs.ToArray());
                if (!adapter.Inner.TryValidateArgs(span, out var error))
                {
                    Log.Warning($"[ActionSchemaRegistry] Args validation failed for action={id.Value}: {error}");
                }
                return adapter.Inner.ParseArgs(namedArgs, ctx);
            }
            return null;
        }

        /// <summary>
        /// 将具名参数字典解析为强类型参数结构体
        /// 如果 Action 没有注册 Schema（向后兼容），则返回 null
        /// </summary>
        [Obsolete("Use the generic ExecCtx<TCtx> ParseArgs overload on the formal runtime path.")]
        public static object ParseArgs(ActionId id, Dictionary<string, ActionArgValue> namedArgs, object ctx)
        {
            if (namedArgs == null || namedArgs.Count == 0)
                return null;

            if (!_schemas.TryGetValue(id, out var schema))
                throw new InvalidOperationException($"Action schema is not registered: {id.Value}");

            // 验证（可选）
            var span = new ReadOnlySpan<KeyValuePair<string, ActionArgValue>>(namedArgs.ToArray());
            if (!schema.TryValidateArgs(span, out var error))
            {
                Log.Warning($"[ActionSchemaRegistry] Args validation failed for action={id.Value}: {error}");
            }

            return schema.ParseArgs(namedArgs, ctx);
        }

        /// <summary>
        /// 将具名参数字典解析为强类型参数结构体（泛型版本）
        /// </summary>
        public static TActionArgs ParseArgs<TActionArgs, TCtx>(ActionId id, Dictionary<string, ActionArgValue> namedArgs, ExecCtx<TCtx> ctx)
        {
            if (namedArgs == null || namedArgs.Count == 0)
                return default;

            var span = new ReadOnlySpan<KeyValuePair<string, ActionArgValue>>(namedArgs.ToArray());

            if (_schemas.TryGetValue(id, out var s) && s is GenericSchemaAdapter<TActionArgs, TCtx> adapter)
            {
                if (!adapter.Inner.TryValidateArgs(span, out var error))
                {
                    Log.Warning($"[ActionSchemaRegistry] Args validation failed for action={id.Value}: {error}");
                }
                return adapter.Inner.ParseArgs(namedArgs, ctx);
            }

            return default;
        }

        /// <summary>
        /// 尝试解析单个 NumericValueRef 为 double 值
        /// 通用于所有 Schema 实现中的值解析
        /// </summary>
        public static bool TryResolveNumericRef<TArgs, TCtx>(in NumericValueRef valueRef, in TArgs args, in ExecCtx<TCtx> ctx, out double value)
        {
            value = 0.0;

            if (valueRef.Kind == ENumericValueRefKind.Const)
            {
                value = valueRef.ConstValue;
                return true;
            }

            if (valueRef.Kind == ENumericValueRefKind.Blackboard)
                return TryResolveBlackboard(in valueRef, in ctx, out value);

            if (valueRef.Kind == ENumericValueRefKind.PayloadField)
                return TryResolvePayloadField(in valueRef, in args, in ctx, out value);

            if (valueRef.Kind == ENumericValueRefKind.Var)
                return TryResolveNumericVar(in valueRef, in ctx, out value);

            if (valueRef.Kind == ENumericValueRefKind.Expr)
                return TryResolveExpr(in valueRef, in ctx, out value);

            return false;
        }

        /// <summary>
        /// 解析单个 NumericValueRef 为 double 值
        /// 通用于所有 Schema 实现中的值解析
        /// </summary>
        public static double ResolveNumericRef<TArgs, TCtx>(in NumericValueRef valueRef, in TArgs args, in ExecCtx<TCtx> ctx)
        {
            if (TryResolveNumericRef(in valueRef, in args, in ctx, out var value))
                return value;

            throw new InvalidOperationException("Failed to resolve numeric value reference: " + DescribeNumericValueRef(in valueRef));
        }

        /// <summary>
        /// 尝试解析单个 NumericValueRef 为 double 值。
        /// 兼容旧 object 上下文；正式路径应优先使用泛型重载。
        /// </summary>
        [Obsolete("Use the generic ExecCtx<TCtx> TryResolveNumericRef overload on the formal runtime path.")]
        public static bool TryResolveNumericRef(in NumericValueRef valueRef, object ctx, out double value)
        {
            value = 0.0;

            if (valueRef.Kind == ENumericValueRefKind.Const)
            {
                value = valueRef.ConstValue;
                return true;
            }

            if (ctx == null)
                return false;

            if (valueRef.Kind == ENumericValueRefKind.Blackboard)
            {
                var resolver = TryGetBlackboardResolver(ctx);
                if (resolver == null) return false;
                if (!resolver.TryResolve(valueRef.BoardId, out var bb) || bb == null) return false;
                return bb.TryGetDouble(valueRef.KeyId, out value);
            }

            if (valueRef.Kind == ENumericValueRefKind.PayloadField)
            {
                var payloads = TryGetPayloadAccessorRegistry(ctx);
                return payloads != null && TryResolvePayloadFieldReflectively(payloads, valueRef.FieldId, ctx, out value);
            }

            if (valueRef.Kind == ENumericValueRefKind.Var)
            {
                return TryResolveNumericVar(ctx, valueRef.DomainId, valueRef.Key, out value);
            }

            if (valueRef.Kind == ENumericValueRefKind.Expr)
            {
                return TryResolveExprReflectively(ctx, valueRef.ExprText, out value);
            }

            return false;
        }

        /// <summary>
        /// 解析单个 NumericValueRef 为 double 值。
        /// 兼容旧 object 上下文；正式路径应优先使用泛型重载。
        /// </summary>
        [Obsolete("Use the generic ExecCtx<TCtx> ResolveNumericRef overload on the formal runtime path.")]
        public static double ResolveNumericRef(in NumericValueRef valueRef, object ctx)
        {
            if (TryResolveNumericRef(in valueRef, ctx, out var value))
                return value;

            throw new InvalidOperationException("Failed to resolve numeric value reference from object context: " + DescribeNumericValueRef(in valueRef));
        }

        private static bool TryResolveBlackboard<TCtx>(in NumericValueRef valueRef, in ExecCtx<TCtx> ctx, out double value)
        {
            value = 0.0;
            var resolver = ctx.Blackboards;
            if (resolver == null) return false;
            if (!resolver.TryResolve(valueRef.BoardId, out var board) || board == null) return false;
            return board.TryGetDouble(valueRef.KeyId, out value);
        }

        private static bool TryResolvePayloadField<TArgs, TCtx>(in NumericValueRef valueRef, in TArgs args, in ExecCtx<TCtx> ctx, out double value)
        {
            value = 0.0;
            var payloads = ctx.Payloads;
            return payloads != null && payloads.TryGetDouble(in args, valueRef.FieldId, out value);
        }

        private static bool TryResolveNumericVar<TCtx>(in NumericValueRef valueRef, in ExecCtx<TCtx> ctx, out double value)
        {
            value = 0.0;
            if (string.IsNullOrEmpty(valueRef.DomainId) || string.IsNullOrEmpty(valueRef.Key))
                return false;
            return ctx.TryGetNumericVar(valueRef.DomainId, valueRef.Key, out value);
        }

        private static bool TryResolveExpr<TCtx>(in NumericValueRef valueRef, in ExecCtx<TCtx> ctx, out double value)
        {
            value = 0.0;
            if (string.IsNullOrEmpty(valueRef.ExprText)) return false;
            if (!NumericExpressionCompiler.TryCompileCached(valueRef.ExprText, out var program) || program == null)
                return false;

            return NumericExpressionEvaluator.TryEvaluate(in ctx, program, out value);
        }

        private static IBlackboardResolver TryGetBlackboardResolver(object ctx)
        {
            var prop = ctx.GetType().GetProperty("Blackboards");
            return prop?.GetValue(ctx) as IBlackboardResolver;
        }

        private static IPayloadAccessorRegistry TryGetPayloadAccessorRegistry(object ctx)
        {
            var prop = ctx.GetType().GetProperty("Payloads");
            return prop?.GetValue(ctx) as IPayloadAccessorRegistry;
        }

        private static bool TryResolvePayloadFieldReflectively(IPayloadAccessorRegistry registry, int fieldId, object ctx, out double value)
        {
            value = 0.0;
            var method = typeof(IPayloadAccessorRegistry)
                .GetMethod("TryGetDouble")
                ?.MakeGenericMethod(ctx.GetType());
            if (method == null)
                return false;

            var parameters = new object[] { ctx, fieldId, 0.0 };
            var result = method.Invoke(registry, parameters);
            if (result is bool success && success)
            {
                value = (double)parameters[2];
                return true;
            }

            return false;
        }

        private static bool TryResolveExprReflectively(object ctx, string exprText, out double value)
        {
            value = 0.0;
            if (string.IsNullOrEmpty(exprText)) return false;
            if (!NumericExpressionCompiler.TryCompileCached(exprText, out var program) || program == null)
                return false;

            return NumericRpnTokenEvaluator.TryEvaluate(
                program,
                (string domainId, string key, out double resolved) => TryResolveNumericVar(ctx, domainId, key, out resolved),
                TryGetNumericFunctionRegistry(ctx),
                out value);
        }

        private static string DescribeNumericValueRef(in NumericValueRef valueRef)
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

        private static bool TryResolveNumericVar(object ctx, string domainId, string key, out double value)
        {
            value = 0.0;
            if (ctx == null || string.IsNullOrEmpty(domainId) || string.IsNullOrEmpty(key))
                return false;

            var ctxType = ctx.GetType();
            if (!ctxType.IsGenericType || ctxType.GetGenericTypeDefinition() != typeof(ExecCtx<>))
                return false;

            var registryField = ctxType.GetField(nameof(ExecCtx<object>.NumericDomains));
            var registry = registryField?.GetValue(ctx) as INumericVarDomainRegistry ?? DefaultNumericVarDomainRegistry.Instance;
            if (registry == null || !registry.TryGetDomain(domainId, out var domain) || domain == null)
                return false;

            var tryGetMethod = typeof(INumericVarDomain).GetMethod(nameof(INumericVarDomain.TryGet));
            if (tryGetMethod == null)
                return false;

            var genericMethod = tryGetMethod.MakeGenericMethod(ctxType.GetGenericArguments()[0]);
            var parameters = new object[] { ctx, key, 0.0 };
            var result = genericMethod.Invoke(domain, parameters);
            if (result is bool success && success)
            {
                value = (double)parameters[2];
                return true;
            }

            return false;
        }

        private static INumericRpnFunctionRegistry TryGetNumericFunctionRegistry(object ctx)
        {
            var prop = ctx?.GetType().GetProperty("NumericFunctions");
            return prop?.GetValue(ctx) as INumericRpnFunctionRegistry;
        }

        /// <summary>
        /// 泛型 Schema 的适配器，将 IActionSchema<TActionArgs, TCtx> 适配为 IActionSchema
        /// </summary>
        private sealed class GenericSchemaAdapter<TActionArgs, TCtx> : IActionSchema
        {
            public readonly IActionSchema<TActionArgs, TCtx> Inner;

            public GenericSchemaAdapter(IActionSchema<TActionArgs, TCtx> inner)
            {
                Inner = inner;
            }

            public ActionId ActionId => Inner.ActionId;
            public Type ArgsType => Inner.ArgsType;

        public object ParseArgs(Dictionary<string, ActionArgValue> namedArgs, object ctx)
        {
            var typedCtx = ctx is ExecCtx<TCtx> e ? e : default;
            return Inner.ParseArgs(namedArgs, typedCtx);
        }

        public bool TryValidateArgs(ReadOnlySpan<KeyValuePair<string, ActionArgValue>> args, out string error)
        {
            return Inner.TryValidateArgs(args, out error);
        }
    }
    }
}
