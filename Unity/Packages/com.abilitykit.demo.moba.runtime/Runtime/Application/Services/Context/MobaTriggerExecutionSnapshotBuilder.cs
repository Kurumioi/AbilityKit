namespace AbilityKit.Demo.Moba.Services
{
    public sealed class MobaTriggerExecutionSnapshotBuilder
    {
        private EffectContextKind _kind;
        private int _sourceActorId;
        private int _targetActorId;
        private long _sourceContextId;
        private long _rootContextId;
        private long _ownerContextId;
        private int _triggerId;
        private int _configId;
        private int _frame;
        private MobaSkillCastRuntimeHandle _skillRuntimeHandle;
        private int _stackCount;
        private float _elapsedSeconds;
        private float _remainingSeconds;

        private MobaTriggerExecutionSnapshotBuilder()
        {
        }

        public static MobaTriggerExecutionSnapshotBuilder Create()
        {
            return new MobaTriggerExecutionSnapshotBuilder();
        }

        public MobaTriggerExecutionSnapshotBuilder FromLineage(in MobaEffectLineageInput lineageInput)
        {
            if (lineageInput.ContextKind != EffectContextKind.Unknown) _kind = lineageInput.ContextKind;
            if (lineageInput.SourceActorId != 0) _sourceActorId = lineageInput.SourceActorId;
            if (lineageInput.TargetActorId != 0) _targetActorId = lineageInput.TargetActorId;
            if (lineageInput.ParentContextId != 0) _sourceContextId = lineageInput.ParentContextId;
            if (lineageInput.EffectiveRootContextId != 0) _rootContextId = lineageInput.EffectiveRootContextId;
            if (lineageInput.OwnerKey != 0) _ownerContextId = lineageInput.OwnerKey;
            if (lineageInput.OriginConfigId != 0) _configId = lineageInput.OriginConfigId;
            return this;
        }

        public MobaTriggerExecutionSnapshotBuilder FromPayload(object payload)
        {
            if (payload == null) return this;

            if (payload.TryResolveExecutionSnapshot(out var snapshot))
            {
                FromSnapshot(in snapshot);
            }

            if (payload is IMobaTriggerSkillRuntimeContext skillRuntimeContext
                && skillRuntimeContext.TryGetSkillRuntimeHandle(out var handle)
                && handle.IsValid)
            {
                _skillRuntimeHandle = handle;
            }

            if (payload is IMobaTriggerDataContext dataContext)
            {
                if (!_skillRuntimeHandle.IsValid
                    && dataContext.TryGetData(AbilityContextKeys.SkillRuntimeHandle.ToKeyString(), out MobaSkillCastRuntimeHandle dataHandle)
                    && dataHandle.IsValid)
                {
                    _skillRuntimeHandle = dataHandle;
                }

                if (_stackCount == 0 && dataContext.TryGetData(AbilityContextKeys.BuffStackCount.ToKeyString(), out int stackCount))
                {
                    _stackCount = stackCount;
                }

                if (_frame == 0 && dataContext.TryGetData(AbilityContextKeys.Frame.ToKeyString(), out int frame))
                {
                    _frame = frame;
                }
            }

            return this;
        }

        public MobaTriggerExecutionSnapshotBuilder FromSnapshot(in MobaTriggerExecutionSnapshot snapshot)
        {
            if (!snapshot.IsValid) return this;
            if (snapshot.Kind != EffectContextKind.Unknown) _kind = snapshot.Kind;
            if (snapshot.SourceActorId != 0) _sourceActorId = snapshot.SourceActorId;
            if (snapshot.TargetActorId != 0) _targetActorId = snapshot.TargetActorId;
            if (snapshot.SourceContextId != 0) _sourceContextId = snapshot.SourceContextId;
            if (snapshot.RootContextId != 0) _rootContextId = snapshot.RootContextId;
            if (snapshot.OwnerContextId != 0) _ownerContextId = snapshot.OwnerContextId;
            if (snapshot.TriggerId != 0) _triggerId = snapshot.TriggerId;
            if (snapshot.ConfigId != 0) _configId = snapshot.ConfigId;
            if (snapshot.Frame != 0) _frame = snapshot.Frame;
            if (snapshot.SkillRuntimeHandle.IsValid) _skillRuntimeHandle = snapshot.SkillRuntimeHandle;
            if (snapshot.StackCount != 0) _stackCount = snapshot.StackCount;
            if (snapshot.ElapsedSeconds != 0f) _elapsedSeconds = snapshot.ElapsedSeconds;
            if (snapshot.RemainingSeconds != 0f) _remainingSeconds = snapshot.RemainingSeconds;
            return this;
        }

        public MobaTriggerExecutionSnapshotBuilder WithTrigger(int triggerId, int configId)
        {
            if (triggerId != 0) _triggerId = triggerId;
            if (configId != 0) _configId = configId;
            return this;
        }

        public MobaTriggerExecutionSnapshotBuilder WithFrame(int frame)
        {
            if (frame != 0) _frame = frame;
            return this;
        }

        public MobaTriggerExecutionSnapshotBuilder WithFrameIfMissing(int frame)
        {
            if (_frame == 0 && frame != 0) _frame = frame;
            return this;
        }

        public MobaTriggerExecutionSnapshot Build()
        {
            return new MobaTriggerExecutionSnapshot(
                _kind,
                _sourceActorId,
                _targetActorId,
                _sourceContextId,
                _rootContextId,
                _ownerContextId,
                _triggerId,
                _configId,
                _frame,
                _skillRuntimeHandle,
                _stackCount,
                _elapsedSeconds,
                _remainingSeconds);
        }
    }
}
