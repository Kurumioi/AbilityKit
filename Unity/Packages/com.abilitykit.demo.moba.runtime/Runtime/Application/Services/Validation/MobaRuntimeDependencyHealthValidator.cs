using AbilityKit.Ability.Host.Extensions.Moba.Runtime;
using AbilityKit.Core.Continuous;
using AbilityKit.Demo.Moba.Services.Buffs;
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
    public static class MobaRuntimeDependencyValidationRules
    {
        public const string AggregateSource = "runtime.dependencies";
        public const string CoreSource = "runtime.dependencies.core";
        public const string GameplaySource = "runtime.dependencies.gameplay";

        public static void Register(MobaRuntimeValidationReport report)
        {
            // 为保持编译稳定性而保留的占位实现。
        }
    }

    public sealed class MobaRuntimeCoreDependencyValidator : MobaRuntimeDependencyValidatorBase
    {
        public override string Name => "runtime.dependencies.core";
    }

    public sealed class MobaRuntimeSkillDependencyValidator : MobaRuntimeDependencyValidatorBase
    {
        public override string Name => "runtime.dependencies.skill";
    }

    public sealed class MobaRuntimeContinuousDependencyValidator : MobaRuntimeDependencyValidatorBase
    {
        public override string Name => "runtime.dependencies.continuous";
    }

    public sealed class MobaRuntimeCombatDependencyValidator : MobaRuntimeDependencyValidatorBase
    {
        public override string Name => "runtime.dependencies.combat";
    }

    public sealed class MobaRuntimeTemporaryEntityDependencyValidator : MobaRuntimeDependencyValidatorBase
    {
        public override string Name => "runtime.dependencies.temp_entity";
    }

    public sealed class MobaRuntimeOutputDependencyValidator : MobaRuntimeDependencyValidatorBase
    {
        public override string Name => "runtime.dependencies.output";
    }

    public sealed class MobaRuntimeDiagnosticsDependencyValidator : MobaRuntimeDependencyValidatorBase
    {
        public override string Name => "runtime.dependencies.diagnostics";
    }

    public abstract class MobaRuntimeDependencyValidatorBase : IMobaRuntimeValidator
    {
        public abstract string Name { get; }

        public virtual void Validate(in MobaRuntimeValidationContext context, MobaRuntimeValidationReport report)
        {
            MobaRuntimeDependencyValidationRules.Register(report);
        }
    }
}
