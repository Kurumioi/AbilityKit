namespace AbilityKit.Orleans.Gateway.HttpApi;

internal static class GatewaySkillAnalysisModelProvider
{
    private const string ModelVersion = "skill-analysis-v1";

    private static readonly string[] Sources =
    {
        "runtime-diagnostics",
        "scenario-acceptance-artifacts"
    };

    private static readonly SkillAnalysisStageDefinition[] StageDefinitions =
    {
        new(
            "cast-pipeline",
            "技能释放管线",
            "SkillPipelineContext / SkillPipelineRunner",
            "summary.result + trace kind SkillCast",
            new[] { "skillId", "casterActorId", "targetActorId", "frame", "castSequence", "runtimeId", "sourceContextId", "pipelineState", "failReason" }),
        new(
            "trigger-lineage",
            "触发链路溯源",
            "MobaTriggerLineageContext / MobaTraceRuntimeServices",
            "trace rootId + parentId + nodeId",
            new[] { "nodeId", "rootId", "parentId", "kind", "configId", "frame", "timeMs", "isEnded", "childCount" }),
        new(
            "effect-execution",
            "效果执行",
            "EffectExecutionTraceScope / MobaEffectTraceScopeSnapshot / MobaEffectInvokerService",
            "trace kind EffectExecution + result.effectExecutionTraceFound",
            new[] { "effectId", "contextId", "actionContextIds", "targetActorId", "value", "endedFrame" }),
        new(
            "assertion-result",
            "验收断言结果",
            "Runtime assertions projected by test harness",
            "stateExpectations + contextExpectations + traceExpectations",
            new[] { "passed", "finalFrame", "finalTimeMs", "traceNodeCount", "allExpectedActionsExecuted", "projectileLaunched" })
    };

    private static readonly SkillAnalysisFieldDefinition[] CorrelationFieldDefinitions =
    {
        new("battleId", "运行态战斗实例标识；用于把后台实时诊断和 artifact case 关联到同一场战斗。", true),
        new("worldId", "逻辑世界标识；用于隔离多房间、多环境、多批次结果。", true),
        new("caseId", "Scenario artifact 稳定复现入口；真实运行态可为空。", false),
        new("rootId", "Trace chain 根节点；用于从技能释放追踪到触发、效果、动作。", true),
        new("nodeId", "Trace 节点唯一标识；用于构建树和定位异常节点。", true),
        new("parentId", "父 Trace 节点；用于构建 tree/DAG lineage。", true),
        new("frame", "逻辑帧；用于对齐运行时事件、Scenario timeline 和最终断言。", true),
        new("actorId", "技能释放者、目标或上下文所属 Actor。", true),
        new("skillId", "技能配置标识；用于聚合技能维度指标。", true)
    };

    private static readonly SkillAnalysisProjectionSchemaDefinition[] ProjectionSchemaDefinitions =
    {
        new(
            "analysis-node-v1",
            "统一 Trace Node 投影",
            "AdminConsole tree、inspector、timeline 共用的最小节点 schema，可由 Scenario JSONL 或 runtime trace sink 填充。",
            new[] { "nodeId", "rootId", "parentId", "kind", "stage", "label", "configId", "frame", "timeMs", "actorId", "skillId", "sourceActorId", "targetActorId", "sourceContextId", "rootContextId", "ownerContextId", "entityKind", "entityKey", "status", "severity", "summary", "source", "failure", "raw" }),
        new(
            "analysis-filter-v1",
            "高密度效果过滤投影",
            "用于在大量战斗效果中按文本、severity、stage、kind、实体类型、Actor、Skill、Config、Root 和 Context 快速定位目标链路。",
            new[] { "searchText", "severity", "stage", "kind", "entityKind", "actorId", "skillId", "configId", "rootId", "contextId", "onlyFailures" }),
        new(
            "analysis-entity-relation-v1",
            "战斗实体关联投影",
            "用于把主战斗实体与 Projectile、AOE、Buff、Damage、Presentation、EffectAction 等派生实体按 owner/root/source context 聚合展示。",
            new[] { "ownerKey", "entityKind", "entityKey", "label", "nodeCount", "failureCount", "firstFrame", "lastFrame", "nodes" }),
        new(
            "analysis-edge-v1",
            "Trace Lineage Edge 投影",
            "用于把 root/parent/node 关系显式化，后续可扩展为 DAG 与跨 root 关联。",
            new[] { "fromNodeId", "toNodeId", "edgeType", "label", "source" }),
        new(
            "analysis-timeline-event-v1",
            "时间轴事件投影",
            "用于按 frame/timeMs 将技能释放、触发、效果、断言结果与 runtime events 对齐。",
            new[] { "id", "frame", "timeMs", "lane", "label", "nodeId", "severity", "source" })
    };

    private static readonly string[] Notes =
    {
        "Scenario artifact viewer and runtime skill diagnostics now share the same conceptual model: context, lineage, effect and assertion.",
        "High-density battle analysis should start from filters first: failure, entityKind, actorId, configId, rootId and contextId are the primary narrowing dimensions.",
        "Entity relation projection groups main actors with projectile, area, buff, damage, presentation and effect-action children through source/root/owner context.",
        "The current runtime endpoint still returns TraceNotConnected placeholders until the MOBA runtime trace sink is projected into Orleans/Gateway.",
        "Future live traces should emit the same correlation fields as acceptance JSONL so the Web UI can switch between replay artifact and live battle views."
    };

    public static AdminSkillAnalysisModelHttpResponse GetModel()
    {
        return new AdminSkillAnalysisModelHttpResponse(
            ModelVersion,
            Sources.ToArray(),
            StageDefinitions.Select(stage => new AdminSkillAnalysisStageHttpResponse(
                stage.Id,
                stage.DisplayName,
                stage.RuntimeSource,
                stage.AcceptanceSource,
                stage.Fields.ToArray())).ToArray(),
            CorrelationFieldDefinitions.Select(field => new AdminSkillAnalysisFieldHttpResponse(
                field.Name,
                field.Description,
                field.RequiredForCorrelation)).ToArray(),
            ProjectionSchemaDefinitions.Select(schema => new AdminSkillAnalysisProjectionSchemaHttpResponse(
                schema.Id,
                schema.DisplayName,
                schema.Description,
                schema.Fields.ToArray())).ToArray(),
            Notes.ToArray(),
            DateTime.UtcNow.Ticks);
    }

    private sealed record SkillAnalysisStageDefinition(string Id, string DisplayName, string RuntimeSource, string AcceptanceSource, string[] Fields);

    private sealed record SkillAnalysisFieldDefinition(string Name, string Description, bool RequiredForCorrelation);

    private sealed record SkillAnalysisProjectionSchemaDefinition(string Id, string DisplayName, string Description, string[] Fields);
}
