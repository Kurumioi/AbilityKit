namespace AbilityKit.Ability.Host.Extensions.Server.BattleHost
{
    public sealed class BattleSnapshotSyncPolicy
    {
        public BattleSnapshotSyncPolicy(int fullSnapshotInterval = 30)
        {
            FullSnapshotInterval = fullSnapshotInterval > 0 ? fullSnapshotInterval : 30;
        }

        public int FullSnapshotInterval { get; }

        public bool ShouldPublish(int observerCount, bool worldTicked)
        {
            return observerCount > 0 && worldTicked;
        }

        public bool ShouldCreateFullSnapshot(int frame)
        {
            return frame <= 0 || frame % FullSnapshotInterval == 0;
        }
    }
}
