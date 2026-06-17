using AbilityKit.Ability.Host.Extensions.Moba.Runtime;
using AbilityKit.Core.Continuous;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.Services.Area;
using AbilityKit.Demo.Moba.Services.Projectile;
using AbilityKit.Demo.Moba.Services.Projectile.Launch;
using AbilityKit.Demo.Moba.Services.Search;
using AbilityKit.Demo.Moba.Services.Triggering;
using AbilityKit.Demo.Moba.Services.Triggering.PlanActions;
using AbilityKit.Demo.Moba.Systems;
using AbilityKit.GameplayTags;
using AbilityKit.Triggering.Eventing;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan.Json;

namespace AbilityKit.Demo.Moba.Services
{
    public sealed class MobaRuntimeDependencyHealthValidator : IMobaRuntimeValidator
    {
        private const string Source = "runtime.dependencies";

        public string Name => Source;

        public void Validate(in MobaRuntimeValidationContext context, MobaRuntimeValidationReport report)
        {
            if (report == null) return;

            Require<MobaConfigDatabase>(context, report, "config.database", "MobaConfigDatabase is required by skill, buff, projectile, summon, area and tag template runtime.");
            Require<IEventBus>(context, report, "event.bus", "IEventBus is required for gameplay, buff, projectile and trigger events.");
            Require<TriggerPlanJsonDatabase>(context, report, "trigger.plan.database", "TriggerPlanJsonDatabase is required for runtime trigger plans.");
            Require<TriggerRunner<AbilityKit.Ability.World.DI.IWorldResolver>>(context, report, "trigger.runner", "TriggerRunner<IWorldResolver> is required to execute configured plans.");
            Require<MobaEventSubscriptionRegistry>(context, report, "trigger.event.registry", "MobaEventSubscriptionRegistry is required to bind typed event args.");
            Require<MobaTriggerPlanSubscriptionService>(context, report, "trigger.plan.subscription", "MobaTriggerPlanSubscriptionService is required for lifecycle trigger subscriptions.");
            Require<PlanActionModuleRegistry>(context, report, "trigger.action.modules", "PlanActionModuleRegistry is required for plan action discovery.");

            Require<IMobaSkillPipelineLibrary>(context, report, "skill.pipeline.library", "IMobaSkillPipelineLibrary is required for table driven skill execution.");
            Require<SkillExecutor>(context, report, "skill.executor", "SkillExecutor is required for command driven skill casts.");
            Require<MobaEffectInvokerService>(context, report, "skill.effect.invoker", "MobaEffectInvokerService is required for configured effect execution.");
            Require<MobaEffectExecutionService>(context, report, "skill.effect.execution", "MobaEffectExecutionService is required for trigger/action effect execution.");
            Require<MobaSkillCastRuntimeService>(context, report, "skill.cast.runtime", "MobaSkillCastRuntimeService is required for cast runtime tracking.");

            Require<MobaBuffService>(context, report, "buff.service", "MobaBuffService is required for continuous buff lifecycle.");
            Require<IContinuousManager>(context, report, "continuous.manager", "IContinuousManager is required for shared continuous behavior lifecycle.");
            Require<IMobaContinuousModifierQueryService>(context, report, "continuous.modifier.query", "IMobaContinuousModifierQueryService is required for modifier projection and parameter overrides.");
            Require<IMobaEffectiveTagQueryService>(context, report, "continuous.effective.tags", "IMobaEffectiveTagQueryService is required for gameplay tag based state/control queries.");
            Require<IMobaContinuousTagRuleService>(context, report, "continuous.tag.rules", "IMobaContinuousTagRuleService is required for tag admission and lifecycle rules.");
            Require<IGameplayTagService>(context, report, "gameplay.tags", "IGameplayTagService is required for gameplay tag based runtime state.");

            Require<DamagePipelineService>(context, report, "combat.damage.pipeline", "DamagePipelineService is required for formal damage calculation.");
            Require<MobaShieldService>(context, report, "combat.shield", "MobaShieldService is required for shield mitigation stages.");
            Require<MobaDamageMitigationService>(context, report, "combat.damage.mitigation", "MobaDamageMitigationService is required for mitigation stages.");
            Require<MobaCombatRulesService>(context, report, "combat.rules", "MobaCombatRulesService is required for target legality and team rules.");

            Require<MobaProjectileService>(context, report, "projectile.service", "MobaProjectileService is required for projectile runtime.");
            Require<IMobaProjectileEmitterManager>(context, report, "projectile.emitter.manager", "IMobaProjectileEmitterManager is required for attribute driven projectile emitter extensions.");
            Require<MobaSummonService>(context, report, "summon.service", "MobaSummonService is required for summon spawning and owner lifecycle.");
            Require<MobaAreaRuntimeService>(context, report, "area.runtime", "MobaAreaRuntimeService is required for area runtime tracking.");
            Require<SearchTargetService>(context, report, "search.target", "SearchTargetService is required for configured target query execution.");
            Require<IMobaTemporaryEntityLifecycleHealthProvider>(context, report, "temp_entity.lifecycle", "IMobaTemporaryEntityLifecycleHealthProvider is required for projectile/area/summon lifecycle governance.");

            Require<MobaSnapshotRouter>(context, report, "snapshot.router", "MobaSnapshotRouter is required for battle state snapshots.");
            Require<IMobaSnapshotHealthProvider>(context, report, "snapshot.health", "IMobaSnapshotHealthProvider is required for snapshot readiness diagnostics.");
            Require<IMobaBattleRuntimePort>(context, report, "runtime.port", "IMobaBattleRuntimePort is required as the single formal battle runtime entry.");
            Require<MobaAuthorityFrameService>(context, report, "frames.authority", "MobaAuthorityFrameService is required for confirmed/predicted frame tracking.");
            Require<IMobaBattleDiagnosticsService>(context, report, "diagnostics.service", "IMobaBattleDiagnosticsService is required for runtime diagnostics events.");
            Require<IMobaBattleExceptionPolicy>(context, report, "diagnostics.exception.policy", "IMobaBattleExceptionPolicy is required for centralized exception governance.");

            Require<MobaTraceRegistry>(context, report, "trace.registry", "MobaTraceRegistry is required for formal trace lineage diagnostics and skill cast roots.");
            Optional<MobaBattleRouteRegistry>(context, report, "routing.registry", "MobaBattleRouteRegistry is not resolved; battle route diagnostics may be incomplete.");
        }

        private static void Require<T>(in MobaRuntimeValidationContext context, MobaRuntimeValidationReport report, string path, string message) where T : class
        {
            if (!context.TryResolve<T>(out _))
            {
                report.Error(Source, path, message, typeof(T).Name, blocksStartup: true);
            }
        }

        private static void Optional<T>(in MobaRuntimeValidationContext context, MobaRuntimeValidationReport report, string path, string message) where T : class
        {
            if (!context.TryResolve<T>(out _))
            {
                report.Warning(Source, path, message, typeof(T).Name);
            }
        }
    }
}
