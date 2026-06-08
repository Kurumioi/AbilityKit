using AbilityKit.Protocol.Shooter;

namespace AbilityKit.Demo.Shooter.Runtime
{
    public interface IShooterBattleRuntimePort
    {
        bool IsStarted { get; }

        int CurrentFrame { get; }

        ShooterStartGamePayload StartSpec { get; }

        bool StartGame(in ShooterStartGamePayload spec);

        int SubmitInput(int frame, ShooterPlayerCommand[] commands);

        bool Tick(float deltaTime);

        ShooterStateSnapshotPayload GetSnapshot();
    }
}
