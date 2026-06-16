using AbilityKit.Protocol.Shooter;

namespace AbilityKit.Demo.Shooter.Runtime
{
    public interface IShooterBattleRuntimePort :
        IShooterGameStartPort,
        IShooterInputPort,
        IShooterSimulationClock,
        IShooterSnapshotReadPort,
        IShooterStateHashProvider,
        IShooterPackedSnapshotPort
    {
        bool TryGetPlayer(int playerId, out ShooterSveltoPlayerComponent player);

        void SetPlayer(in ShooterSveltoPlayerComponent player);
    }
}
