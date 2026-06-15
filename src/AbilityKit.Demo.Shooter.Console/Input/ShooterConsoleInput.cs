using AbilityKit.Demo.Shooter.View.Hosting;
using AbilityKit.Protocol.Shooter;

namespace AbilityKit.Demo.Shooter.Console;

internal interface IShooterConsoleInputSource
{
    ShooterConsoleInputState Poll();
}

internal readonly struct ShooterConsoleInputState
{
    public ShooterConsoleInputState(float moveX, float moveY, float aimX, float aimY, bool fire, bool quit, bool pause, bool help)
    {
        MoveX = moveX;
        MoveY = moveY;
        AimX = aimX;
        AimY = aimY;
        Fire = fire;
        Quit = quit;
        Pause = pause;
        Help = help;
    }

    public float MoveX { get; }

    public float MoveY { get; }

    public float AimX { get; }

    public float AimY { get; }

    public bool Fire { get; }

    public bool Quit { get; }

    public bool Pause { get; }

    public bool Help { get; }

    public ShooterHostFrameInput ToHostInput(float fallbackAimX, float fallbackAimY)
    {
        var aimX = AimX;
        var aimY = AimY;
        if (aimX == 0f && aimY == 0f)
        {
            aimX = fallbackAimX;
            aimY = fallbackAimY;
        }

        return new ShooterHostFrameInput(MoveX, MoveY, aimX, aimY, Fire);
    }

    public ShooterPlayerCommand ToCommand(int playerId, float fallbackAimX, float fallbackAimY)
    {
        var hostInput = ToHostInput(fallbackAimX, fallbackAimY);
        return new ShooterPlayerCommand(playerId, hostInput.MoveX, hostInput.MoveY, hostInput.AimX, hostInput.AimY, hostInput.Fire);
    }
}
