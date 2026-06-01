namespace AbilityKit.Demo.Moba.Services
{
    public readonly struct MobaEffectTraceScopeSnapshot
    {
        public MobaEffectTraceScopeSnapshot(long effectContextId, int effectConfigId, int triggerId, int sourceActorId, int targetActorId, bool isRoot)
        {
            EffectContextId = effectContextId;
            EffectConfigId = effectConfigId;
            TriggerId = triggerId;
            SourceActorId = sourceActorId;
            TargetActorId = targetActorId;
            IsRoot = isRoot;
        }

        public long EffectContextId { get; }
        public int EffectConfigId { get; }
        public int TriggerId { get; }
        public int SourceActorId { get; }
        public int TargetActorId { get; }
        public bool IsRoot { get; }
    }
}
