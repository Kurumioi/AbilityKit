$ErrorActionPreference = 'Stop'

$root = Join-Path (Get-Location) 'Unity/Packages/com.abilitykit.triggering/Runtime'
$unityRoot = Join-Path (Get-Location) 'Unity'
$projectFiles = @(
    (Join-Path (Get-Location) 'Unity/AbilityKit.Triggering.csproj'),
    (Join-Path (Get-Location) 'Unity/AbilityKit.Triggering.Tests.csproj')
)

function Move-TriggeringFile {
    param(
        [Parameter(Mandatory=$true)][string]$From,
        [Parameter(Mandatory=$true)][string]$To
    )

    $source = Join-Path $root $From
    $target = Join-Path $root $To
    $targetDir = Split-Path $target -Parent

    if (-not (Test-Path $source)) {
        throw "Missing source file: $source"
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

    $script:Replacements += [pscustomobject]@{
        From = "Packages\com.abilitykit.triggering\Runtime\" + ($From -replace '/', '\')
        To = "Packages\com.abilitykit.triggering\Runtime\" + ($To -replace '/', '\')
    }
}

$script:Replacements = @()

# Batch 1: remove confirmed empty placeholder directories that have no source files.
$emptyDirs = @('Data', 'TriggerScheduler', 'Example')
foreach ($dir in $emptyDirs) {
    $full = Join-Path $root $dir
    if (Test-Path $full) {
        $items = Get-ChildItem -LiteralPath $full -Force -Recurse
        if ($items.Count -eq 0) {
            Remove-Item -LiteralPath $full -Force
        }
    }
}

# Batch 2: Runtime/Runtime -> Core + Triggering.
$coreExecution = @(
    'ContextAccessor.cs',
    'DefaultTCtx.cs',
    'ExecCtx.cs',
    'ExecCtxExtensions.cs',
    'ExecPolicy.cs',
    'ExecutionControl.cs'
)
foreach ($file in $coreExecution) { Move-TriggeringFile "Runtime/$file" "Core/Execution/$file" }
Move-TriggeringFile 'IActionResolver.cs' 'Core/Execution/IActionResolver.cs'

$coreIdentity = @('IIdNameRegistry.cs', 'IdNameRegistry.cs')
foreach ($file in $coreIdentity) { Move-TriggeringFile "Runtime/$file" "Core/Identity/$file" }

$coreContext = @(
    'CompositeContextSource.cs',
    'ICueParams.cs',
    'ITriggerContextSource.cs',
    'TriggerContext.cs',
    'TriggerCueContext.cs'
)
foreach ($file in $coreContext) { Move-TriggeringFile "Runtime/$file" "Core/Context/$file" }

$triggerContracts = @(
    'ETriggerShortCircuitReason.cs',
    'ILegacyTriggerExecutor.cs',
    'ITrigger.cs',
    'ITriggerCue.cs',
    'ITriggerLifecycle.cs',
    'ITriggerObserver.cs'
)
foreach ($file in $triggerContracts) { Move-TriggeringFile "Runtime/$file" "Triggering/Contracts/$file" }

$triggerRunner = @(
    'TriggerRunner.cs',
    'TriggerRunnerCueDispatcher.cs',
    'TriggerRunnerEntry.cs',
    'TriggerRunnerRegistration.cs',
    'TriggerRunnerRuntimeServices.cs'
)
foreach ($file in $triggerRunner) { Move-TriggeringFile "Runtime/$file" "Triggering/Runner/$file" }
Move-TriggeringFile 'Runtime/HierarchicalTriggerRunner.cs' 'Triggering/Hierarchy/HierarchicalTriggerRunner.cs'

$triggerImpl = @(
    'CompiledTrigger.cs',
    'DelegateTrigger.cs',
    'NullTriggerCue.cs',
    'TriggerLifecycleImplementations.cs'
)
foreach ($file in $triggerImpl) { Move-TriggeringFile "Runtime/$file" "Triggering/Implementations/$file" }

# Batch 3: Schedule + RuleScheduler + ActionScheduler -> Scheduling.
$schedulingCommon = @('IScheduleManager.cs', 'ScheduleHandle.cs')
foreach ($file in $schedulingCommon) { Move-TriggeringFile "Schedule/$file" "Scheduling/Common/$file" }
Move-TriggeringFile 'Schedule/SimpleScheduleManager.cs' 'Scheduling/Simple/SimpleScheduleManager.cs'

$schedulingGrouped = @('GroupedScheduleIndex.cs', 'GroupedScheduleManager.cs', 'GroupedScheduleStore.cs')
foreach ($file in $schedulingGrouped) { Move-TriggeringFile "Schedule/$file" "Scheduling/Grouped/$file" }

$schedulingRules = @('RuleScheduleEntry.cs', 'RuleScheduler.cs', 'RuleSchedulerRegistry.cs', 'RuleSchedulerTypes.cs')
foreach ($file in $schedulingRules) { Move-TriggeringFile "RuleScheduler/$file" "Scheduling/Rules/$file" }

$schedulingActions = @('ActionExecutor.cs', 'ActionInstance.cs', 'ActionScheduler.cs', 'ActionSchedulerManager.cs')
foreach ($file in $schedulingActions) { Move-TriggeringFile "ActionScheduler/$file" "Scheduling/Actions/$file" }

$schedulingData = @('ScheduleData.cs', 'ScheduleItemData.cs', 'ScheduleItemState.cs', 'ScheduleRegisterRequest.cs')
foreach ($file in $schedulingData) { Move-TriggeringFile "Schedule/Data/$file" "Scheduling/Data/$file" }

Move-TriggeringFile 'Schedule/EffectExecutorAdapter.cs' 'Scheduling/Effects/EffectExecutorAdapter.cs'
Move-TriggeringFile 'Schedule/Behavior/EffectExecutor.cs' 'Scheduling/Effects/EffectExecutor.cs'

$schedulingStrategies = @(
    'DefaultScheduleStrategy.cs',
    'IScheduleEffect.cs',
    'IScheduleStrategy.cs',
    'SchedulableBehaviorScheduleAdapter.cs',
    'ScheduleContext.cs',
    'ScheduleToBehaviorContextAdapter.cs'
)
foreach ($file in $schedulingStrategies) { Move-TriggeringFile "Schedule/Behavior/$file" "Scheduling/Strategies/$file" }

# Batch 4: Plan -> Plans.
$plansModel = @(
    'ActionArgs.cs',
    'ActionCallPlan.cs',
    'ActionCallPlanExtensions.cs',
    'IntValueRef.cs',
    'PlannedTrigger.cs',
    'PredicateExprPlan.cs',
    'RpnIntExprPlan.cs',
    'RpnIntExprRuntime.cs',
    'ScheduleModePlan.cs',
    'TriggerDelegates.cs',
    'TriggerExecutionControlPlan.cs',
    'TriggerPlan.cs'
)
foreach ($file in $plansModel) { Move-TriggeringFile "Plan/$file" "Plans/Model/$file" }

$plansBuilders = @(
    'ActionCallPlanFactory.cs',
    'NumericValueRefDsl.cs',
    'NumericValueRefResolver.cs',
    'PredicateExprDsl.cs',
    'RpnIntExprEval.cs',
    'RpnIntExprParser.cs',
    'TriggerPlanDsl.cs',
    'TriggerPlanFactory.cs'
)
foreach ($file in $plansBuilders) { Move-TriggeringFile "Plan/$file" "Plans/Builders/$file" }

$plansExecution = @(
    'ActionSchemaRegistry.cs',
    'NamedArgsPlanActionModuleBase.cs',
    'PlannedTriggerActionBindingResolver.cs',
    'PlannedTriggerActionExecutor.cs',
    'PlannedTriggerArgumentResolver.cs',
    'PlannedTriggerPredicateEvaluator.cs',
    'PlannedTriggerScheduleRegistrar.cs',
    'TriggerRunnerPlanExtensions.cs'
)
foreach ($file in $plansExecution) { Move-TriggeringFile "Plan/$file" "Plans/Execution/$file" }

Move-TriggeringFile 'Plan/Attributes/AutoPlanAction.cs' 'Plans/Attributes/AutoPlanAction.cs'

$planExecutables = @(
    'ActionCallTriggerPlanExecutable.cs',
    'CompositeTriggerPlanExecutableBase.cs',
    'ETriggerPlanExecutableKind.cs',
    'ETriggerPlanExecutionStatus.cs',
    'FailTriggerPlanExecutable.cs',
    'IfTriggerPlanExecutable.cs',
    'InvertTriggerPlanExecutable.cs',
    'ITriggerPlanCondition.cs',
    'ITriggerPlanExecutable.cs',
    'MetadataTriggerPlanExecutable.cs',
    'ParallelTriggerPlanExecutable.cs',
    'PredicateExprTriggerPlanCondition.cs',
    'RandomTriggerPlanExecutable.cs',
    'RepeatTriggerPlanExecutable.cs',
    'ScheduledTriggerPlanExecutable.cs',
    'SelectorTriggerPlanExecutable.cs',
    'SequenceTriggerPlanExecutable.cs',
    'SucceedTriggerPlanExecutable.cs',
    'TriggerPlanExecutableBase.cs',
    'TriggerPlanExecutableDsl.cs',
    'TriggerPlanExecutionResult.cs',
    'UntilTriggerPlanExecutable.cs'
)
foreach ($file in $planExecutables) { Move-TriggeringFile "Plan/Executables/$file" "Plans/Executables/$file" }

$planJsonLoading = @(
    'ITriggerPlanDirectoryLoader.cs',
    'TriggerPlanDirectoryLoader.cs',
    'TriggerPlanDirectoryLoadOptions.cs'
)
foreach ($file in $planJsonLoading) { Move-TriggeringFile "Plan/Json/$file" "Plans/Serialization/Json/Loading/$file" }

$planJsonParsing = @(
    'TriggerPlanJsonParser.cs',
    'TriggerPlanJsonParseResult.cs',
    'TriggerPlanSourceParser.cs'
)
foreach ($file in $planJsonParsing) { Move-TriggeringFile "Plan/Json/$file" "Plans/Serialization/Json/Parsing/$file" }

$planJsonConversion = @(
    'TriggerPlanConverter.cs',
    'TriggerPlanExecutionNodeConverter.cs',
    'TriggerPlanExecutionNodeReferenceResolver.cs',
    'TriggerPlanModuleExpander.cs'
)
foreach ($file in $planJsonConversion) { Move-TriggeringFile "Plan/Json/$file" "Plans/Serialization/Json/Conversion/$file" }

$planJsonSource = @(
    'TriggerPlanSourceActionWriter.cs',
    'TriggerPlanSourceBehaviorResolver.cs',
    'TriggerPlanSourceConverter.Conditions.cs',
    'TriggerPlanSourceConverter.cs',
    'TriggerPlanSourceConverter.Helpers.cs',
    'TriggerPlanSourceConverter.SourceDtos.cs',
    'TriggerPlanSourceExecutionControlWriter.cs',
    'TriggerPlanSourceExecutionNodeOptionWriter.cs',
    'TriggerPlanSourceExecutionNodeShape.cs',
    'TriggerPlanSourceExecutionNodeWriter.cs',
    'TriggerPlanSourceFragmentResolver.cs',
    'TriggerPlanSourceTriggerShape.cs'
)
foreach ($file in $planJsonSource) { Move-TriggeringFile "Plan/Json/$file" "Plans/Serialization/Json/Source/$file" }

$planJsonDatabase = @(
    'TriggerPlanJsonDatabase.cs',
    'ValidatingTriggerPlanJsonDatabase.cs'
)
foreach ($file in $planJsonDatabase) { Move-TriggeringFile "Plan/Json/$file" "Plans/Serialization/Json/Database/$file" }

# Update explicit project compile paths.
foreach ($projectFile in $projectFiles) {
    if (-not (Test-Path $projectFile)) { continue }
    $text = [System.IO.File]::ReadAllText($projectFile)
    foreach ($replacement in $script:Replacements) {
        $text = $text.Replace($replacement.From, $replacement.To)
    }
    [System.IO.File]::WriteAllText($projectFile, $text, [System.Text.UTF8Encoding]::new($false))
}

Write-Host "Moved $($script:Replacements.Count) source files and updated project references."
