$ErrorActionPreference = 'Stop'
$dbPath = 'Unity/Packages/com.abilitykit.triggering/Runtime/Plan/Json/TriggerPlanJsonDatabase.cs'
$newPath = 'Unity/Packages/com.abilitykit.triggering/Runtime/Plan/Json/TriggerPlanModuleExpander.cs'
$csprojPath = 'Unity/AbilityKit.Triggering.csproj'

$db = [System.IO.File]::ReadAllText($dbPath)

$start = $db.IndexOf('        private static TriggerPlanDatabaseDto ExpandModuleInstances(TriggerPlanDatabaseDto dto)')
$endMarker = '        private static TriggerPlanScope NormalizeScope(TriggerPlanScope scope)'
$end = $db.IndexOf($endMarker, $start)
if ($start -lt 0 -or $end -lt 0) {
    throw 'Unable to locate TriggerPlanJsonDatabase module expansion block.'
}

$block = $db.Substring($start, $end - $start).TrimEnd()
$block = $block -replace 'private static TriggerPlanDatabaseDto ExpandModuleInstances\(TriggerPlanDatabaseDto dto\)', 'public static TriggerPlanDatabaseDto Expand(TriggerPlanDatabaseDto dto)'

$newContent = @"
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AbilityKit.Triggering.Runtime.Plan.Json
{
    using ActionCallPlanDto = TriggerPlanJsonDatabase.ActionCallPlanDto;
    using BoolExprNodeDto = TriggerPlanJsonDatabase.BoolExprNodeDto;
    using ExecutionControlPlanDto = TriggerPlanJsonDatabase.ExecutionControlPlanDto;
    using ExecutionNodeDto = TriggerPlanJsonDatabase.ExecutionNodeDto;
    using NumericValueRefDto = TriggerPlanJsonDatabase.NumericValueRefDto;
    using PredicatePlanDto = TriggerPlanJsonDatabase.PredicatePlanDto;
    using TemplateParameterDto = TriggerPlanJsonDatabase.TemplateParameterDto;
    using TriggerPlanDatabaseDto = TriggerPlanJsonDatabase.TriggerPlanDatabaseDto;
    using TriggerPlanDto = TriggerPlanJsonDatabase.TriggerPlanDto;
    using TriggerPlanModuleInstanceDto = TriggerPlanJsonDatabase.TriggerPlanModuleInstanceDto;
    using TriggerPlanModuleTemplateDto = TriggerPlanJsonDatabase.TriggerPlanModuleTemplateDto;
    using TriggerTemplateBindingDto = TriggerPlanJsonDatabase.TriggerTemplateBindingDto;

    /// <summary>
    /// 负责展开触发器计划模块/模板实例，避免 TriggerPlanJsonDatabase 同时承担 DTO 组装和模块实例化逻辑。
    /// </summary>
    internal static class TriggerPlanModuleExpander
    {
$block
    }
}
"@

[System.IO.File]::WriteAllText($newPath, $newContent, [System.Text.UTF8Encoding]::new($false))

$db = $db.Substring(0, $start) + $db.Substring($end)
$db = $db.Replace('dto = ExpandModuleInstances(dto);', 'dto = TriggerPlanModuleExpander.Expand(dto);')
[System.IO.File]::WriteAllText($dbPath, $db, [System.Text.UTF8Encoding]::new($false))

$csproj = [System.IO.File]::ReadAllText($csprojPath)
$include = '    <Compile Include="Packages\com.abilitykit.triggering\Runtime\Plan\Json\TriggerPlanModuleExpander.cs" />'
if ($csproj -notlike '*TriggerPlanModuleExpander.cs*') {
    $anchor = '    <Compile Include="Packages\com.abilitykit.triggering\Runtime\Plan\Json\TriggerPlanJsonDatabase.cs" />'
    $csproj = $csproj.Replace($anchor, $anchor + "`r`n" + $include)
    [System.IO.File]::WriteAllText($csprojPath, $csproj, [System.Text.UTF8Encoding]::new($true))
}
