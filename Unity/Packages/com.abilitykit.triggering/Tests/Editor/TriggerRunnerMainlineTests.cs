using System;
using System.Collections.Generic;
using AbilityKit.Core.Eventing;
using AbilityKit.Triggering.Eventing;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.ActionScheduler;
using AbilityKit.Triggering.Runtime.Config;
using AbilityKit.Triggering.Runtime.Dispatcher;
using AbilityKit.Triggering.Runtime.Plan;
using AbilityKit.Triggering.Runtime.RuleScheduler;
using AbilityKit.Triggering.Runtime.Schedule;
using AbilityKit.Triggering.Runtime.Scheduler;
using AbilityKit.Triggering.Runtime.Schedule.Behavior;
using AbilityKit.Triggering.Runtime.Schedule.Data;
using AbilityKit.Triggering.Validation;
using NUnit.Framework;

namespace AbilityKit.Triggering.Tests
{
    public sealed class TriggerRunnerMainlineTests
    {
        [Test]
        public void Register_NotifiesLifecycleForEveryTriggerOnSameKey()
        {
            var bus = new EventBus();
            var lifecycle = new RecordingLifecycle<TestContext>();
            var runner = new TriggerRunner<TestContext>(
                bus,
                new FunctionRegistry(),
                new ActionRegistry(),
                lifecycle: lifecycle);
            var key = new EventKey<Ping>(StableStringId.Get("test:trigger_runner:registered_each_trigger"));

            runner.Register(key, new DelegateTrigger<Ping, TestContext>((evt, ctx) => true, (evt, ctx) => { }));
            runner.Register(key, new DelegateTrigger<Ping, TestContext>((evt, ctx) => true, (evt, ctx) => { }));

            Assert.That(lifecycle.RegisteredOrders, Is.EqualTo(new long[] { 0, 1 }));
        }

        [Test]
        public void PlannedTrigger_ImmediateNamedAction_PassesArgsAndContext()
        {
            var bus = new EventBus();
            var actions = new ActionRegistry();
            var contextSource = new MutableContextSource { Current = new TestContext(7) };
            var observedContextValues = new List<int>();
            NamedArgsDict observedArgs = null;
            var actionId = new ActionId(StableStringId.Get("test:trigger_runner:immediate_named_action"));

            actions.Register<NamedAction1<Ping, object, TestContext>>(
                actionId,
                (triggerArgs, actionArgs, ctx) =>
                {
                    observedContextValues.Add(ctx.Context.Value);
                    observedArgs = (NamedArgsDict)actionArgs;
                },
                isDeterministic: true);

            var runner = new TriggerRunner<TestContext>(
                bus,
                new FunctionRegistry(),
                actions,
                contextSource: contextSource);

            var key = new EventKey<Ping>(StableStringId.Get("test:trigger_runner:immediate_named_action_event"));
            var actionArgs = new Dictionary<string, ActionArgValue>
            {
                ["amount"] = ActionArgValue.OfConst(12, "amount")
            };
            var call = ActionCallPlan.WithArgs(actionId, actionArgs);
            var plan = new TriggerPlan<Ping>(phase: 0, priority: 0, triggerId: 1000, actions: new[] { call });

            runner.RegisterPlan<Ping, TestContext>(key, in plan);
            bus.Publish(key, new Ping());

            Assert.That(observedContextValues, Is.EqualTo(new[] { 7 }));
            Assert.That(observedArgs, Is.Not.Null);
            Assert.That(observedArgs.TryGetValue("amount", out var amount), Is.True);
            Assert.That(amount.Ref.ConstValue, Is.EqualTo(12));
        }

        [Test]
        public void PlannedTrigger_ImmediatePositionalAction_PassesResolvedArgs()
        {
            var bus = new EventBus();
            var actions = new ActionRegistry();
            var contextSource = new MutableContextSource { Current = new TestContext(5) };
            NamedArgsDict observedArgs = null;
            var actionId = new ActionId(StableStringId.Get("test:trigger_runner:immediate_positional_action"));

            actions.Register<NamedAction1<Ping, object, TestContext>>(
                actionId,
                (triggerArgs, actionArgs, ctx) => observedArgs = (NamedArgsDict)actionArgs,
                isDeterministic: true);

            var runner = new TriggerRunner<TestContext>(
                bus,
                new FunctionRegistry(),
                actions,
                contextSource: contextSource);

            var key = new EventKey<Ping>(StableStringId.Get("test:trigger_runner:immediate_positional_action_event"));
            var call = new ActionCallPlan(
                actionId,
                1,
                NumericValueRef.Const(12),
                default,
                null,
                EActionScheduleMode.Immediate,
                0f,
                -1,
                true,
                EActionExecutionPolicy.Immediate);
            var plan = new TriggerPlan<Ping>(phase: 0, priority: 0, triggerId: 1001, actions: new[] { call });

            runner.RegisterPlan<Ping, TestContext>(key, in plan);
            bus.Publish(key, new Ping());

            Assert.That(observedArgs, Is.Not.Null);
            Assert.That(observedArgs.TryGetValue("_0", out var value), Is.True);
            Assert.That(value.Ref.ConstValue, Is.EqualTo(12));
        }

        [Test]
        public void ScheduledPlannedTrigger_UsesLatestExecContextAfterRepeatedDispatch()
        {
            var bus = new EventBus();
            var actions = new ActionRegistry();
            var schedulerManager = new ActionSchedulerManager();
            var contextSource = new MutableContextSource();
            var observedContextValues = new List<int>();
            var actionId = new ActionId(StableStringId.Get("test:trigger_runner:scheduled_context"));

            actions.Register<NamedAction0<Ping, object, TestContext>>(
                actionId,
                (triggerArgs, actionArgs, ctx) => observedContextValues.Add(ctx.Context.Value),
                isDeterministic: true);

            var runner = new TriggerRunner<TestContext>(
                bus,
                new FunctionRegistry(),
                actions,
                contextSource: contextSource,
                actionSchedulerManager: schedulerManager);

            var key = new EventKey<Ping>(StableStringId.Get("test:trigger_runner:scheduled_context_event"));
            var call = new ActionCallPlan(
                actionId,
                0,
                default,
                default,
                null,
                EActionScheduleMode.Delayed,
                1f,
                1,
                true,
                EActionExecutionPolicy.Immediate);
            var plan = new TriggerPlan<Ping>(phase: 0, priority: 0, triggerId: 1001, actions: new[] { call });

            runner.RegisterPlan<Ping, TestContext>(key, in plan);

            contextSource.Current = new TestContext(1);
            bus.Publish(key, new Ping());

            contextSource.Current = new TestContext(2);
            bus.Publish(key, new Ping());

            schedulerManager.Update(1f, new TestDispatcherContext(contextSource.Current));

            Assert.That(observedContextValues, Is.EqualTo(new[] { 2 }));
        }

        [Test]
        public void ScheduledPlannedTrigger_ImmediateAction_UsesLatestContextAtExecutionTime()
        {
            var bus = new EventBus();
            var actions = new ActionRegistry();
            var schedulerManager = new ActionSchedulerManager();
            var contextSource = new MutableContextSource { Current = new TestContext(1) };
            var observedContextValues = new List<int>();
            var actionId = new ActionId(StableStringId.Get("test:trigger_runner:immediate_latest_context"));

            actions.Register<NamedAction0<Ping, object, TestContext>>(
                actionId,
                (triggerArgs, actionArgs, ctx) => observedContextValues.Add(ctx.Context.Value),
                isDeterministic: true);

            var runner = new TriggerRunner<TestContext>(
                bus,
                new FunctionRegistry(),
                actions,
                contextSource: contextSource,
                actionSchedulerManager: schedulerManager);

            var key = new EventKey<Ping>(StableStringId.Get("test:trigger_runner:immediate_latest_context_event"));
            var call = new ActionCallPlan(
                actionId,
                0,
                default,
                default,
                null,
                EActionScheduleMode.Immediate,
                0f,
                -1,
                true,
                EActionExecutionPolicy.Immediate);
            var plan = new TriggerPlan<Ping>(phase: 0, priority: 0, triggerId: 1003, actions: new[] { call });

            runner.RegisterPlan<Ping, TestContext>(key, in plan);
            contextSource.Current = new TestContext(7);
            bus.Publish(key, new Ping());

            Assert.That(observedContextValues, Is.EqualTo(new[] { 7 }));
        }

        [Test]
        public void ScheduledPlannedTrigger_PeriodicAction_UsesScheduleSubPlanForIntervalsAndMaxExecutions()
        {
            var bus = new EventBus();
            var actions = new ActionRegistry();
            var schedulerManager = new ActionSchedulerManager();
            var contextSource = new MutableContextSource { Current = new TestContext(3) };
            var observedContextValues = new List<int>();
            var actionId = new ActionId(StableStringId.Get("test:trigger_runner:periodic_schedule_sub_plan"));

            actions.Register<NamedAction0<Ping, object, TestContext>>(
                actionId,
                (triggerArgs, actionArgs, ctx) => observedContextValues.Add(ctx.Context.Value),
                isDeterministic: true);

            var runner = new TriggerRunner<TestContext>(
                bus,
                new FunctionRegistry(),
                actions,
                contextSource: contextSource,
                actionSchedulerManager: schedulerManager);

            var key = new EventKey<Ping>(StableStringId.Get("test:trigger_runner:periodic_schedule_sub_plan_event"));
            var call = new ActionCallPlan(
                actionId,
                0,
                default,
                default,
                null,
                EActionScheduleMode.Periodic,
                10f,
                2,
                true,
                EActionExecutionPolicy.Immediate);
            var plan = new TriggerPlan<Ping>(phase: 0, priority: 0, triggerId: 1002, actions: new[] { call });

            runner.RegisterPlan<Ping, TestContext>(key, in plan);
            bus.Publish(key, new Ping());

            schedulerManager.Update(9f, new TestDispatcherContext(contextSource.Current));
            schedulerManager.Update(1f, new TestDispatcherContext(contextSource.Current));
            schedulerManager.Update(9f, new TestDispatcherContext(contextSource.Current));
            schedulerManager.Update(1f, new TestDispatcherContext(contextSource.Current));
            schedulerManager.Update(10f, new TestDispatcherContext(contextSource.Current));

            Assert.That(observedContextValues, Is.EqualTo(new[] { 3, 3 }));
        }

        [Test]
        public void RuleScheduler_EveryPlan_ExecutesUntilMaxOccurrences()
        {
            var driver = new DefaultRuleSchedulerDriver();
            var executed = 0;
            var completed = 0;
            var handle = driver.Schedule(
                RuleSchedulePlan.Every(10f, maxOccurrences: 2, groupId: "rule:interval"),
                new DelegateRuleScheduleEffect(_ => executed++, onCompleted: _ => completed++));

            driver.Update(9f);
            driver.Update(1f);
            driver.Update(10f);

            Assert.That(handle.IsValid, Is.True);
            Assert.That(executed, Is.EqualTo(2));
            Assert.That(completed, Is.EqualTo(1));
            Assert.That(driver.TryGet(handle, out _), Is.False);
        }

        [Test]
        public void RuleScheduler_ReplaceExisting_CancelsMatchingRuleOnly()
        {
            var driver = new DefaultRuleSchedulerDriver();
            var first = driver.Schedule(
                RuleSchedulePlan.Every(10f, groupId: "rule:replace", subjectId: "target:1"),
                new DelegateRuleScheduleEffect(_ => { }));
            var other = driver.Schedule(
                RuleSchedulePlan.Every(10f, groupId: "rule:replace", subjectId: "target:2"),
                new DelegateRuleScheduleEffect(_ => { }));

            driver.Schedule(
                RuleSchedulePlan.Now(groupId: "rule:replace", subjectId: "target:1").WithReplacement(),
                new DelegateRuleScheduleEffect(_ => { }));

            Assert.That(driver.TryGet(first, out var firstSnapshot), Is.True);
            Assert.That(firstSnapshot.State, Is.EqualTo(ERuleScheduleState.Cancelled));
            Assert.That(driver.TryGet(other, out var otherSnapshot), Is.True);
            Assert.That(otherSnapshot.State, Is.Not.EqualTo(ERuleScheduleState.Cancelled));
        }

        [Test]
        public void RuleSchedulerRegistry_DispatchesToCustomDriver()
        {
            var defaultDriver = new DefaultRuleSchedulerDriver("default:test");
            var customDriver = new RecordingRuleSchedulerDriver("custom:test");
            var registry = new RuleSchedulerRegistry(defaultDriver);
            registry.RegisterDriver(customDriver);

            var handle = registry.Schedule(
                RuleSchedulePlan.Now(label: "custom rule"),
                new DelegateRuleScheduleEffect(_ => { }),
                "custom:test");

            Assert.That(handle.DriverId, Is.EqualTo("custom:test"));
            Assert.That(customDriver.ScheduledCount, Is.EqualTo(1));
            Assert.That(defaultDriver.FindByGroup("custom rule"), Is.Empty);
        }

        [Test]
        public void RuleScheduler_InvalidEveryPlan_ReportsValidationError()
        {
            var plan = RuleSchedulePlan.Every(0f, groupId: "rule:invalid");
            var result = RuleSchedulePlanValidator.Validate(in plan);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Matches<ValidationIssue>(issue => issue.Code == ValidationErrorCodes.INVALID_RULE_SCHEDULE));
        }

        [Test]
        public void RuleScheduler_PauseAndResume_PreservesElapsedTime()
        {
            var driver = new DefaultRuleSchedulerDriver();
            var executed = 0;
            var handle = driver.Schedule(
                RuleSchedulePlan.Every(10f, groupId: "rule:pause"),
                new DelegateRuleScheduleEffect(_ => executed++));

            driver.Update(5f);
            Assert.That(driver.Pause(handle), Is.True);
            driver.Update(20f);
            Assert.That(driver.Resume(handle), Is.True);
            driver.Update(5f);
            driver.Update(5f);

            Assert.That(executed, Is.EqualTo(1));
        }

        [Test]
        public void RuleScheduler_CancelGroup_CancelsOnlyActiveMatchingGroup()
        {
            var driver = new DefaultRuleSchedulerDriver();
            var cancelledGroupExecuted = 0;
            var otherGroupExecuted = 0;
            var first = driver.Schedule(
                RuleSchedulePlan.Every(10f, groupId: "rule:cancel_group", subjectId: "target:1"),
                new DelegateRuleScheduleEffect(_ => cancelledGroupExecuted++));
            var second = driver.Schedule(
                RuleSchedulePlan.After(20f, groupId: "rule:cancel_group", subjectId: "target:2"),
                new DelegateRuleScheduleEffect(_ => cancelledGroupExecuted++));
            var other = driver.Schedule(
                RuleSchedulePlan.Every(10f, groupId: "rule:other_group", subjectId: "target:3"),
                new DelegateRuleScheduleEffect(_ => otherGroupExecuted++));

            Assert.That(driver.CancelGroup("rule:cancel_group"), Is.EqualTo(2));
            Assert.That(driver.TryGet(first, out var firstSnapshot), Is.True);
            Assert.That(driver.TryGet(second, out var secondSnapshot), Is.True);
            Assert.That(firstSnapshot.State, Is.EqualTo(ERuleScheduleState.Cancelled));
            Assert.That(secondSnapshot.State, Is.EqualTo(ERuleScheduleState.Cancelled));

            driver.Update(20f);

            Assert.That(cancelledGroupExecuted, Is.EqualTo(0));
            Assert.That(otherGroupExecuted, Is.EqualTo(1));
            Assert.That(driver.TryGet(first, out _), Is.False);
            Assert.That(driver.TryGet(second, out _), Is.False);
            Assert.That(driver.TryGet(other, out var otherSnapshot), Is.True);
            Assert.That(otherSnapshot.State, Is.Not.EqualTo(ERuleScheduleState.Cancelled));
        }

        [Test]
        public void SchedulerMigration_MapsLegacyConfigToRuleSchedulerPlanAndRecommendedRuntime()
        {
            var periodic = SchedulerConfig.Periodic(25f, maxExecutions: 3);
            periodic.DelayMs = 5f;
            var continuous = SchedulerConfig.Continuous(10f);

            var periodicPlan = SchedulerMigration.ToRuleSchedulePlan(
                in periodic,
                groupId: "legacy:scheduler",
                subjectId: "buff:1",
                label: "legacy periodic",
                replaceExisting: true);
            var continuousPlan = SchedulerMigration.ToRuleSchedulePlan(in continuous);

            Assert.That(periodicPlan.Mode, Is.EqualTo(ERuleScheduleMode.Every));
            Assert.That(periodicPlan.DelayMs, Is.EqualTo(5f));
            Assert.That(periodicPlan.IntervalMs, Is.EqualTo(25f));
            Assert.That(periodicPlan.MaxOccurrences, Is.EqualTo(3));
            Assert.That(periodicPlan.GroupId, Is.EqualTo("legacy:scheduler"));
            Assert.That(periodicPlan.SubjectId, Is.EqualTo("buff:1"));
            Assert.That(periodicPlan.Label, Is.EqualTo("legacy periodic"));
            Assert.That(periodicPlan.ReplaceExisting, Is.True);
            Assert.That(continuousPlan.Mode, Is.EqualTo(ERuleScheduleMode.WhileActive));
            Assert.That(continuousPlan.MaxOccurrences, Is.EqualTo(-1));
            Assert.That(SchedulerMigration.GetRecommendedRuntime(in periodic), Is.EqualTo(SchedulerMigration.RuleSchedulingRuntime));
            Assert.That(SchedulerMigration.GetRecommendedRuntime(in periodic, isTriggerPlanAction: true), Is.EqualTo(SchedulerMigration.TriggerPlanActionSchedulingRuntime));
        }

        [Test]
        public void SimpleScheduleManager_StaleHandle_DoesNotAffectReusedIndexItem()
        {
            var manager = new SimpleScheduleManager();
            var staleHandle = manager.Register(
                ScheduleRegisterRequest.Delayed(0f, businessId: 1, triggerId: 10),
                new RecordingScheduleEffect());

            manager.Update(0f);

            var activeHandle = manager.Register(
                ScheduleRegisterRequest.Delayed(100f, businessId: 2, triggerId: 20),
                new RecordingScheduleEffect());

            Assert.That(manager.Cancel(staleHandle), Is.False);
            Assert.That(manager.TryGetItem(activeHandle, out var activeItem), Is.True);
            Assert.That(activeItem.BusinessId, Is.EqualTo(2));
            Assert.That(activeItem.TriggerId, Is.EqualTo(20));
            Assert.That(activeItem.State, Is.Not.EqualTo(EScheduleItemState.Terminated));
        }

        private sealed class RecordingRuleSchedulerDriver : IRuleSchedulerDriver
        {
            private readonly DefaultRuleSchedulerDriver _inner;

            public RecordingRuleSchedulerDriver(string driverId)
            {
                _inner = new DefaultRuleSchedulerDriver(driverId);
            }

            public string DriverId => _inner.DriverId;
            public int ScheduledCount { get; private set; }

            public RuleScheduleHandle Schedule(in RuleSchedulePlan plan, IRuleScheduleEffect effect)
            {
                ScheduledCount++;
                return _inner.Schedule(in plan, effect);
            }

            public bool TryGet(RuleScheduleHandle handle, out RuleScheduleSnapshot snapshot) => _inner.TryGet(handle, out snapshot);
            public IReadOnlyList<RuleScheduleSnapshot> FindByGroup(string groupId) => _inner.FindByGroup(groupId);
            public IReadOnlyList<RuleScheduleSnapshot> FindBySubject(string subjectId) => _inner.FindBySubject(subjectId);
            public bool Pause(RuleScheduleHandle handle) => _inner.Pause(handle);
            public bool Resume(RuleScheduleHandle handle) => _inner.Resume(handle);
            public bool Interrupt(RuleScheduleHandle handle, string reason = null) => _inner.Interrupt(handle, reason);
            public bool Cancel(RuleScheduleHandle handle) => _inner.Cancel(handle);
            public int InterruptGroup(string groupId, string reason = null) => _inner.InterruptGroup(groupId, reason);
            public int CancelGroup(string groupId) => _inner.CancelGroup(groupId);
            public void Update(float deltaTimeMs, object userContext = null) => _inner.Update(deltaTimeMs, userContext);
            public void Clear() => _inner.Clear();
        }

        private sealed class RecordingScheduleEffect : IScheduleEffect
        {
            public int ExecuteCount { get; private set; }
            public bool CanExecute(in ScheduleContext ctx) => true;
            public void Execute(in ScheduleContext ctx) => ExecuteCount++;
        }

        private sealed class RecordingLifecycle<TCtx> : ITriggerLifecycle<TCtx>
        {
            public readonly List<long> RegisteredOrders = new List<long>();

            public void OnRegistered<TArgs>(EventKey<TArgs> key, ITrigger<TArgs, TCtx> trigger, int phase, int priority, long order)
            {
                RegisteredOrders.Add(order);
            }

            public void OnUnregistered<TArgs>(EventKey<TArgs> key, ITrigger<TArgs, TCtx> trigger) { }
            public void OnEventDispatching<TArgs>(EventKey<TArgs> key, in TArgs args) { }
            public void OnEventDispatched<TArgs>(EventKey<TArgs> key, in TArgs args, int executedCount, int shortCircuitedCount) { }
            public void OnBeforeEvaluate<TArgs>(EventKey<TArgs> key, in TArgs args, int phase, int priority, long order) { }
            public void OnAfterEvaluate<TArgs>(EventKey<TArgs> key, in TArgs args, int phase, int priority, long order, bool result) { }
            public void OnBeforeExecute<TArgs>(EventKey<TArgs> key, in TArgs args, int phase, int priority, long order) { }
            public void OnAfterExecute<TArgs>(EventKey<TArgs> key, in TArgs args, int phase, int priority, long order) { }
            public void OnShortCircuit<TArgs>(EventKey<TArgs> key, in TArgs args, int phase, int priority, long order, ShortCircuitReason reason) { }
            public void OnScopeTransition(string fromScope, string toScope) { }
            public void OnConditionPassed<TArgs>(EventKey<TArgs> key, in TArgs args, int phase, int priority, long order, int conditionId, string conditionName) { }
            public void OnConditionFailed<TArgs>(EventKey<TArgs> key, in TArgs args, int phase, int priority, long order, int conditionId, string conditionName) { }
            public void OnActionExecuting<TArgs>(EventKey<TArgs> key, in TArgs args, int phase, int priority, long order, int actionId, string actionName, int actionIndex, int totalActions) { }
            public void OnActionExecuted<TArgs>(EventKey<TArgs> key, in TArgs args, int phase, int priority, long order, int actionId, string actionName, int actionIndex, int totalActions, bool wasInterrupted) { }
            public void OnActionFailed<TArgs>(EventKey<TArgs> key, in TArgs args, int phase, int priority, long order, int actionId, string actionName, int actionIndex, int totalActions, string errorMessage) { }
        }

        private sealed class MutableContextSource : ITriggerContextSource<TestContext>
        {
            public TestContext Current { get; set; }

            public TestContext GetContext()
            {
                return Current;
            }
        }

        private sealed class TestDispatcherContext : ITriggerDispatcherContext
        {
            public TestDispatcherContext(object context)
            {
                Context = context;
            }

            public object Context { get; }
            public float CurrentTimeMs => 0f;
            public T GetService<T>() where T : class => Context as T;
        }

        private sealed class TestContext
        {
            public TestContext(int value = 0)
            {
                Value = value;
            }

            public int Value { get; }
        }

        private sealed class Ping
        {
        }
    }
}
