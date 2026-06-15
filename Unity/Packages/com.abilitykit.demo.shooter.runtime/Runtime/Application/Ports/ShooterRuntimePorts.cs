using AbilityKit.Protocol.Shooter;

namespace AbilityKit.Demo.Shooter.Runtime
{
    public interface IShooterGameStartPort
    {
        bool IsStarted { get; }

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
}
