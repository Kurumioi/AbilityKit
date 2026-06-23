using System;

namespace AbilityKit.Game.Test.UnitTest
{
    [Serializable]
    public sealed class MobaAcceptanceExpectation
    {
        public string caseId;
        public string description;
        public string worldId;
        public int tickRate;
        public bool accelerated;
        public MobaAcceptanceInputExpectation input;
        public MobaAcceptanceConfigExpectation config;
        public MobaAcceptanceScenarioExpectation scenario;
        public MobaAcceptanceActorExpectation[] actors;
        public MobaAcceptanceSetupActionExpectation[] setupActions;
        public MobaAcceptanceTimelineStepExpectation[] timeline;
        public MobaAcceptanceStateExpectation[] stateExpectations;
        public MobaAcceptanceContextExpectation[] contextExpectations;
        public MobaAcceptanceTraceExpectation[] mustContain;
        public MobaAcceptanceTraceExpectation[] mustNotContain;
        public MobaAcceptanceRelationshipExpectation[] relationships;
    }

    [Serializable]
    public sealed class MobaAcceptanceScenarioExpectation
    {
        public string scenarioId;
        public string name;
        public string description;
        public string worldId;
        public int tickRate;
        public bool accelerated;
        public MobaAcceptanceActorExpectation[] actors;
        public MobaAcceptanceSetupActionExpectation[] setupActions;
        public MobaAcceptanceTimelineStepExpectation[] timeline;
        public MobaAcceptanceStateExpectation[] stateExpectations;
        public MobaAcceptanceContextExpectation[] contextExpectations;
    }

    [Serializable]
    public sealed class MobaAcceptanceActorExpectation
    {
        public string alias;
        public string actorId;
        public string playerId;
        public int teamId;
        public int heroId;
        public int attributeTemplateId;
        public int level;
        public int basicAttackSkillId;
        public int[] skillIds;
        public int spawnIndex;
        public int unitSubType;
        public int mainType;
        public bool hasSpawnPosition;
        public MobaAcceptanceVector3Expectation spawnPosition;
        public MobaAcceptanceVector3Expectation facingDirection;
        public int[] carriedSkillIds;
        public string configKey;
    }

    [Serializable]
    public sealed class MobaAcceptanceSetupActionExpectation
    {
        public string action;
        public string actorAlias;
        public string targetAlias;
        public string playerId;
        public int slot;
        public int skillId;
        public int targetActorId;
        public int durationMs;
        public bool enabled;
        public MobaAcceptanceVector3Expectation position;
        public MobaAcceptanceVector3Expectation direction;
        public string payload;
        public string note;
    }

    [Serializable]
    public sealed class MobaAcceptanceTimelineStepExpectation
    {
        public string stepId;
        public int atMs;
        public string action;
        public string actorAlias;
        public string targetAlias;
        public string playerId;
        public int slot;
        public int skillId;
        public int targetActorId;
        public int durationMs;
        public MobaAcceptanceVector3Expectation position;
        public MobaAcceptanceVector3Expectation direction;
        public string payload;
        public string note;
    }

    [Serializable]
    public sealed class MobaAcceptanceStateExpectation
    {
        public string alias;
        public string actorId;
        public string state;
        public string property;
        public string comparator;
        public string expectedValue;
        public float expectedFloat;
        public int expectedInt;
        public bool expectedBool;
        public MobaAcceptanceVector3Expectation expectedVector;
        public MobaAcceptanceVector3Expectation tolerance;
        public string note;
    }

    [Serializable]
    public sealed class MobaAcceptanceContextExpectation
    {
        public string alias;
        public string actorId;
        public string kind;
        public string property;
        public string comparator;
        public string expectedValue;
        public float expectedFloat;
        public int expectedInt;
        public bool expectedBool;
        public string note;
    }

    [Serializable]
    public sealed class MobaAcceptanceVector3Expectation
    {
        public float x;
        public float y;
        public float z;
    }

    [Serializable]
    public sealed class MobaAcceptanceInputExpectation
    {
        public string playerId;
        public string actorAlias;
        public string targetAlias;
        public int slot;
        public string phase;
        public MobaAcceptanceVector3Expectation position;
        public MobaAcceptanceVector3Expectation direction;
        public int targetActorId;
        public string note;
    }

    [Serializable]
    public sealed class MobaAcceptanceConfigExpectation
    {
        public int skillId;
        public int castFlowId;
        public int effectId;
        public int triggerId;
        public MobaAcceptanceActionExpectation[] expectedActions;
        public MobaAcceptanceProjectileExpectation expectedProjectile;
    }

    [Serializable]
    public sealed class MobaAcceptanceActionExpectation
    {
        public int actionId;
        public string type;
    }

    [Serializable]
    public sealed class MobaAcceptanceProjectileExpectation
    {
        public int launcherId;
        public int projectileId;
    }

    [Serializable]
    public sealed class MobaAcceptanceTraceExpectation
    {
        public string kind;
        public int configId;
        public int underEffectId;
    }

    [Serializable]
    public sealed class MobaAcceptanceRelationshipExpectation
    {
        public string parentKind;
        public int parentConfigId;
        public string childKind;
        public int childConfigId;
    }

    [Serializable]
    public sealed class MobaAcceptanceTraceRecord
    {
        public string caseId;
        public int frame;
        public int timeMs;
        public long rootId;
        public long parentId;
        public long nodeId;
        public string kind;
        public int kindValue;
        public int configId;
        public long sourceActorId;
        public long targetActorId;
        public long sourceId;
        public long targetId;
        public long originSourceId;
        public long originTargetId;
        public string originSource;
        public string originTarget;
        public bool isRoot;
        public bool isEnded;
        public int endedFrame;
        public int endReason;
        public int childCount;
    }

    [Serializable]
    public sealed class MobaAcceptanceSummary
    {
        public string caseId;
        public string worldId;
        public string expectationPath;
        public int tickRate;
        public bool accelerated;
        public MobaAcceptanceScenarioExpectation scenario;
        public MobaAcceptanceActorExpectation[] actors;
        public MobaAcceptanceSetupActionExpectation[] setupActions;
        public MobaAcceptanceTimelineStepExpectation[] timeline;
        public MobaAcceptanceStateExpectation[] stateExpectations;
        public MobaAcceptanceContextExpectation[] contextExpectations;
        public MobaAcceptanceInputExpectation input;
        public MobaAcceptanceConfigExpectation config;
        public MobaAcceptanceResult result;
        public MobaAcceptanceTraceCount[] traceCounts;
        public string traceJsonlPath;
        public string summaryJsonPath;
    }

    [Serializable]
    public sealed class MobaAcceptanceResult
    {
        public bool passed;
        public bool skillCastTraceFound;
        public bool effectExecutionTraceFound;
        public bool allExpectedActionsExecuted;
        public bool projectileLaunched;
        public long effectRootId;
        public int finalFrame;
        public int finalTimeMs;
        public int traceNodeCount;
    }

    [Serializable]
    public sealed class MobaAcceptanceTraceCount
    {
        public string kind;
        public int count;
    }

    [Serializable]
    public sealed class MobaAcceptanceCaseRunResult
    {
        public string caseId;
        public string expectationPath;
        public bool passed;
        public string errorType;
        public string errorMessage;
        public string startedUtc;
        public long durationMs;
        public MobaAcceptanceSummary summary;
    }

    [Serializable]
    public sealed class MobaAcceptanceBatchSummary
    {
        public string expectationDirectory;
        public string artifactDirectory;
        public bool recursive;
        public string startedUtc;
        public long durationMs;
        public int total;
        public int passed;
        public int failed;
        public bool allPassed;
        public MobaAcceptanceCaseRunResult[] results;
        public string batchSummaryJsonPath;
    }
}
