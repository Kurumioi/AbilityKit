using System;
using System.Collections.Generic;

namespace AbilityKit.Triggering.Runtime.Plan
{
    public abstract class CompositeTriggerPlanExecutableBase : TriggerPlanExecutableBase
    {
        private readonly ITriggerPlanExecutable[] _children;

        protected IReadOnlyList<ITriggerPlanExecutable> Children => _children;

        protected CompositeTriggerPlanExecutableBase(ITriggerPlanExecutable[] children, ITriggerPlanCondition condition = null, float weight = 1f)
            : base(condition, weight)
        {
            _children = children ?? Array.Empty<ITriggerPlanExecutable>();
        }
    }
}
