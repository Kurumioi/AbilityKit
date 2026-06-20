using System;
using System.Collections.Generic;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime.Config;
using AbilityKit.Triggering.Runtime.Plan;
using AbilityKit.Triggering.Variables.Numeric;

namespace AbilityKit.Triggering.Runtime.Executable
{
    [Obsolete("Runtime/Executable builders are removed compatibility surface. Use TriggerPlanBuilder and Runtime.Plan executables instead.")]
    public class ExecutableBuilder<T> where T : ISimpleExecutable, new()
    {
        protected readonly T _executable = new();

        public T Build() => _executable;
        public ISimpleExecutable ToExecutable() => _executable;
    }

    [Obsolete("SequenceBuilder is removed compatibility surface. Use TriggerPlanBuilder and Runtime.Plan executables instead.")]
    public sealed class SequenceBuilder : ExecutableBuilder<SequenceExecutable>
    {
        public SequenceBuilder Add(ISimpleExecutable child) { _executable.Add(child); return this; }
        public SequenceBuilder AddRange(params ISimpleExecutable[] children) { _executable.AddRange(children); return this; }
        public SequenceBuilder AddAction(ActionId actionId) { _executable.Add(new ActionCallExecutable { ActionId = actionId, Arity = 0 }); return this; }
        public SequenceBuilder AddAction(ActionId actionId, double arg0) { _executable.Add(new ActionCallExecutable { ActionId = actionId, Arg0 = NumericValueRef.Const(arg0), Arity = 1 }); return this; }
        public SequenceBuilder AddAction(ActionId actionId, double arg0, double arg1) { _executable.Add(new ActionCallExecutable { ActionId = actionId, Arg0 = NumericValueRef.Const(arg0), Arg1 = NumericValueRef.Const(arg1), Arity = 2 }); return this; }
        public SequenceBuilder AddAction(ActionId actionId, NumericValueRef arg0) { _executable.Add(new ActionCallExecutable { ActionId = actionId, Arg0 = arg0, Arity = 1 }); return this; }
        public SequenceBuilder AddAction(ActionId actionId, NumericValueRef arg0, NumericValueRef arg1) { _executable.Add(new ActionCallExecutable { ActionId = actionId, Arg0 = arg0, Arg1 = arg1, Arity = 2 }); return this; }
        public SequenceBuilder If(ICondition condition, ISimpleExecutable body) { _executable.Add(new IfExecutable { Condition = condition, Body = body }); return this; }
        public SequenceBuilder IfElse(ICondition condition, ISimpleExecutable thenBody, ISimpleExecutable elseBody) { _executable.Add(new IfElseExecutable().If(condition, thenBody).Else(elseBody)); return this; }
        public SequenceBuilder Delay(float delayMs) { _executable.Add(new DelayExecutable { DelayMs = delayMs }); return this; }
        public SequenceBuilder Log(string message) { _executable.Add(new DebugLogExecutable { Message = message }); return this; }
        public SequenceBuilder Event(string eventName) { _executable.Add(new EventSendExecutable { EventName = eventName }); return this; }
    }

    [Obsolete("SelectorBuilder is removed compatibility surface. Use TriggerPlanBuilder and Runtime.Plan executables instead.")]
    public sealed class SelectorBuilder : ExecutableBuilder<SelectorExecutable>
    {
        public SelectorBuilder Add(ISimpleExecutable child) { _executable.Add(child); return this; }
        public SelectorBuilder AddAction(ActionId actionId) { _executable.Add(new ActionCallExecutable { ActionId = actionId, Arity = 0 }); return this; }
    }

    [Obsolete("ParallelBuilder is removed compatibility surface. Use TriggerPlanBuilder and Runtime.Plan executables instead.")]
    public sealed class ParallelBuilder : ExecutableBuilder<ParallelExecutable>
    {
        public ParallelBuilder Add(ISimpleExecutable child) { _executable.Add(child); return this; }
        public ParallelBuilder SetMode(ECompositeMode mode) { _executable.ParallelMode = mode; return this; }
        public ParallelBuilder SetTimeout(float timeoutMs) { _executable.TimeoutMs = timeoutMs; return this; }
    }

    [Obsolete("IfElseBuilder is removed compatibility surface. Use TriggerPlan predicates and Runtime.Plan executables instead.")]
    public sealed class IfElseBuilder : ExecutableBuilder<IfElseExecutable>
    {
        public IfElseBuilder If(ICondition condition, ISimpleExecutable body) { _executable.If(condition, body); return this; }
        public IfElseBuilder ElseIf(ICondition condition, ISimpleExecutable body) { _executable.ElseIf(condition, body); return this; }
        public IfElseBuilder Else(ISimpleExecutable body) { _executable.Else(body); return this; }
    }

    [Obsolete("SwitchBuilder is removed compatibility surface. Use Runtime.Plan executables or registered action logic instead.")]
    public sealed class SwitchBuilder : ExecutableBuilder<SwitchExecutable>
    {
        public SwitchBuilder Selector(Func<object, int> selector) { _executable.ValueSelector = selector; return this; }
        public SwitchBuilder Case(int value, ISimpleExecutable body) { _executable.Case(value, body); return this; }
        public SwitchBuilder Case(int value, ActionId actionId) { _executable.Case(value, new ActionCallExecutable { ActionId = actionId, Arity = 0 }); return this; }
        public SwitchBuilder Case(int value, ActionId actionId, double arg0) { _executable.Case(value, new ActionCallExecutable { ActionId = actionId, Arg0 = NumericValueRef.Const(arg0), Arity = 1 }); return this; }
        public SwitchBuilder Default(ISimpleExecutable body) { _executable.Default(body); return this; }
    }

    [Obsolete("RandomSelectorBuilder is removed compatibility surface. Use Runtime.Plan executables or registered action logic instead.")]
    public sealed class RandomSelectorBuilder : ExecutableBuilder<RandomSelectorExecutable>
    {
        public RandomSelectorBuilder Add(ISimpleExecutable child, float weight = 1f)
        {
            _executable.Children.Add(child);
            return this;
        }

        public RandomSelectorBuilder SetWeights(params float[] weights)
        {
            _executable.Weights = weights;
            return this;
        }
    }

    [Obsolete("RepeatBuilder is removed compatibility surface. Use ActionScheduler or Runtime.Plan executables instead.")]
    public sealed class RepeatBuilder : ExecutableBuilder<RepeatExecutable>
    {
        public RepeatBuilder Count(int count) { _executable.Count = count; return this; }
        public RepeatBuilder Body(ISimpleExecutable body) { _executable.Child = body; return this; }
    }

    [Obsolete("UntilBuilder is removed compatibility surface. Use ActionScheduler or Runtime.Plan executables instead.")]
    public sealed class UntilBuilder : ExecutableBuilder<UntilExecutable>
    {
        public UntilBuilder Condition(ICondition condition) { return this; }
        public UntilBuilder Body(ISimpleExecutable body) { _executable.Child = body; return this; }
    }

    [Obsolete("ConditionBuilderExtensions is removed compatibility surface. Use TriggerPlan predicates or registered condition extensions on the formal runtime path.")]
    public static class ConditionBuilderExtensions
    {
        [Obsolete("PayloadCompare is removed compatibility surface. Use TriggerPlan payload predicates on the formal runtime path.")]
        public static ICondition PayloadCompare(int fieldId, ECompareOp op, NumericValueRef compareValue)
        {
            throw new NotSupportedException("Legacy Runtime/Executable condition builders are removed. Use TriggerPlan predicates on the formal runtime path.");
        }

        [Obsolete("PayloadCompare is removed compatibility surface. Use TriggerPlan payload predicates on the formal runtime path.")]
        public static ICondition PayloadCompare(int fieldId, ECompareOp op, double compareValue)
        {
            throw new NotSupportedException("Legacy Runtime/Executable condition builders are removed. Use TriggerPlan predicates on the formal runtime path.");
        }

        [Obsolete("HasTarget is removed compatibility surface. Use targeting package predicates instead.")]
        public static ICondition HasTarget(bool negate = false)
        {
            throw new NotSupportedException("Legacy Runtime/Executable target conditions are removed. Use targeting package predicates instead.");
        }
    }

    [Obsolete("ScheduledExecutableBuilderExtensions is removed compatibility surface. Use ActionScheduler formal scheduling instead.")]
    public static class ScheduledExecutableBuilderExtensions
    {
        [Obsolete("Schedule is removed compatibility surface. Use ActionScheduler formal scheduling instead.")]
        public static ISimpleExecutable Schedule(this ISimpleExecutable executable, int delayMs)
        {
            throw new NotSupportedException("Legacy Runtime/Executable scheduling is removed. Use ActionScheduler formal scheduling instead.");
        }
    }

    [Obsolete("Runtime/Executable is removed compatibility surface. Use AbilityKit.Triggering.Runtime.Plan.TriggerPlanExecutableDsl instead.")]
    public static class ExecutableDsl
    {
        public static SequenceBuilder Sequence() => throw new NotSupportedException("Runtime/Executable is removed. Use Runtime.Plan.TriggerPlanExecutableDsl.");
        public static SelectorBuilder Selector() => throw new NotSupportedException("Runtime/Executable is removed. Use Runtime.Plan.TriggerPlanExecutableDsl.");
        public static ParallelBuilder Parallel() => throw new NotSupportedException("Runtime/Executable is removed. Use Runtime.Plan.TriggerPlanExecutableDsl.");
        public static IfElseBuilder IfElse() => throw new NotSupportedException("Runtime/Executable is removed. Use Runtime.Plan.TriggerPlanExecutableDsl.");
        public static SwitchBuilder Switch() => throw new NotSupportedException("Runtime/Executable is removed. Use Runtime.Plan.TriggerPlanExecutableDsl.");
        public static RandomSelectorBuilder RandomSelector() => throw new NotSupportedException("Runtime/Executable is removed. Use Runtime.Plan.TriggerPlanExecutableDsl.");
        public static RepeatBuilder Repeat() => throw new NotSupportedException("Runtime/Executable is removed. Use Runtime.Plan.TriggerPlanExecutableDsl.");
        public static UntilBuilder Until() => throw new NotSupportedException("Runtime/Executable is removed. Use Runtime.Plan.TriggerPlanExecutableDsl.");
    }
}
