using System;

namespace AbilityKit.Demo.Moba.Services
{
    public readonly struct MobaTriggerConditionContext : IMobaTriggerStageSnapshotProvider
    {
        private readonly object _payload;
        private readonly IMobaTriggerDataContext _dataContext;
        private readonly MobaSkillCastRuntimeService _skillRuntimes;

        private MobaTriggerConditionContext(
            object payload,
            MobaEffectLineageInput lineageInput,
            MobaGameplayOrigin origin,
            MobaTriggerExecutionSnapshot executionSnapshot,
            MobaSkillCastRuntimeHandle skillRuntimeHandle,
            IMobaTriggerDataContext dataContext,
            MobaTriggerStageSnapshot stageSnapshot,
            MobaSkillCastRuntimeService skillRuntimes,
            int frame)
        {
            _payload = payload;
            _dataContext = dataContext;
            _skillRuntimes = skillRuntimes;
            LineageInput = lineageInput;
            Origin = origin;
            ExecutionSnapshot = executionSnapshot;
            StageSnapshot = stageSnapshot;
            SkillRuntimeHandle = skillRuntimeHandle;
            Frame = frame != 0 ? frame : executionSnapshot.Frame;
        }

        public object Payload => _payload;
        public MobaEffectLineageInput LineageInput { get; }
        public MobaEffectTraceInput TraceInput => LineageInput.ToTraceInput();
        public MobaGameplayOrigin Origin { get; }
        public MobaTriggerExecutionSnapshot ExecutionSnapshot { get; }
        public MobaTriggerStageSnapshot StageSnapshot { get; }
        public MobaSkillCastRuntimeHandle SkillRuntimeHandle { get; }
        public int Frame { get; }
        public EffectContextKind ContextKind => LineageInput.ContextKind != EffectContextKind.Unknown ? LineageInput.ContextKind : ExecutionSnapshot.Kind;
        public MobaTraceKind OriginKind => LineageInput.OriginKind;
        public int SourceActorId => LineageInput.SourceActorId != 0 ? LineageInput.SourceActorId : Origin.SourceActorId != 0 ? Origin.SourceActorId : ExecutionSnapshot.SourceActorId;
        public int TargetActorId => LineageInput.TargetActorId != 0 ? LineageInput.TargetActorId : Origin.TargetActorId != 0 ? Origin.TargetActorId : ExecutionSnapshot.TargetActorId;
        public long ParentContextId => LineageInput.ParentContextId != 0 ? LineageInput.ParentContextId : Origin.EffectiveParentContextId != 0 ? Origin.EffectiveParentContextId : ExecutionSnapshot.SourceContextId;
        public long RootContextId => LineageInput.EffectiveRootContextId != 0 ? LineageInput.EffectiveRootContextId : Origin.EffectiveRootContextId != 0 ? Origin.EffectiveRootContextId : ExecutionSnapshot.EffectiveRootContextId;
        public long OwnerContextId => LineageInput.OwnerKey != 0 ? LineageInput.OwnerKey : Origin.OwnerContextId != 0 ? Origin.OwnerContextId : ExecutionSnapshot.OwnerContextId;
        public int TriggerId => ExecutionSnapshot.TriggerId;
        public int ConfigId => ExecutionSnapshot.ConfigId;
        public int StackCount => StageSnapshot.StackCount;
        public float ElapsedSeconds => StageSnapshot.ElapsedSeconds;
        public float RemainingSeconds => StageSnapshot.RemainingSeconds;
        public float DurationSeconds => StageSnapshot.DurationSeconds;
        public bool HasSkillRuntime => SkillRuntimeHandle.IsValid;

        public bool TryGetPayload<TPayload>(out TPayload payload)
        {
            if (_payload is TPayload typed)
            {
                payload = typed;
                return true;
            }

            payload = default;
            return false;
        }

        public bool TryGetData<T>(AbilityContextKeys key, out T value)
        {
            value = default;
            return _dataContext != null && _dataContext.TryGetData(key.ToKeyString(), out value);
        }

        public T GetData<T>(AbilityContextKeys key, T defaultValue = default)
        {
            return _dataContext != null ? _dataContext.GetData(key.ToKeyString(), defaultValue) : defaultValue;
        }

        public bool TryGetBlackboard(out MobaSkillRuntimeBlackboard blackboard)
        {
            blackboard = null;
            if (!SkillRuntimeHandle.IsValid || _skillRuntimes == null) return false;
            var handle = SkillRuntimeHandle;
            return _skillRuntimes.TryGetBlackboard(in handle, out blackboard);
        }

        public bool HasDamagedTarget(int actorId)
        {
            return TryGetBlackboard(out var blackboard) && blackboard.ContainsActorId(in MobaSkillRuntimeBlackboardKeys.DamagedTargets, actorId);
        }

        public int GetHitCount()
        {
            if (!TryGetBlackboard(out var blackboard)) return 0;
            return blackboard.TryGet(in MobaSkillRuntimeBlackboardKeys.HitCount, out var value) ? value.IntValue : 0;
        }

        public bool HasLoopGuard(long contextId)
        {
            return TryGetBlackboard(out var blackboard) && blackboard.ContainsContextId(in MobaSkillRuntimeBlackboardKeys.LoopGuards, contextId);
        }

        public MobaTriggerExecutionRequest ToExecutionRequest(int triggerId)
        {
            return new MobaTriggerExecutionRequest(
                triggerId,
                Frame,
                RootContextId,
                ParentContextId,
                SourceActorId,
                TargetActorId,
                ContextKind,
                OriginKind);
        }

        public bool TryGetExecutionSnapshot(out MobaTriggerExecutionSnapshot snapshot)
        {
            snapshot = ExecutionSnapshot;
            return snapshot.IsValid;
        }

        public bool TryGetStageSnapshot(out MobaTriggerStageSnapshot snapshot)
        {
            snapshot = StageSnapshot;
            return snapshot.IsValid;
        }

        public static MobaTriggerConditionContext Create(
            object payload,
            in MobaEffectLineageInput lineageInput,
            in MobaTriggerExecutionSnapshot executionSnapshot,
            MobaSkillCastRuntimeService skillRuntimes,
            int frame)
        {
            var executionContext = MobaCombatExecutionContextFactory.Create(payload, in lineageInput, in executionSnapshot, frame);
            return Create(in executionContext, skillRuntimes, frame);
        }

        public static MobaTriggerConditionContext Create(
            in MobaCombatExecutionContext executionContext,
            MobaSkillCastRuntimeService skillRuntimes,
            int frame)
        {
            var payload = executionContext.Payload;
            var dataContext = payload as IMobaTriggerDataContext;
            payload.TryResolveStageSnapshot(out var stageSnapshot);
            return new MobaTriggerConditionContext(
                payload,
                executionContext.LineageInput,
                executionContext.Origin,
                executionContext.ExecutionSnapshot,
                executionContext.SkillRuntimeHandle,
                dataContext,
                stageSnapshot,
                skillRuntimes,
                frame != 0 ? frame : executionContext.Frame);
        }

        public static MobaTriggerConditionContext Create(
            object payload,
            in MobaEffectTraceInput traceInput,
            in MobaTriggerExecutionSnapshot executionSnapshot,
            MobaSkillCastRuntimeService skillRuntimes,
            int frame)
        {
            var lineageInput = traceInput.ToLineageInput();
            return Create(payload, in lineageInput, in executionSnapshot, skillRuntimes, frame);
        }

        public static MobaTriggerConditionContext Create(
            object payload,
            in MobaEffectLineageInput lineageInput,
            MobaSkillCastRuntimeService skillRuntimes,
            int frame)
        {
            var snapshot = default(MobaTriggerExecutionSnapshot);
            return Create(payload, in lineageInput, in snapshot, skillRuntimes, frame);
        }

        public static MobaTriggerConditionContext Create(
            object payload,
            in MobaEffectTraceInput traceInput,
            MobaSkillCastRuntimeService skillRuntimes,
            int frame)
        {
            var lineageInput = traceInput.ToLineageInput();
            return Create(payload, in lineageInput, skillRuntimes, frame);
        }
    }
}
