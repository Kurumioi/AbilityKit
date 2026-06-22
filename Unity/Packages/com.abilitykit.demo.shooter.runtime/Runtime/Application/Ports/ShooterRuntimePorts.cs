using AbilityKit.Protocol.Shooter;

namespace AbilityKit.Demo.Shooter.Runtime
{
    public interface IShooterGameStartPort
    {
        bool IsStarted { get; }

        ShooterBattleMatchState MatchState { get; }

        ShooterMatchResultSnapshot MatchResult { get; }

        ShooterStartGamePayload StartSpec { get; }

        bool StartGame(in ShooterStartGamePayload spec);
    }

    public interface IShooterInputPort
    {
        int SubmitInput(int frame, ShooterPlayerCommand[] commands);
    }

    public interface IShooterSimulationClock
    {
        int CurrentFrame { get; }

        bool Tick(float deltaTime);
    }

    public interface IShooterSnapshotReadPort
    {
        ShooterStateSnapshotPayload GetSnapshot();
    }

    public interface IShooterStateHashProvider
    {
        uint ComputeStateHash();
    }

    public interface IShooterPackedSnapshotPort
    {
        ShooterPackedSnapshotPayload ExportPackedSnapshot(ulong worldId, bool isFullSnapshot = true, bool authorityOverride = false);

        bool ImportPackedSnapshot(in ShooterPackedSnapshotPayload snapshot);

        byte[] ExportPackedSnapshotBytes(ulong worldId, bool isFullSnapshot = true, bool authorityOverride = false);

        bool ImportPackedSnapshotBytes(byte[] payload);
    }

    public interface IShooterPureStateSnapshotPort
    {
        ShooterPureStateSnapshotPayload ExportPureStateSnapshot(
            ulong worldId,
            bool isFullBaseline = true,
            ShooterPureStateSyncSettings? settings = null,
            int baselineFrame = 0,
            uint baselineHash = 0,
            ShooterPureStateInterestScope? interestScope = null);
    }

    public readonly struct ShooterPureStateInterestScope
    {
        public ShooterPureStateInterestScope(int observerPlayerId, float centerX, float centerY, float radius, int maxEntities = 0)
        {
            ObserverPlayerId = observerPlayerId;
            CenterX = centerX;
            CenterY = centerY;
            Radius = radius;
            MaxEntities = maxEntities;
        }

        public int ObserverPlayerId { get; }

        public float CenterX { get; }

        public float CenterY { get; }

        public float Radius { get; }

        public int MaxEntities { get; }

        public bool HasRadius => Radius > 0f;
    }
}
