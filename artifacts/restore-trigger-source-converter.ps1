$ErrorActionPreference = 'Stop'
$path = 'Unity/Packages/com.abilitykit.triggering/Runtime/Plan/Json/TriggerPlanSourceConverter.cs'
$lines = & git show HEAD:Unity/Packages/com.abilitykit.triggering/Runtime/Plan/Json/TriggerPlanSourceConverter.cs
$content = $lines -join "`r`n"

$content = [regex]::Replace($content, 'public sealed class TriggerPlanSourceConverter\r?\n    \{', 'public sealed class TriggerPlanSourceConverter' + "`r`n" + '    {' + "`r`n" + '        private readonly TriggerPlanSourceConditionWriter _conditionWriter = new TriggerPlanSourceConditionWriter();')

$content = [regex]::Replace($content, '(?s)\r?\n        private static string ReadString\(JObject obj, params string\[\] aliases\).*?\r?\n        private static JObject GetExecutionRootSource', "`r`n" + '        private static JObject GetExecutionRootSource')
$content = [regex]::Replace($content, '(?s)\r?\n        private void WritePredicate\(JsonTextWriter writer, List<JObject> conditions\).*?\r?\n        private void WriteExecutionNode', "`r`n" + '        private void WriteExecutionNode')
$content = [regex]::Replace($content, '(?s)\r?\n        private static bool IsKind\(string kind, params string\[\] values\).*?\r?\n        /// <summary>\r?\n        /// 源格式 JSON 结构\r?\n        /// </summary>\r?\n#pragma warning disable 0649.*?\r?\n#pragma warning restore 0649', '')

$content = $content -replace 'WritePredicate\(writer, ResolveConditionList\(([^;]+)\)\);', '_conditionWriter.WritePredicate(writer, ResolveConditionList($1), WriteParamValue);'
$content = $content -replace 'WritePredicate\(writer, conditionNodes\);', '_conditionWriter.WritePredicate(writer, conditionNodes, WriteParamValue);'
$content = $content -replace 'WritePredicate\(writer, untilConditions\);', '_conditionWriter.WritePredicate(writer, untilConditions, WriteParamValue);'

$content = $content -replace '\bReadString\(', 'TriggerPlanSourceJsonUtility.ReadString('
$content = $content -replace '\bReadInt\(', 'TriggerPlanSourceJsonUtility.ReadInt('
$content = $content -replace '\bReadFloat\(', 'TriggerPlanSourceJsonUtility.ReadFloat('
$content = $content -replace '\bIsKind\(', 'TriggerPlanSourceJsonUtility.IsKind('
$content = $content -replace '\bReadConditionItems\(', 'TriggerPlanSourceConditionWriter.ReadConditionItems('
$content = $content -replace '\bWriteConstValue\(', 'TriggerPlanSourceValueWriter.WriteConstValue('
$content = $content -replace '\bWritePayloadFieldValue\(', 'TriggerPlanSourceValueWriter.WritePayloadFieldValue('
$content = $content -replace '\bWriteVarValue\(', 'TriggerPlanSourceValueWriter.WriteVarValue('
$content = $content -replace '\bWriteExprValue\(', 'TriggerPlanSourceValueWriter.WriteExprValue('

[System.IO.File]::WriteAllText($path, $content + "`r`n", [System.Text.UTF8Encoding]::new($false))
