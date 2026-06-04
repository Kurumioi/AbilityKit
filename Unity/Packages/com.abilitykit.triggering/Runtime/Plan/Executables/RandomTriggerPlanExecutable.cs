using AbilityKit.Triggering.Runtime;

namespace AbilityKit.Triggering.Runtime.Plan
{
    public sealed class RandomTriggerPlanExecutable : CompositeTriggerPlanExecutableBase
    {
        private static readonly System.Random Random = new System.Random();

        public override string Name => "Random";
        public override ETriggerPlanExecutableKind Kind => ETriggerPlanExecutableKind.Random;

        public RandomTriggerPlanExecutable(ITriggerPlanExecutable[] children, ITriggerPlanCondition condition = null, float weight = 1f)
            : base(children, condition, weight)
        {
        }

        protected override TriggerPlanExecutionResult ExecuteCore<TCtx>(object args, in ExecCtx<TCtx> ctx)
        {
            var totalWeight = 0f;
            for (int i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                if (child != null)
                    totalWeight += child.Weight;
            }

            if (totalWeight <= 0f)
                return TriggerPlanExecutionResult.Skipped("Random has no weighted branch");

            var selected = (float)(Random.NextDouble() * totalWeight);
            for (int i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                if (child == null)
                    continue;

                selected -= child.Weight;
                if (selected <= 0f)
                    return child.Execute(args, in ctx);
            }

            return TriggerPlanExecutionResult.Skipped("Random branch selection failed");
        }
    }
}
