using AbilityKit.Core.Eventing;

namespace AbilityKit.Triggering.Runtime
{
    internal enum TriggerRunnerShortCircuitCueKind
    {
        Skipped,
        Interrupted
    }

    /// <summary>
    /// 集中处理 TriggerRunner 的 Cue 上下文构建与短路 Cue 派发，避免主运行器混杂展示/反馈回调细节。
    /// </summary>
    internal static class TriggerRunnerCueDispatcher
    {
        public static TriggerCueContext BuildCueContext<TArgs, TCtx>(
            EventKey<TArgs> key,
            in TArgs args,
            int phase,
            int priority,
            long order,
            ITrigger<TArgs, TCtx> trigger,
            ShortCircuitReason reason,
            string interruptSourceName,
            int interruptTriggerId,
            bool interruptConditionPassed,
            ExecutionControl control)
        {
            var triggerId = 0;
            var triggerTypeName = trigger?.GetType().Name ?? "Unknown";
            if (trigger is ITriggerWithId tid)
            {
                triggerId = tid.TriggerId;
            }

            return new TriggerCueContext(
                key.IntId,
                key.StringId,
                args,
                phase,
                priority,
                order,
                triggerId,
                triggerTypeName,
                MapReason(reason),
                interruptSourceName,
                interruptTriggerId,
                interruptConditionPassed,
                control);
        }

        public static void DispatchShortCircuitCue<TArgs, TCtx>(
            ITrigger<TArgs, TCtx> trigger,
            in TriggerCueContext cueContext,
            TriggerRunnerShortCircuitCueKind cueKind)
        {
            switch (cueKind)
            {
                case TriggerRunnerShortCircuitCueKind.Skipped:
                    trigger.Cue.OnSkipped(in cueContext);
                    break;
                case TriggerRunnerShortCircuitCueKind.Interrupted:
                    trigger.Cue.OnInterrupted(in cueContext);
                    break;
            }
        }

        public static ETriggerShortCircuitReason MapReason(ShortCircuitReason reason)
        {
            switch (reason)
            {
                case ShortCircuitReason.ConditionFailed:
                    return ETriggerShortCircuitReason.ConditionFailed;
                case ShortCircuitReason.StopPropagation:
                    return ETriggerShortCircuitReason.StopPropagation;
                case ShortCircuitReason.Cancel:
                    return ETriggerShortCircuitReason.Cancel;
                case ShortCircuitReason.InterruptedByHigherPriority:
                    return ETriggerShortCircuitReason.InterruptedByHigherPriority;
                case ShortCircuitReason.InterruptedByFailedCondition:
                    return ETriggerShortCircuitReason.InterruptedByFailedCondition;
                default:
                    return ETriggerShortCircuitReason.None;
            }
        }
    }
}
