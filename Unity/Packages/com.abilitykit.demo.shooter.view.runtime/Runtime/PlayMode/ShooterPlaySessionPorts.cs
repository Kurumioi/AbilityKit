#nullable enable

using AbilityKit.Demo.Shooter.View.Hosting;

namespace AbilityKit.Demo.Shooter.View.PlayMode
{
    public readonly struct ShooterPlayFrameInput
    {
        private readonly ShooterHostFrameInput _input;

        public ShooterPlayFrameInput(float moveX, float moveY, float aimX, float aimY, bool fire)
            : this(new ShooterHostFrameInput(moveX, moveY, aimX, aimY, fire))
        {
        }

        public ShooterPlayFrameInput(ShooterHostFrameInput input)
        {
            _input = input;
        }

        public float MoveX => _input.MoveX;
        public float MoveY => _input.MoveY;
        public float AimX => _input.AimX;
        public float AimY => _input.AimY;
        public bool Fire => _input.Fire;

        public ShooterHostFrameInput ToHostInput()
        {
            return _input;
        }
    }

    public interface IShooterPlayInputSource : IShooterHostInputSource
    {
        new ShooterPlayFrameInput ReadInput(int controlledPlayerId);

        ShooterHostFrameInput IShooterHostInputSource.ReadInput(int controlledPlayerId)
        {
            return ReadInput(controlledPlayerId).ToHostInput();
        }
    }

    public interface IShooterPlayViewSink : IShooterHostViewSink
    {
    }
}
