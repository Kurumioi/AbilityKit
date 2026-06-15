namespace AbilityKit.Game.Flow
{
    public enum BattleViewEventSourceMode
    {
        SnapshotOnly = 0,
        TriggerOnly = 1,
        Hybrid = 2,
    }

    public enum BattleSyncMode
    {
        Lockstep = 0,
        SnapshotAuthority = 1,
        HybridPredictReconcile = 2,
    }
}
