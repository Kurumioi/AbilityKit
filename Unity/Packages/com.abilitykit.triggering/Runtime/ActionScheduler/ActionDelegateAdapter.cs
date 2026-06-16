using System;
using AbilityKit.Triggering.Runtime.Dispatcher;
using AbilityKit.Triggering.Runtime.Executable;
using AbilityKit.Triggering.Runtime.Plan;
using AbilityKit.Triggering.Runtime.Context;
using AbilityKit.Triggering.Variables.Numeric;

namespace AbilityKit.Triggering.Runtime.ActionScheduler
{
    /// <summary>
    /// Action 委托兼容适配器。
    /// 当前不接入 PlannedTrigger 主线，仅保留强类型委托到 ActionScheduler 通用委托的旧适配入口。
    /// </summary>
    internal static class ActionDelegateAdapter
    {
        /// <summary>
        /// 创建通用 Action 委托
        /// </summary>
        public static Action<object, ITriggerDispatcherContext> Create<TArgs, TCtx>(
            Action0<TArgs, TCtx> action0,
            Action1<TArgs, TCtx> action1,
            Action2<TArgs, TCtx> action2,
            int arity,
            NumericValueRef arg0,
            NumericValueRef arg1)
            where TArgs : class
        {
            return (argsObj, dispatcherCtx) =>
            {
                var args = (TArgs)argsObj;
                var execCtx = CreateExecCtx<TArgs, TCtx>(dispatcherCtx);

                switch (arity)
                {
                    case 0:
                        action0?.Invoke(args, execCtx);
                        break;
                    case 1:
                        var v0 = arg0.Resolve(args);
                        var argsDict1 = new NamedArgsDict(new System.Collections.Generic.Dictionary<string, ActionArgValue>
                        {
                            ["_0"] = ActionArgValue.OfConst(v0, "_0")
                        });
                        action1?.Invoke(args, argsDict1, execCtx);
                        break;
                    case 2:
                        var v1 = arg0.Resolve(args);
                        var v2 = arg1.Resolve(args);
                        var argsDict2 = new NamedArgsDict(new System.Collections.Generic.Dictionary<string, ActionArgValue>
                        {
                            ["_0"] = ActionArgValue.OfConst(v1, "_0"),
                            ["_1"] = ActionArgValue.OfConst(v2, "_1")
                        });
                        action2?.Invoke(args, argsDict2, execCtx);
                        break;
                }
            };
        }

        /// <summary>
        /// 创建条件委托适配器
        /// </summary>
        public static TriggerPredicate<object> CreatePredicate<TArgs, TCtx>(
            Predicate0<TArgs, TCtx> predicate0,
            Predicate1<TArgs, TCtx> predicate1,
            Predicate2<TArgs, TCtx> predicate2,
            int arity,
            NumericValueRef arg0,
            NumericValueRef arg1)
            where TArgs : class
        {
            return (argsObj, dispatcherCtx) =>
            {
                var args = (TArgs)argsObj;
                var execCtx = CreateExecCtx<TArgs, TCtx>(dispatcherCtx);

                switch (arity)
                {
                    case 0:
                        return predicate0(args, execCtx);
                    case 1:
                        var v0 = arg0.Resolve(args);
                        var argsDict1 = new NamedArgsDict(new System.Collections.Generic.Dictionary<string, ActionArgValue>
                        {
                            ["_0"] = ActionArgValue.OfConst(v0, "_0")
                        });
                        return predicate1(args, argsDict1, execCtx);
                    case 2:
                        var v1 = arg0.Resolve(args);
                        var v2 = arg1.Resolve(args);
                        var argsDict2 = new NamedArgsDict(new System.Collections.Generic.Dictionary<string, ActionArgValue>
                        {
                            ["_0"] = ActionArgValue.OfConst(v1, "_0"),
                            ["_1"] = ActionArgValue.OfConst(v2, "_1")
                        });
                        return predicate2(args, argsDict2, execCtx);
                    default:
                        return false;
                }
            };
        }

        /// <summary>
        /// 从 ITriggerDispatcherContext 创建 ExecCtx。
        /// 当前兼容适配器无法从派发上下文还原完整 ExecCtx，禁止返回默认上下文伪装成功。
        /// </summary>
        private static ExecCtx<TCtx> CreateExecCtx<TArgs, TCtx>(ITriggerDispatcherContext dispatcherCtx)
            where TArgs : class
        {
            throw new NotSupportedException(
                "ActionDelegateAdapter 无法从 ITriggerDispatcherContext 构建完整 ExecCtx；请使用 PlannedTrigger 主线或传入已构建的 ExecCtx。");
        }
    }
}
