namespace AbilityKit.Demo.Moba.Services
{
    /// <summary>
    /// Boundary helpers for context-source models. These helpers make call sites state whether they are consuming
    /// an execution-time model, a diagnostic/query view, or a persistence-safe snapshot.
    /// </summary>
    public static class MobaContextSourceBoundaryGuards
    {
        public static bool IsExecutionBoundary(this in MobaContextSourceView source)
        {
            return source.Boundary == MobaContextSourceBoundary.Execution;
        }

        public static bool IsSnapshotBoundary(this in MobaContextSourceView source)
        {
            return source.Boundary == MobaContextSourceBoundary.Snapshot;
        }

        public static bool IsLiveRuntimeBoundary(this in MobaContextSourceView source)
        {
            return source.Boundary == MobaContextSourceBoundary.LiveRuntime;
        }

        public static bool CanCreateDownstreamExecution(this in MobaContextSourceView source)
        {
            return source.HasExecutionSource && source.ParentContextId != 0;
        }

        public static MobaPersistentContextSourceSnapshot ToPersistentSnapshot(this in MobaContextSourceView source)
        {
            return MobaPersistentContextSourceSnapshotFactory.FromContextSource(in source);
        }

        public static bool TryToPersistentSnapshot(this in MobaContextSourceView source, out MobaPersistentContextSourceSnapshot snapshot)
        {
            snapshot = source.IsValid
                ? MobaPersistentContextSourceSnapshotFactory.FromContextSource(in source)
                : default;
            return snapshot.IsValid;
        }

        public static bool TryAsExecutionSource(this in MobaContextSourceView source, out MobaCombatExecutionContext context)
        {
            context = default;
            if (!source.CanCreateDownstreamExecution()) return false;

            var snapshot = MobaPersistentContextSourceSnapshotFactory.FromContextSource(in source);
            return snapshot.TryGetCombatContextSource(out var combatSource)
                   && MobaCombatContextBuilder.TryFromSource(snapshot, in combatSource, out context)
                   && context.HasExecutionSource;
        }
    }
}
