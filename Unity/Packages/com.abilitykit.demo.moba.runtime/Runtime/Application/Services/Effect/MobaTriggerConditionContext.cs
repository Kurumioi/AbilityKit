using System;

namespace AbilityKit.Demo.Moba.Services
{
    public readonly struct MobaTriggerConditionContext
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
            MobaSkillCastRuntimeService skillRuntimes,
            int frame)
        {
            _payload = payload;
            _dataContext = dataContext;
            _skillRuntimes = skillRuntimes;
            LineageInput = lineageInput;
            Origin = origin;
            ExecutionSnapshot = executionSnapshot;
            SkillRuntimeHandle = skillRuntimeHandle;
            Frame = frame != 0 ? frame : executionSnapshot.Frame;
        }

        public object Payload => _payload;
        public MobaEffectLineageInput LineageInput { get; }
        public MobaEffectTraceInput TraceInput => LineageInput.ToTraceInput();
        public MobaGameplayOrigin Origin { get; }
        public MobaTriggerExecutionSnapshot ExecutionSnapshot { get; }
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
        public int StackCount => ExecutionSnapshot.StackCount;
        public float ElapsedSeconds => ExecutionSnapshot.ElapsedSeconds;
        public float RemainingSeconds => ExecutionSnapshot.RemainingSeconds;
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

        public static MobaTriggerConditionContext Create(
            object payload,
            in MobaEffectLineageInput lineageInput,
            in MobaTriggerExecutionSnapshot executionSnapshot,
            MobaSkillCastRuntimeService skillRuntimes,
            int frame)
        {
            var origin = default(MobaGameplayOrigin);
            payload.TryResolveOrigin(out origin);

            var builder = MobaTriggerExecutionSnapshotBuilder.Create()
                .FromLineage(in lineageInput)
                .FromPayload(payload)
                .FromSnapshot(in executionSnapshot);

            if (frame != 0)
            {
                builder.WithFrame(frame);
            }

            var snapshot = builder.Build();
            var handle = origin.SkillRuntimeHandle;
            if (!handle.IsValid && snapshot.SkillRuntimeHandle.IsValid)
            {
                handle = snapshot.SkillRuntimeHandle;
            }

            var dataContext = payload as IMobaTriggerDataContext;
            return new MobaTriggerConditionContext(payload, lineageInput, origin, snapshot, handle, dataContext, skillRuntimes, frame);
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
