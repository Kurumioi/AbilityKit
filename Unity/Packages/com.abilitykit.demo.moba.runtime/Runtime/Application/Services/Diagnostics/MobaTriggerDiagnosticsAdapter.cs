using System;
using System.Diagnostics;
using AbilityKit.Ability.World.DI;
using AbilityKit.Core.Eventing;
using AbilityKit.Triggering.Runtime;

namespace AbilityKit.Demo.Moba.Services
{
    public sealed class MobaTriggerDiagnosticsAdapter : ITriggerLifecycle<IWorldResolver>, ITriggerTracer<IWorldResolver>
    {
        private readonly IWorldResolver _services;
        private long _nextScopeId = 1L;

        public MobaTriggerDiagnosticsAdapter(IWorldResolver services)
        {
            _services = services;
        }

        public void OnRegistered<TArgs>(EventKey<TArgs> key, ITrigger<TArgs, IWorldResolver> trigger, int phase, int priority, long order)
        {
            var diagnostics = Diagnostics;
            if (ShouldSampleHook(diagnostics)) diagnostics.Counter(MobaBattleDiagnosticMetric.TriggerRegistered);
        }

        public void OnUnregistered<TArgs>(EventKey<TArgs> key, ITrigger<TArgs, IWorldResolver> trigger)
        {
            var diagnostics = Diagnostics;
            if (ShouldSampleHook(diagnostics)) diagnostics.Counter(MobaBattleDiagnosticMetric.TriggerUnregistered);
        }

        public void OnEventDispatching<TArgs>(EventKey<TArgs> key, in TArgs args)
        {
            var diagnostics = Diagnostics;
            if (ShouldSampleHook(diagnostics)) diagnostics.Counter(MobaBattleDiagnosticMetric.TriggerDispatchStarted);
        }

        public void OnEventDispatched<TArgs>(EventKey<TArgs> key, in TArgs args, int executedCount, int shortCircuitedCount)
        {
            var diagnostics = Diagnostics;
            if (!ShouldSampleHook(diagnostics)) return;

            diagnostics.Counter(MobaBattleDiagnosticMetric.TriggerDispatchCompleted);
            diagnostics.Sample(MobaBattleDiagnosticMetric.TriggerDispatchExecuted, executedCount);
            diagnostics.Sample(MobaBattleDiagnosticMetric.TriggerDispatchShortCircuited, shortCircuitedCount);
        }

        public void OnBeforeEvaluate<TArgs>(EventKey<TArgs> key, in TArgs args, int phase, int priority, long order)
        {
        }

        public void OnAfterEvaluate<TArgs>(EventKey<TArgs> key, in TArgs args, int phase, int priority, long order, bool result)
        {
        }

        public void OnBeforeExecute<TArgs>(EventKey<TArgs> key, in TArgs args, int phase, int priority, long order)
        {
        }

        public void OnAfterExecute<TArgs>(EventKey<TArgs> key, in TArgs args, int phase, int priority, long order)
        {
        }

        public void OnShortCircuit<TArgs>(EventKey<TArgs> key, in TArgs args, int phase, int priority, long order, ShortCircuitReason reason)
        {
            var diagnostics = Diagnostics;
            if (ShouldSampleHook(diagnostics)) diagnostics.Counter(MobaBattleDiagnosticMetric.TriggerShortCircuit);
        }

        public void OnScopeTransition(string fromScope, string toScope)
        {
        }

        public void OnConditionPassed<TArgs>(EventKey<TArgs> key, in TArgs args, int phase, int priority, long order, int conditionId, string conditionName)
        {
        }

        public void OnConditionFailed<TArgs>(EventKey<TArgs> key, in TArgs args, int phase, int priority, long order, int conditionId, string conditionName)
        {
        }

        public void OnActionExecuting<TArgs>(EventKey<TArgs> key, in TArgs args, int phase, int priority, long order, int actionId, string actionName, int actionIndex, int totalActions)
        {
        }

        public void OnActionExecuted<TArgs>(EventKey<TArgs> key, in TArgs args, int phase, int priority, long order, int actionId, string actionName, int actionIndex, int totalActions, bool wasInterrupted)
        {
            if (wasInterrupted)
            {
                var diagnostics = Diagnostics;
                if (ShouldSampleHook(diagnostics)) diagnostics.Counter(MobaBattleDiagnosticMetric.TriggerActionInterrupted);
            }
        }

        public void OnActionFailed<TArgs>(EventKey<TArgs> key, in TArgs args, int phase, int priority, long order, int actionId, string actionName, int actionIndex, int totalActions, string errorMessage)
        {
            var diagnostics = Diagnostics;
            if (diagnostics == null) return;

            diagnostics.Counter(MobaBattleDiagnosticMetric.TriggerActionFailed);
            diagnostics.Warning(
                MobaBattleDiagnosticMetric.TriggerActionFailed,
                () => $"[MobaTriggerDiagnosticsAdapter] Trigger action failed. event={GetEventName(key)} phase={phase} priority={priority} order={order} actionId={actionId} action={actionName} index={actionIndex}/{totalActions} error={errorMessage}");
        }

        public TraceScope BeginTrace<TArgs>(EventKey<TArgs> key, in TArgs args)
        {
            return new TraceScope(_nextScopeId++, Stopwatch.GetTimestamp(), GetEventName(key), key.GetHashCode());
        }

        public void RecordTrigger<TArgs>(TraceScope scope, TriggerTraceRecord record)
        {
            var diagnostics = Diagnostics;
            if (!ShouldSampleHook(diagnostics)) return;

            switch (record.Kind)
            {
                case TriggerRecordKind.Evaluated:
                    diagnostics.Counter(MobaBattleDiagnosticMetric.TriggerEvaluated);
                    diagnostics.Counter(record.PredicateResult == false ? MobaBattleDiagnosticMetric.TriggerEvaluateFailed : MobaBattleDiagnosticMetric.TriggerEvaluatePassed);
                    RecordElapsedSample(diagnostics, MobaBattleDiagnosticMetric.TriggerEvaluateDuration, record.ElapsedTicks);
                    break;
                case TriggerRecordKind.Executed:
                    diagnostics.Counter(MobaBattleDiagnosticMetric.TriggerExecuted);
                    RecordElapsedSample(diagnostics, MobaBattleDiagnosticMetric.TriggerExecuteDuration, record.ElapsedTicks);
                    break;
                case TriggerRecordKind.ShortCircuited:
                    diagnostics.Counter(MobaBattleDiagnosticMetric.TriggerShortCircuit);
                    break;
            }
        }

        public void EndTrace(TraceScope scope)
        {
            var diagnostics = Diagnostics;
            if (ShouldSampleHook(diagnostics)) RecordElapsedSample(diagnostics, MobaBattleDiagnosticMetric.TriggerDispatchDuration, Stopwatch.GetTimestamp() - scope.StartTimestamp);
        }

        private IMobaBattleDiagnosticsService Diagnostics
        {
            get
            {
                if (_services == null) return null;
                return _services.TryResolve<IMobaBattleDiagnosticsService>(out var diagnostics) ? diagnostics : null;
            }
        }

        private static bool ShouldSampleHook(IMobaBattleDiagnosticsService diagnostics)
        {
            return diagnostics != null && diagnostics.ShouldSample(MobaBattleDiagnosticChannel.TriggerHook);
        }

        private static void RecordElapsedSample(IMobaBattleDiagnosticsService diagnostics, string metricName, long elapsedTicks)
        {
            if (diagnostics == null || elapsedTicks <= 0L) return;
            diagnostics.Sample(metricName, elapsedTicks * 1000.0d / Stopwatch.Frequency);
        }

        private static string GetEventName<TArgs>(EventKey<TArgs> key)
        {
            return key.StringId ?? key.IntId.ToString();
        }
    }
}
