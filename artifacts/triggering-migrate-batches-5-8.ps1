$ErrorActionPreference = 'Stop'

$workspace = Get-Location
$root = Join-Path $workspace 'Unity/Packages/com.abilitykit.triggering/Runtime'
$projectFiles = @(
    (Join-Path $workspace 'Unity/AbilityKit.Triggering.csproj'),
    (Join-Path $workspace 'Unity/AbilityKit.Triggering.Tests.csproj')
)

$script:Replacements = @()
$script:MovedCount = 0

function Move-TriggeringAsset {
    param(
        [Parameter(Mandatory=$true)][string]$From,
        [Parameter(Mandatory=$true)][string]$To,
        [bool]$CompileItem = $true
    )

    $source = Join-Path $root $From
    $target = Join-Path $root $To
    $targetDir = Split-Path $target -Parent

    if (-not (Test-Path $source)) {
        if (Test-Path $target) {
            Write-Host "Already moved: $To"
            return
        }

        throw "Missing source asset: $source"
    }

    if (-not (Test-Path $targetDir)) {
        New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
    }

    Move-Item -LiteralPath $source -Destination $target -Force

    $sourceMeta = "$source.meta"
    $targetMeta = "$target.meta"
    if (Test-Path $sourceMeta) {
        Move-Item -LiteralPath $sourceMeta -Destination $targetMeta -Force
    }

    if ($CompileItem -and $From.EndsWith('.cs')) {
        $script:Replacements += [pscustomobject]@{
            From = "Packages\com.abilitykit.triggering\Runtime\" + ($From -replace '/', '\')
            To = "Packages\com.abilitykit.triggering\Runtime\" + ($To -replace '/', '\')
        }
    }

    $script:MovedCount++
}

# 第5批：Events 与 Registries 收口。
Move-TriggeringAsset 'Eventing/EventBus.cs' 'Events/EventBus.cs'
Move-TriggeringAsset 'Eventing/EventBusOptions.cs' 'Events/EventBusOptions.cs'
Move-TriggeringAsset 'Eventing/EventChannel.cs' 'Events/EventChannel.cs'
Move-TriggeringAsset 'Eventing/EventSchemaRegistry.cs' 'Events/EventSchemaRegistry.cs'
Move-TriggeringAsset 'Eventing/IEventBus.cs' 'Events/IEventBus.cs'
Move-TriggeringAsset 'Eventing/StableStringId.cs' 'Events/StableStringId.cs'

Move-TriggeringAsset 'Registry/ActionId.cs' 'Registries/ActionId.cs'
Move-TriggeringAsset 'Registry/ActionRegistry.cs' 'Registries/ActionRegistry.cs'
Move-TriggeringAsset 'Registry/FunctionId.cs' 'Registries/FunctionId.cs'
Move-TriggeringAsset 'Registry/FunctionRegistry.cs' 'Registries/FunctionRegistry.cs'
Move-TriggeringAsset 'Registry/TriggerRegistry.cs' 'Registries/TriggerRegistry.cs'

# 第6批：Blackboard、Payload、Variables 内部分层。
Move-TriggeringAsset 'Blackboard/IBlackboard.cs' 'Blackboard/Core/IBlackboard.cs'
Move-TriggeringAsset 'Blackboard/DictionaryBlackboard.cs' 'Blackboard/Core/DictionaryBlackboard.cs'
Move-TriggeringAsset 'Blackboard/IBlackboardResolver.cs' 'Blackboard/Resolvers/IBlackboardResolver.cs'
Move-TriggeringAsset 'Blackboard/IBlackboardDomainResolver.cs' 'Blackboard/Resolvers/IBlackboardDomainResolver.cs'
Move-TriggeringAsset 'Blackboard/DictionaryBlackboardResolver.cs' 'Blackboard/Resolvers/DictionaryBlackboardResolver.cs'
Move-TriggeringAsset 'Blackboard/DictionaryBlackboardDomainResolver.cs' 'Blackboard/Resolvers/DictionaryBlackboardDomainResolver.cs'
Move-TriggeringAsset 'Blackboard/BlackboardSchema.cs' 'Blackboard/Schema/BlackboardSchema.cs'
Move-TriggeringAsset 'Blackboard/BlackboardKeyMeta.cs' 'Blackboard/Schema/BlackboardKeyMeta.cs'
Move-TriggeringAsset 'Blackboard/BlackboardKeyRef.cs' 'Blackboard/Schema/BlackboardKeyRef.cs'
Move-TriggeringAsset 'Blackboard/BlackboardKeyRegistry.cs' 'Blackboard/Schema/BlackboardKeyRegistry.cs'
Move-TriggeringAsset 'Blackboard/BlackboardKeyType.cs' 'Blackboard/Schema/BlackboardKeyType.cs'
Move-TriggeringAsset 'Blackboard/BlackboardIdMapper.cs' 'Blackboard/Mapping/BlackboardIdMapper.cs'
Move-TriggeringAsset 'Blackboard/BlackboardIdNameRegistryExtensions.cs' 'Blackboard/Mapping/BlackboardIdNameRegistryExtensions.cs'
Move-TriggeringAsset 'Blackboard/BlackboardNameUtil.cs' 'Blackboard/Mapping/BlackboardNameUtil.cs'

Move-TriggeringAsset 'Payload/IPayloadDoubleAccessor.cs' 'Payload/Accessors/IPayloadDoubleAccessor.cs'
Move-TriggeringAsset 'Payload/IPayloadIntAccessor.cs' 'Payload/Accessors/IPayloadIntAccessor.cs'
Move-TriggeringAsset 'Payload/PayloadAccessorRegistry.cs' 'Payload/Registry/PayloadAccessorRegistry.cs'
Move-TriggeringAsset 'Payload/PayloadAccessorResolver.cs' 'Payload/Registry/PayloadAccessorResolver.cs'

Move-TriggeringAsset 'Variables/Numeric/INumericVarDomain.cs' 'Variables/Numeric/Core/INumericVarDomain.cs'
Move-TriggeringAsset 'Variables/Numeric/INumericVarDomainRegistry.cs' 'Variables/Numeric/Core/INumericVarDomainRegistry.cs'
Move-TriggeringAsset 'Variables/Numeric/DefaultNumericVarDomainRegistry.cs' 'Variables/Numeric/Core/DefaultNumericVarDomainRegistry.cs'
Move-TriggeringAsset 'Variables/Numeric/NumericVarDomainRegistry.cs' 'Variables/Numeric/Core/NumericVarDomainRegistry.cs'
Move-TriggeringAsset 'Variables/Numeric/NumericVarRef.cs' 'Variables/Numeric/Core/NumericVarRef.cs'
Move-TriggeringAsset 'Variables/Numeric/NumericValueSourceRuntime.cs' 'Variables/Numeric/Core/NumericValueSourceRuntime.cs'
Move-TriggeringAsset 'Variables/Numeric/ExecCtxNumericVarExtensions.cs' 'Variables/Numeric/Core/ExecCtxNumericVarExtensions.cs'
Move-TriggeringAsset 'Variables/Numeric/NumericValueRefExtensions.cs' 'Variables/Numeric/Core/NumericValueRefExtensions.cs'
Move-TriggeringAsset 'Variables/Numeric/Expression/DefaultNumericRpnFunctionRegistry.cs' 'Variables/Numeric/Expressions/DefaultNumericRpnFunctionRegistry.cs'
Move-TriggeringAsset 'Variables/Numeric/Expression/DefaultNumericRpnFunctions.cs' 'Variables/Numeric/Expressions/DefaultNumericRpnFunctions.cs'
Move-TriggeringAsset 'Variables/Numeric/Expression/INumericRpnFunction.cs' 'Variables/Numeric/Expressions/INumericRpnFunction.cs'
Move-TriggeringAsset 'Variables/Numeric/Expression/INumericRpnFunctionRegistry.cs' 'Variables/Numeric/Expressions/INumericRpnFunctionRegistry.cs'
Move-TriggeringAsset 'Variables/Numeric/Expression/NumericExpressionCompiler.cs' 'Variables/Numeric/Expressions/NumericExpressionCompiler.cs'
Move-TriggeringAsset 'Variables/Numeric/Expression/NumericExpressionEvaluator.cs' 'Variables/Numeric/Expressions/NumericExpressionEvaluator.cs'
Move-TriggeringAsset 'Variables/Numeric/Expression/NumericRpnFunctionRegistry.cs' 'Variables/Numeric/Expressions/NumericRpnFunctionRegistry.cs'
Move-TriggeringAsset 'Variables/Numeric/Expression/NumericRpnProgram.cs' 'Variables/Numeric/Expressions/NumericRpnProgram.cs'
Move-TriggeringAsset 'Variables/Numeric/Expression/NumericRpnToken.cs' 'Variables/Numeric/Expressions/NumericRpnToken.cs'
Move-TriggeringAsset 'Variables/Numeric/Expression/NumericRpnTokenEvaluator.cs' 'Variables/Numeric/Expressions/NumericRpnTokenEvaluator.cs'
Move-TriggeringAsset 'Extensions/RpnNumericExprParserExtensions.cs' 'Variables/Numeric/Expressions/RpnNumericExprParserExtensions.cs'
Move-TriggeringAsset 'Extensions/AccessorAttributes.cs' 'Payload/Accessors/AccessorAttributes.cs'
Move-TriggeringAsset 'Extensions/AccessorRegistry.cs' 'Payload/Registry/AccessorRegistry.cs'

# 第7批：Executables 与 Behaviors 收口。
Move-TriggeringAsset 'Executable/IExecutable.cs' 'Executables/Core/IExecutable.cs'
Move-TriggeringAsset 'Executable/Executor.cs' 'Executables/Core/Executor.cs'
Move-TriggeringAsset 'Executable/ExecutableRegistry.cs' 'Executables/Core/ExecutableRegistry.cs'
Move-TriggeringAsset 'Executable/ExecutableTriggerDatabase.cs' 'Executables/Core/ExecutableTriggerDatabase.cs'
Move-TriggeringAsset 'Executable/ExecutableDto.cs' 'Executables/Conversion/ExecutableDto.cs'
Move-TriggeringAsset 'Executable/ExecutableConverterStrategies.cs' 'Executables/Conversion/ExecutableConverterStrategies.cs'
Move-TriggeringAsset 'Executable/ExecutableConverterStrategyRegistry.cs' 'Executables/Conversion/ExecutableConverterStrategyRegistry.cs'
Move-TriggeringAsset 'Executable/ActionDelegateFactory.cs' 'Executables/Conversion/ActionDelegateFactory.cs'
Move-TriggeringAsset 'Executable/ContextAdapter.cs' 'Executables/Conversion/ContextAdapter.cs'
Move-TriggeringAsset 'Executable/AtomicExecutables.cs' 'Executables/Composition/AtomicExecutables.cs'
Move-TriggeringAsset 'Executable/CompositeExecutables.cs' 'Executables/Composition/CompositeExecutables.cs'
Move-TriggeringAsset 'Executable/ScheduledExecutables.cs' 'Executables/Composition/ScheduledExecutables.cs'
Move-TriggeringAsset 'Executable/ECompositeMode.cs' 'Executables/Composition/ECompositeMode.cs'
Move-TriggeringAsset 'Executable/DecoratorContract.cs' 'Executables/Decorators/DecoratorContract.cs'
Move-TriggeringAsset 'Executable/DefaultDecorators.cs' 'Executables/Decorators/DefaultDecorators.cs'
Move-TriggeringAsset 'Executable/Decorators/ComposableInterfaces.cs' 'Executables/Decorators/ComposableInterfaces.cs'
Move-TriggeringAsset 'Executable/Decorators/DecoratorBuilder.cs' 'Executables/Decorators/DecoratorBuilder.cs'
Move-TriggeringAsset 'Executable/Decorators/DecoratorDsl.cs' 'Executables/Decorators/DecoratorDsl.cs'
Move-TriggeringAsset 'Executable/ExtensionPoints/DecoratorRegistry.cs' 'Executables/Decorators/DecoratorRegistry.cs'
Move-TriggeringAsset 'Executable/ModifierApplier.cs' 'Executables/Modifiers/ModifierApplier.cs'
Move-TriggeringAsset 'Executable/TypeIdRegistry.cs' 'Executables/Metadata/TypeIdRegistry.cs'
Move-TriggeringAsset 'Executable/IHasPayload.cs' 'Executables/Metadata/IHasPayload.cs'

Move-TriggeringAsset 'Behavior/IBehaviorContext.cs' 'Behaviors/Core/IBehaviorContext.cs'
Move-TriggeringAsset 'Behavior/BehaviorExecutionResult.cs' 'Behaviors/Core/BehaviorExecutionResult.cs'
Move-TriggeringAsset 'Behavior/ResolvedValue.cs' 'Behaviors/Core/ResolvedValue.cs'
Move-TriggeringAsset 'Behavior/TriggerBehavior.cs' 'Behaviors/Core/TriggerBehavior.cs'
Move-TriggeringAsset 'Behavior/Actions/ActionBehavior.cs' 'Behaviors/Actions/ActionBehavior.cs'
Move-TriggeringAsset 'Behavior/Predicates/AutoPredicate.cs' 'Behaviors/Predicates/AutoPredicate.cs'
Move-TriggeringAsset 'Behavior/Predicates/PredicateBehaviors.cs' 'Behaviors/Predicates/PredicateBehaviors.cs'
Move-TriggeringAsset 'Behavior/Composite/CompositeBehaviors.cs' 'Behaviors/Composite/CompositeBehaviors.cs'
Move-TriggeringAsset 'Behavior/Schedule/TriggerBehavior.cs' 'Behaviors/Scheduling/TriggerBehavior.cs'
Move-TriggeringAsset 'Factory/BehaviorFactory.cs' 'Behaviors/Factory/BehaviorFactory.cs'

# 第8批：Config、Validation、RuntimeServices 收口。
Move-TriggeringAsset 'Validation/ITriggerValidator.cs' 'Validation/Core/ITriggerValidator.cs'
Move-TriggeringAsset 'Validation/ValidationContext.cs' 'Validation/Core/ValidationContext.cs'
Move-TriggeringAsset 'Validation/ValidationResult.cs' 'Validation/Core/ValidationResult.cs'
Move-TriggeringAsset 'Validation/TriggerPlanValidation.cs' 'Validation/Core/TriggerPlanValidation.cs'
Move-TriggeringAsset 'Validation/ActionCallPlanValidator.cs' 'Plans/Validation/ActionCallPlanValidator.cs'
Move-TriggeringAsset 'Validation/CompositeTriggerValidator.cs' 'Plans/Validation/CompositeTriggerValidator.cs'
Move-TriggeringAsset 'Validation/CycleDetectorValidator.cs' 'Plans/Validation/CycleDetectorValidator.cs'
Move-TriggeringAsset 'Validation/ExecutionRootValidator.cs' 'Plans/Validation/ExecutionRootValidator.cs'
Move-TriggeringAsset 'Validation/ReferenceValidator.cs' 'Plans/Validation/ReferenceValidator.cs'
Move-TriggeringAsset 'Validation/RuleSchedulePlanValidator.cs' 'Plans/Validation/RuleSchedulePlanValidator.cs'
Move-TriggeringAsset 'Validation/TriggerPlanDatabase.cs' 'Plans/Validation/TriggerPlanDatabase.cs'
Move-TriggeringAsset 'Validation/TriggerPlanExecutableValidator.cs' 'Plans/Validation/TriggerPlanExecutableValidator.cs'
Move-TriggeringAsset 'Validation/UgcLimitsValidator.cs' 'Plans/Validation/UgcLimitsValidator.cs'
Move-TriggeringAsset 'Validation/RuntimeCompatibilityCatalog.cs' 'Validation/Compatibility/RuntimeCompatibilityCatalog.cs'

Move-TriggeringAsset 'Continuous/ContinuousExecutorAdapter.cs' 'RuntimeServices/Continuous/ContinuousExecutorAdapter.cs'
Move-TriggeringAsset 'Continuous/ContinuousExecutorBase.cs' 'RuntimeServices/Continuous/ContinuousExecutorBase.cs'
Move-TriggeringAsset 'Continuous/ProcessUnit.cs' 'RuntimeServices/Continuous/ProcessUnit.cs'
Move-TriggeringAsset 'Diagnostics/TriggeringDiagnosticCollector.cs' 'RuntimeServices/Diagnostics/TriggeringDiagnosticCollector.cs'
Move-TriggeringAsset 'Random/DeterministicRandom.cs' 'RuntimeServices/Random/DeterministicRandom.cs'
Move-TriggeringAsset 'Random/IRandomProvider.cs' 'RuntimeServices/Random/IRandomProvider.cs'
Move-TriggeringAsset 'Sync/ISyncedTriggerExecutor.cs' 'RuntimeServices/Sync/ISyncedTriggerExecutor.cs'
Move-TriggeringAsset 'Sync/ITriggerSyncService.cs' 'RuntimeServices/Sync/ITriggerSyncService.cs'
Move-TriggeringAsset 'Sync/SyncedTriggerExecutor.cs' 'RuntimeServices/Sync/SyncedTriggerExecutor.cs'
Move-TriggeringAsset 'Sync/TriggerSyncService.cs' 'RuntimeServices/Sync/TriggerSyncService.cs'
Move-TriggeringAsset 'Time/IFrameClock.cs' 'RuntimeServices/Time/IFrameClock.cs'
Move-TriggeringAsset 'Time/UnityFrameClock.cs' 'RuntimeServices/Time/UnityFrameClock.cs'
Move-TriggeringAsset 'Dispatcher/ITriggerDispatcher.cs' 'RuntimeServices/Dispatcher/ITriggerDispatcher.cs'
Move-TriggeringAsset 'Dispatcher/README.md' 'RuntimeServices/Dispatcher/README.md' $false
Move-TriggeringAsset 'Instance/ITriggerInstance.cs' 'RuntimeServices/Instance/ITriggerInstance.cs'
Move-TriggeringAsset 'Instance/TriggerInstanceManager.cs' 'RuntimeServices/Instance/TriggerInstanceManager.cs'
Move-TriggeringAsset 'Instance/TriggerState.cs' 'RuntimeServices/Instance/TriggerState.cs'
Move-TriggeringAsset 'Compatibility/RootRuntimeCompatibilityCatalog.cs' 'RuntimeServices/Compatibility/RootRuntimeCompatibilityCatalog.cs'
Move-TriggeringAsset 'Compatibility/README.md' 'RuntimeServices/Compatibility/README.md' $false

foreach ($projectFile in $projectFiles) {
    if (-not (Test-Path $projectFile)) { continue }

    $text = [System.IO.File]::ReadAllText($projectFile)
    foreach ($replacement in $script:Replacements) {
        $text = $text.Replace($replacement.From, $replacement.To)
    }

    [System.IO.File]::WriteAllText($projectFile, $text, [System.Text.UTF8Encoding]::new($false))
}

Write-Host "Moved $($script:MovedCount) assets and updated $($script:Replacements.Count) compile references."
