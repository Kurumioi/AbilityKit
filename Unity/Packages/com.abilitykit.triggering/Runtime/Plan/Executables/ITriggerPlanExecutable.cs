using AbilityKit.Triggering.Runtime;

namespace AbilityKit.Triggering.Runtime.Plan
{
    public interface ITriggerPlanExecutable
    {
        string Name { get; }
        ETriggerPlanExecutableKind Kind { get; }
        float Weight { get; }

        TriggerPlanExecutionResult Execute<TCtx>(object args, in ExecCtx<TCtx> ctx)
            where TCtx : class;
    }
}
