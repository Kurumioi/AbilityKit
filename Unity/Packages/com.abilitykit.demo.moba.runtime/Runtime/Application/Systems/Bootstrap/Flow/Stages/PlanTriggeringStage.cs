using System;
using AbilityKit.Ability.World.DI;
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
                if (!services.TryResolve<TriggerPlanJsonDatabase>(out var db) || db == null)
                {
                    throw new InvalidOperationException("PlanTriggeringStage requires TriggerPlanJsonDatabase.");
                }

                if (!services.TryResolve<MobaEffectExecutionService>(out var effects) || effects == null)
                {
                    throw new InvalidOperationException("PlanTriggeringStage requires MobaEffectExecutionService.");
                }

                Log.Info("[PlanTriggeringStage] initializing plan actions...");
                effects.InitializePlanActions();

                RunRuntimeValidation(services);

                if (services.TryResolve<TriggerRunner<IWorldResolver>>(out var runner) && runner != null)
                {
                    RegisterGlobalPlans(services, db, runner);
                }

                Log.Info($"[PlanTriggeringStage] PlanTriggering initialized. records={db.Records?.Count ?? 0}");
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "[PlanTriggeringStage] PlanTriggering init exception");
                throw;
            }
        }

        private static void RunRuntimeValidation(IWorldResolver services)
        {
            if (services == null)
            {
                throw new InvalidOperationException("PlanTriggeringStage requires world services for runtime validation.");
            }

            if (!services.TryResolve<IMobaRuntimeValidationRegistry>(out var registry) || registry == null)
            {
                throw new InvalidOperationException("PlanTriggeringStage requires IMobaRuntimeValidationRegistry.");
            }

            var validatorContract = MobaRuntimeValidatorContract.CreateDefault();
            validatorContract.RegisterInto(registry);
            var contractValidation = validatorContract.Validate(registry);
            if (!contractValidation.Succeeded)
            {
                throw new InvalidOperationException("Runtime validator contract validation failed. " + string.Join("; ", contractValidation.Errors));
            }

            if (!services.TryResolve<IMobaRuntimeValidationRunner>(out var runner) || runner == null)
            {
                throw new InvalidOperationException("PlanTriggeringStage requires IMobaRuntimeValidationRunner.");
            }

            var context = new MobaRuntimeValidationContext(services, nameof(PlanTriggeringStage));
            var report = runner.ValidateAll(in context);
            if (services.TryResolve<IMobaRuntimeValidationHistory>(out var history) && history != null && history.TryGetLastReport(out var lastReport))
            {
                MobaRuntimeLog.Info(MobaRuntimeLogModule.Bootstrap, MobaRuntimeLogPurpose.Validation, nameof(PlanTriggeringStage), $"Runtime validation history updated. runs={history.RunCount}, stage={history.LastStageName}, {lastReport.FormatSummary()}");
            }

            if (report != null && report.ShouldBlockStartup)
            {
                throw new InvalidOperationException("Runtime validation has blocking errors. " + report.FormatSummary() + "\n" + report.FormatAllEntries());
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

                if (eventRegistry == null)
                {
                    throw new InvalidOperationException("PlanTriggeringStage requires MobaEventSubscriptionRegistry for typed global trigger registration.");
                }

                if (string.IsNullOrEmpty(record.EventName)
                    || !eventRegistry.TryGetArgsType(record.EventName, out var argsType)
                    || argsType == null)
                {
                    throw new InvalidOperationException($"Global trigger event is not registered. triggerId={record.TriggerId} eventName={record.EventName}");
                }

                if (!argsType.IsClass)
                {
                    throw new InvalidOperationException($"Global trigger event args type must be a class. triggerId={record.TriggerId} eventName={record.EventName} argsType={argsType.FullName}");
                }

                runner.RegisterPlan(record.EventId, argsType, record.Plan);
            }
        }

        private static bool IsGameplayRecord(TriggerPlanJsonDatabase.Record record)
        {
            return !string.IsNullOrEmpty(record.EventName)
                   && record.EventName.StartsWith("gameplay.", StringComparison.OrdinalIgnoreCase);
        }
    }
}
