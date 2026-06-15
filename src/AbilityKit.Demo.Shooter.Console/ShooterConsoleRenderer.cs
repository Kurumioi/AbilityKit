using AbilityKit.Demo.Host.Console;
using AbilityKit.Demo.Shooter.View;
using AbilityKit.Demo.Shooter.View.Hosting;
using AbilityKit.Protocol.Shooter;
using System.Text;

namespace AbilityKit.Demo.Shooter.Console;

internal sealed class ShooterConsoleRenderer
{
    private const int Width = 41;
    private const int Height = 17;
    private const float WorldMinX = -8f;
    private const float WorldMaxX = 8f;
    private const float WorldMinY = -4f;
    private const float WorldMaxY = 4f;

    private readonly IConsoleOutput _output;
    private readonly ShooterSnapshotViewAdapter _viewAdapter;
    private readonly ShooterProjectedSnapshotViewSink _projectionSink;

    private uint _stateHash;
    private bool _paused;

    public ShooterConsoleRenderer(IConsoleOutput output)
        : this(output, new ShooterSnapshotViewAdapter(), new ShooterSnapshotViewProjection())
    {
    }

    public ShooterConsoleRenderer(IConsoleOutput output, ShooterSnapshotViewAdapter viewAdapter, ShooterSnapshotViewProjection projection)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
        _viewAdapter = viewAdapter ?? throw new ArgumentNullException(nameof(viewAdapter));
        _projectionSink = new ShooterProjectedSnapshotViewSink(
            projection ?? throw new ArgumentNullException(nameof(projection)),
            new ConsoleProjectedViewSink(this));
    }

    public void RenderHelp()
    {
        _output.WriteTitle(ConsoleOutputChannel.System, "Shooter Console Controls");
        _output.Write(ConsoleOutputChannel.Input, "Move: W/A/S/D");
        _output.Write(ConsoleOutputChannel.Input, "Aim : Arrow keys");
        _output.Write(ConsoleOutputChannel.Input, "Fire: Space");
        _output.Write(ConsoleOutputChannel.Input, "Pause: P    Help: H    Quit: Q or Esc");
    }

    public void Render(ShooterStateSnapshotPayload snapshot, uint stateHash, bool paused)
    {
        var batch = _viewAdapter.ApplySnapshot(in snapshot, ShooterViewBatchSource.LocalPrediction);
        RenderBatch(in batch, stateHash, paused);
    }

    public void Render(in ShooterHostPresentationFrame frame, uint stateHash, bool paused)
    {
        var clientBatch = frame.ClientBatch;
        RenderBatch(in clientBatch, stateHash, paused);
    }

    private void RenderBatch(in ShooterSnapshotViewBatch batch, uint stateHash, bool paused)
    {
        _stateHash = stateHash;
        _paused = paused;
        _projectionSink.ApplySnapshot(in batch);
    }

    private void RenderProjectedView(
        ShooterViewEntityStore store,
        in ShooterSnapshotViewBatch sourceBatch,
        in ShooterViewProjectionApplyResult applyResult)
    {
        var grid = CreateGrid();

        PlotEntities(grid, store);

        _output.Clear();
        _output.Write(ConsoleOutputChannel.View, $"Shooter Console | Frame {sourceBatch.Frame} | Hash 0x{_stateHash:X8} | {(_paused ? "PAUSED" : "RUNNING")}");
        _output.Write(ConsoleOutputChannel.View, $"World X[{WorldMinX:0},{WorldMaxX:0}] Y[{WorldMinY:0},{WorldMaxY:0}]  Players={applyResult.FinalPlayerCount} Bullets={applyResult.FinalBulletCount} Events={sourceBatch.Events.Count}");
        _output.Write(ConsoleOutputChannel.View, BuildGrid(grid));
        _output.Write(ConsoleOutputChannel.Battle, BuildPlayers(store));
        _output.Write(ConsoleOutputChannel.Battle, BuildBullets(store));
        _output.Write(ConsoleOutputChannel.Battle, BuildEvents(sourceBatch.Events));
        _output.Write(ConsoleOutputChannel.Input, "Controls: WASD move | Arrows aim | Space fire | P pause | H help | Q/Esc quit");
    }

    private static char[,] CreateGrid()
    {
        var grid = new char[Height, Width];
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                grid[y, x] = '.';
            }
        }

        return grid;
    }

    private static void PlotEntities(char[,] grid, ShooterViewEntityStore store)
    {
        foreach (var entity in store.Entities.Values.OrderBy(entity => entity.Kind).ThenBy(entity => entity.EntityId))
        {
            if (!entity.Alive || !store.TryGetTransform(entity.Key, out var transform))
            {
                continue;
            }

            if (!TryMap(transform.X, transform.Y, out var x, out var y))
            {
                continue;
            }

            grid[y, x] = entity.Kind == ShooterViewEntityKind.Player
                ? GetPlayerMarker(entity.EntityId)
                : '*';
        }
    }

    private static char GetPlayerMarker(int playerId)
    {
        return playerId >= 0 && playerId <= 9 ? (char)('0' + playerId) : 'P';
    }

    private static bool TryMap(float worldX, float worldY, out int gridX, out int gridY)
    {
        gridX = 0;
        gridY = 0;

        if (worldX < WorldMinX || worldX > WorldMaxX || worldY < WorldMinY || worldY > WorldMaxY)
        {
            return false;
        }

        var normalizedX = (worldX - WorldMinX) / (WorldMaxX - WorldMinX);
        var normalizedY = (worldY - WorldMinY) / (WorldMaxY - WorldMinY);
        gridX = Math.Clamp((int)MathF.Round(normalizedX * (Width - 1)), 0, Width - 1);
        gridY = Math.Clamp(Height - 1 - (int)MathF.Round(normalizedY * (Height - 1)), 0, Height - 1);
        return true;
    }

    private static string BuildGrid(char[,] grid)
    {
        var builder = new StringBuilder();
        builder.Append('+');
        builder.Append(new string('-', Width));
        builder.AppendLine("+");

        for (var y = 0; y < Height; y++)
        {
            builder.Append('|');
            for (var x = 0; x < Width; x++)
            {
                builder.Append(grid[y, x]);
            }

            builder.AppendLine("|");
        }

        builder.Append('+');
        builder.Append(new string('-', Width));
        builder.Append('+');
        return builder.ToString();
    }

    private static string BuildPlayers(ShooterViewEntityStore store)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Players:");
        var players = store.Entities.Values
            .Where(entity => entity.Kind == ShooterViewEntityKind.Player)
            .OrderBy(entity => entity.EntityId)
            .ToArray();

        if (players.Length == 0)
        {
            builder.Append("  <none>");
            return builder.ToString();
        }

        foreach (var player in players)
        {
            store.TryGetTransform(player.Key, out var transform);
            store.TryGetHealth(player.Key, out var health);
            store.TryGetScore(player.Key, out var score);
            builder.AppendLine($"  P{player.EntityId}: pos=({transform.X,5:0.00},{transform.Y,5:0.00}) face=({transform.FacingX,5:0.00},{transform.FacingY,5:0.00}) hp={health.Hp,3} score={score.Score,3} alive={player.Alive}");
        }

        return builder.ToString().TrimEnd();
    }

    private static string BuildBullets(ShooterViewEntityStore store)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Bullets:");
        var bullets = store.Entities.Values
            .Where(entity => entity.Kind == ShooterViewEntityKind.Bullet)
            .OrderBy(entity => entity.EntityId)
            .Take(8)
            .ToArray();

        if (bullets.Length == 0)
        {
            builder.Append("  <none>");
            return builder.ToString();
        }

        foreach (var bullet in bullets)
        {
            store.TryGetTransform(bullet.Key, out var transform);
            store.TryGetProjectileLifetime(bullet.Key, out var lifetime);
            builder.AppendLine($"  B{bullet.EntityId}: owner=P{bullet.OwnerEntityId} pos=({transform.X,5:0.00},{transform.Y,5:0.00}) vel=({transform.VelocityX,5:0.00},{transform.VelocityY,5:0.00}) ttl={lifetime.RemainingFrames}");
        }

        var remaining = store.BulletCount - bullets.Length;
        if (remaining > 0)
        {
            builder.AppendLine($"  ... {remaining} more");
        }

        return builder.ToString().TrimEnd();
    }

    private static string BuildEvents(IReadOnlyList<ShooterEventSnapshot> events)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Events:");
        if (events.Count == 0)
        {
            builder.Append("  <none>");
            return builder.ToString();
        }

        foreach (var battleEvent in events.Skip(Math.Max(0, events.Count - 6)))
        {
            var name = Enum.IsDefined(typeof(ShooterEventType), battleEvent.EventType)
                ? ((ShooterEventType)battleEvent.EventType).ToString()
                : battleEvent.EventType.ToString();
            builder.AppendLine($"  {name}: src=P{battleEvent.SourcePlayerId} target=P{battleEvent.TargetPlayerId} bullet={battleEvent.BulletId} value={battleEvent.Value} at=({battleEvent.X:0.00},{battleEvent.Y:0.00})");
        }

        return builder.ToString().TrimEnd();
    }
    private sealed class ConsoleProjectedViewSink : IShooterProjectedViewSink
    {
        private readonly ShooterConsoleRenderer _owner;

        public ConsoleProjectedViewSink(ShooterConsoleRenderer owner)
        {
            _owner = owner;
        }

        public void ApplyViewState(
            ShooterViewEntityStore store,
            in ShooterSnapshotViewBatch sourceBatch,
            in ShooterViewProjectionApplyResult applyResult)
        {
            _owner.RenderProjectedView(store, in sourceBatch, in applyResult);
        }

        public void Clear()
        {
        }
    }
}
