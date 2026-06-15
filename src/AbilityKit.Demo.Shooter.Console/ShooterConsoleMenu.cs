using AbilityKit.Demo.Host.Console;
using AbilityKit.Demo.Shooter.View;

namespace AbilityKit.Demo.Shooter.Console;

internal sealed class ShooterConsoleMenu
{
    private readonly IConsoleOutput _output;

    public ShooterConsoleMenu(IConsoleOutput output)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
    }

    public void Write(ShooterConsoleOptions options)
    {
        if (!options.IsValid)
        {
            _output.Write(ConsoleOutputChannel.Error, $"ERROR {options.Error}");
        }

        _output.Write(ConsoleOutputChannel.System, "Shooter Console Acceptance Host");
        _output.Write(ConsoleOutputChannel.System, "AI-oriented command menu:");
        _output.Write(ConsoleOutputChannel.System, "  --mode spec --spec basic-combat");
        _output.Write(ConsoleOutputChannel.System, "  --mode acceptance --sync predict-rollback --network ideal --frames 120 --seed 0");
        _output.Write(ConsoleOutputChannel.System, "  --mode matrix --frames 120 --seed 0");
        _output.Write(ConsoleOutputChannel.System, "  --mode remote-smoke");
        _output.Write(ConsoleOutputChannel.System, "  --mode local --render");
        _output.Write(ConsoleOutputChannel.System, "Switches: --headless --render --authoritative --no-authoritative --help --menu");
        _output.Write(ConsoleOutputChannel.System, "Specs: basic-combat");
        _output.Write(ConsoleOutputChannel.System, "Sync modes:");

        foreach (var sync in ShooterAcceptanceCatalog.SyncModes)
        {
            var id = ShooterConsoleOptions.ToSyncId(sync.Model);
            _output.Write(ConsoleOutputChannel.System, $"  {id} implemented={sync.Implemented} display=\"{sync.DisplayName}\"");
        }

        _output.Write(ConsoleOutputChannel.System, "Networks:");
        foreach (var network in ShooterAcceptanceCatalog.NetworkEnvironments)
        {
            _output.Write(ConsoleOutputChannel.System, $"  {network.Id} display=\"{network.DisplayName}\"");
        }

        _output.Write(ConsoleOutputChannel.System, "Remote smoke runs the canonical Orleans TCP Gateway Shooter smoke project.");
        _output.Write(ConsoleOutputChannel.System, "Machine-readable outputs use RESULT, CASE, SUMMARY, and ERROR tokens.");
    }
}
