using System;
using AbilityKit.Ability.World.DI;
using AbilityKit.Core.Common.Log;
using AbilityKit.Triggering.Eventing;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan;
using AbilityKit.Triggering.Runtime.Plan.Json;

namespace AbilityKit.Demo.Moba.Services
{
    internal sealed class MobaTriggerPlanExecutor
    {
        private readonly IWorldResolver _services;
        private readonly TriggerPlanJsonDatabase _planDb;
        private readonly IEventBus _eventBus;
        private readonly FunctionRegistry _functions;
        private readonly ActionRegistry _actions;
        public MobaTriggerPlanExecutor(
            IWorldResolver services,
            TriggerPlanJsonDatabase planDb,
            IEventBus eventBus,
            FunctionRegistry functions,
            ActionRegistry actions)
        {
            _services = services;
            _planDb = planDb;
            _eventBus = eventBus;
            _functions = functions;
            _actions = actions;
        }

        public bool TryGetPlan(int triggerId, out TriggerPlan<object> plan)
        {
            plan = default;
            return triggerId > 0 && _planDb != null && _planDb.TryGetPlanByTriggerId(triggerId, out plan);
        }

        public bool Execute(int triggerId, object args)
        {
            if (!TryGetPlan(triggerId, out var plan)) return false;

            if (_eventBus == null || _functions == null || _actions == null)
            {
                Log.Warning($"[MobaTriggerPlanExecutor] Plan runtime deps missing; skip plan exec. triggerId={triggerId}");
                return false;
            }

            var ctrl = new ExecutionControl();
            ctrl.Reset();

            var execCtx = new ExecCtx<IWorldResolver>(
                context: _services,
                eventBus: _eventBus,
                functions: _functions,
                actions: _actions,
                blackboards: null,
                payloads: null,
                idNames: null,
                numericDomains: null,
                numericFunctions: null,
                policy: default,
                control: ctrl);

            var hasExecutionRoot = _planDb.TryGetExecutionRootByTriggerId(triggerId, out var executionRoot);

            bool ExecuteOnce()
            {
                var planned = new PlannedTrigger<object, IWorldResolver>(plan);
                var ok = planned.Evaluate(args, execCtx);
                if (ctrl.StopPropagation || ctrl.Cancel) return ok;
                if (!ok) return true;

                if (hasExecutionRoot && executionRoot != null)
                {
                    executionRoot.Execute(args, in execCtx);
                }
                else
                {
                    planned.Execute(args, execCtx);
                }

                return true;
            }

            try
            {
                return ExecuteOnce();
            }
            catch (Exception ex)
            {
                Log.Exception(ex, $"[MobaTriggerPlanExecutor] Plan execution failed. triggerId={triggerId}");
                return false;
            }
        }
    }
}
