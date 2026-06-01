using System;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime.Plan.Json;

namespace AbilityKit.Demo.Moba.Systems
{
    public sealed partial class MobaWorldBootstrapModule
    {
        [Obsolete("Legacy positional plan action stubs are disabled. Register strongly typed plan actions through PlanActionModuleRegistry.")]
        private static void RegisterStubActionsFromPlans(TriggerPlanJsonDatabase db, ActionRegistry actions)
        {
        }
    }
}
