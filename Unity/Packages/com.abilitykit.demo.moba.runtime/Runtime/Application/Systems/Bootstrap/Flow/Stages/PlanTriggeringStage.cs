using System;
using AbilityKit.Ability.World.DI;
using AbilityKit.Core.Common.Event;
using AbilityKit.Core.Common.Log;
using AbilityKit.Demo.Moba.Gameplay.Triggering;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Triggering.Eventing;
using AbilityKit.Triggering.Runtime.Plan.Json;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Config.Plans;

namespace AbilityKit.Demo.Moba.Systems.Bootstrap.Flow.Stages
{
    /// <summary>
    /// PlanTriggering Install Stage
    /// 安装计划触发系统
    /// </summary>
    [MobaBootstrapStage]
    public sealed class PlanTriggeringStage : MobaBootstrapStageBase
    {
        public override string Name => "Install.PlanTriggering";

        protected internal override void Configure(WorldContainerBuilder builder)
        {
            // Plan action modules live under Application/Services/Triggering/PlanActions.
            // This bootstrap stage only triggers installation after service modules are configured.
        }

        protected internal override void Install(
            Entitas.IContexts contexts,
            Entitas.Systems systems,
            IWorldResolver services)
        {
            try
            {
                Log.Info("[PlanTriggeringStage] starting...");
                if (services.TryResolve<TriggerPlanJsonDatabase>(out var db) && db != null
                    && services.TryResolve<MobaEffectExecutionService>(out var effects) && effects != null)
                {
                    Log.Info("[PlanTriggeringStage] initializing plan actions...");
                    effects.InitializePlanActions();

                    services.TryResolve<MobaEventSubscriptionRegistry>(out var eventRegistry);
                    MobaGameplayTriggerValidator.Validate(db, eventRegistry);

                    if (services.TryResolve<TriggerRunner<IWorldResolver>>(out var runner) && runner != null)
                    {
                        RegisterGlobalPlans(services, db, runner);
                    }
                    Log.Info($"[PlanTriggeringStage] PlanTriggering initialized. records={db.Records?.Count ?? 0}");
                }
                else
                {
                    Log.Warning("[PlanTriggeringStage] init skipped (missing deps: db or effects)");
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "[PlanTriggeringStage] PlanTriggering init exception");
            }
        }

        private static void RegisterGlobalPlans(IWorldResolver services, TriggerPlanJsonDatabase db, TriggerRunner<IWorldResolver> runner)
        {
            var records = db.Records;
            if (records == null || records.Count == 0)
            {
                return;
            }

            services.TryResolve<MobaEventSubscriptionRegistry>(out var eventRegistry);

            for (int i = 0; i < records.Count; i++)
            {
                var record = records[i];
                if (record.EventId == 0 || record.Scope != TriggerPlanScope.Global)
                {
                    continue;
                }

                if (IsGameplayRecord(record))
                {
                    continue;
                }

                if (eventRegistry != null
                    && !string.IsNullOrEmpty(record.EventName)
                    && eventRegistry.TryGetArgsType(record.EventName, out var argsType)
                    && argsType != null
                    && argsType.IsClass)
                {
                    runner.RegisterPlan(record.EventId, argsType, record.Plan);
                    continue;
                }

                var key = new EventKey<object>(record.EventId);
                runner.RegisterPlan<object, IWorldResolver>(key, record.Plan);
            }
        }

        private static bool IsGameplayRecord(TriggerPlanJsonDatabase.Record record)
        {
            return !string.IsNullOrEmpty(record.EventName)
                   && record.EventName.StartsWith("gameplay.", StringComparison.OrdinalIgnoreCase);
        }
    }
}
