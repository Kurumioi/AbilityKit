using System;
using System.Reflection;
using AbilityKit.Core.Eventing;
using AbilityKit.Triggering.Runtime.Plan;

namespace AbilityKit.Triggering.Runtime
{
    public static class TriggerRunnerPlanExtensions
    {
        private static readonly MethodInfo RegisterPlanAsMethod = typeof(TriggerRunnerPlanExtensions).GetMethod(nameof(RegisterPlanAs), BindingFlags.NonPublic | BindingFlags.Static);

        public static IDisposable RegisterPlan<TArgs, TCtx>(this TriggerRunner<TCtx> runner, EventKey<TArgs> key, in TriggerPlan<TArgs> plan)
            where TArgs : class
        {
            if (runner == null) throw new ArgumentNullException(nameof(runner));
            var trigger = new PlannedTrigger<TArgs, TCtx>(plan);
            return runner.Register(key, trigger, plan.Phase, plan.Priority);
        }

        public static IDisposable RegisterPlan<TCtx>(this TriggerRunner<TCtx> runner, int eventId, Type argsType, in TriggerPlan<object> plan)
        {
            if (runner == null) throw new ArgumentNullException(nameof(runner));
            if (argsType == null) throw new ArgumentNullException(nameof(argsType));
            if (eventId == 0) throw new ArgumentException(nameof(eventId));
            if (!typeof(object).IsAssignableFrom(argsType)) throw new ArgumentException(nameof(argsType));

            var mi = RegisterPlanAsMethod.MakeGenericMethod(argsType, typeof(TCtx));
            return (IDisposable)mi.Invoke(null, new object[] { runner, eventId, plan });
        }

        private static IDisposable RegisterPlanAs<TArgs, TCtx>(TriggerRunner<TCtx> runner, int eventId, TriggerPlan<object> plan)
            where TArgs : class
        {
            var typedPlan = plan.AsArgs<TArgs>();
            var key = new EventKey<TArgs>(eventId);
            return runner.RegisterPlan<TArgs, TCtx>(key, typedPlan);
        }
    }
}
