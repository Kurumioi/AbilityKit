#nullable enable

namespace AbilityKit.Demo.Shooter.View.PlayMode
{
    public sealed class ShooterNullPlayViewSink : IShooterPlayViewSink
    {
        public static ShooterNullPlayViewSink Shared { get; } = new();

        public void Render(in ShooterPlayPresentationFrame frame)
        {
        }

        public void Clear()
        {
        }
    }
}
