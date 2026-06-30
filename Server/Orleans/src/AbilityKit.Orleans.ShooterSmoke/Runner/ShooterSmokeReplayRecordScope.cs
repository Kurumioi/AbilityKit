using System.IO;
using System.Text.Json;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Core.Recording.FrameRecord;
using AbilityKit.Demo.Shooter;
using AbilityKit.Demo.Shooter.View;
using AbilityKit.Orleans.Contracts.Battle;
using AbilityKit.Protocol.Shooter;

internal sealed class ShooterSmokeReplayRecordScope : IDisposable
{
    private const int StateHashVersion = 1;

    private readonly string _outputPath;
    private readonly string _minimizedOutputPath;
    private readonly ShooterSmokeReplayKind _kind;
    private readonly IFrameRecordWriter _fullWriter;
    private readonly IFrameRecordWriter _minimizedWriter;
    private bool _saved;

    private ShooterSmokeReplayRecordScope(string outputPath, FrameRecordMeta meta)
    {
        _outputPath = outputPath;
        _minimizedOutputPath = CreateMinimizedOutputPath(outputPath);
        _kind = ShooterSmokeReplayTypes.ResolveKind(meta);
        _fullWriter = FrameRecordCodecs.Current.CreateWriter(outputPath, meta);
        _minimizedWriter = FrameRecordCodecs.Current.CreateWriter(_minimizedOutputPath, meta);
    }

    public static ShooterSmokeReplayRecordScope? CreateInputStateReplay(string outputPath, in ShooterSmokeClientProcessOptions options)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return null;
        }

        var meta = new FrameRecordMeta
        {
            WorldId = string.IsNullOrWhiteSpace(options.RoomId) ? options.ClientId : options.RoomId,
            WorldType = ShooterSmokeReplayTypes.CreateInputStateWorldType(options.Mode),
            TickRate = ShooterGameplay.DefaultTickRate,
            RandomSeed = options.Seed,
            PlayerId = options.PlayerId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            StartedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        };

        return new ShooterSmokeReplayRecordScope(outputPath, meta);
    }

    public static ShooterSmokeReplayRecordScope? CreateInputLogicReplay(string outputPath, string roomId, string battleId, ulong worldId, int seed)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return null;
        }

        var meta = new FrameRecordMeta
        {
            WorldId = worldId == 0 ? battleId : worldId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            WorldType = ShooterSmokeReplayTypes.InputLogicReplayWorldType,
            TickRate = ShooterGameplay.DefaultTickRate,
            RandomSeed = seed,
            PlayerId = string.IsNullOrWhiteSpace(roomId) ? battleId : roomId,
            StartedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        };

        return new ShooterSmokeReplayRecordScope(outputPath, meta);
    }

    public string OutputPath => _outputPath;

    public string MinimizedOutputPath => _minimizedOutputPath;

    public void RecordLaunch(string accountId, ShooterClientNetworkLaunchResult launched)
    {
        AppendStateHash(Math.Max(0, launched.Flow.TargetFrame), 0u);
    }

    public void RecordInput(in ShooterClientGatewayInputSubmitResult submit)
    {
        var payload = submit.Local.Packet.Payload ?? Array.Empty<byte>();
        var command = new PlayerInputCommand(
            new FrameIndex(submit.Local.RequestedFrame),
            new PlayerId(submit.Local.Packet.Command.PlayerId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            submit.Local.Packet.OpCode,
            payload);

        AppendInput(in command);
        AppendStateHash(submit.Remote.AcceptedFrame, 0u);
    }

    public void RecordServerInput(int frame, uint playerId, int opCode, byte[]? payload)
    {
        var command = new PlayerInputCommand(
            new FrameIndex(Math.Max(0, frame)),
            new PlayerId(playerId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            opCode,
            payload ?? Array.Empty<byte>());

        AppendInput(in command);
    }

    public void RecordSnapshot(in ShooterSnapshotPushSmokeResult push, ArraySegment<byte> wirePayload)
    {
        var payload = ToArray(wirePayload);
        AppendSnapshot(push.WireFrame, push.PayloadOpCode, payload);

        if (push.PackedStateHash != 0u)
        {
            AppendStateHash(push.PackedFrame, push.PackedStateHash);
        }
    }

    public void RecordServerSnapshot(BattleSnapshot? snapshot)
    {
        if (snapshot == null)
        {
            return;
        }

        var payload = JsonSerializer.SerializeToUtf8Bytes(snapshot);
        AppendSnapshot(Math.Max(0, snapshot.Frame), ShooterSmokeReplayOpCodes.ServerBattleSnapshot, payload);
        AppendStateHash(snapshot.Frame, ComputeSnapshotHash(snapshot));
    }

    public void RecordReconnect(in ShooterSmokeReconnectProcessResult result)
    {
        if (result.ReconnectCount <= 0)
        {
            return;
        }

        AppendStateHash(Math.Max(0, result.TargetFrame), 0u);
    }

    public void RecordResult(in ShooterSmokeClientProcessResult result)
    {
        AppendStateHash(result.RuntimeFrame, result.StateHash);
    }

    public string Save()
    {
        if (_saved)
        {
            return _outputPath;
        }

        _fullWriter.Dispose();
        _minimizedWriter.Dispose();
        _saved = true;
        return _outputPath;
    }

    public void Dispose()
    {
        Save();
    }

    private void AppendInput(in PlayerInputCommand command)
    {
        _fullWriter.Append(in command);
        if (_kind == ShooterSmokeReplayKind.InputLogic)
        {
            _minimizedWriter.Append(in command);
        }
    }

    private void AppendSnapshot(int frame, int opCode, byte[] payload)
    {
        _fullWriter.AppendSnapshot(frame, opCode, payload);
        if (_kind == ShooterSmokeReplayKind.InputState)
        {
            _minimizedWriter.AppendSnapshot(frame, opCode, payload);
        }
    }

    private void AppendStateHash(int frame, uint hash)
    {
        _fullWriter.AppendStateHash(Math.Max(0, frame), StateHashVersion, hash);
    }

    private static string CreateMinimizedOutputPath(string outputPath)
    {
        var directory = Path.GetDirectoryName(outputPath);
        var extension = Path.GetExtension(outputPath);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(outputPath);
        var minimizedFileName = fileNameWithoutExtension.EndsWith(".record", StringComparison.OrdinalIgnoreCase)
            ? fileNameWithoutExtension.Substring(0, fileNameWithoutExtension.Length - ".record".Length) + ".min.record" + extension
            : fileNameWithoutExtension + ".min" + extension;
        return string.IsNullOrWhiteSpace(directory)
            ? minimizedFileName
            : Path.Combine(directory, minimizedFileName);
    }

    private static uint ComputeSnapshotHash(BattleSnapshot snapshot)
    {
        unchecked
        {
            var hash = 2166136261u;
            hash = Mix(hash, snapshot.Frame);
            hash = Mix(hash, snapshot.MatchState);
            hash = Mix(hash, snapshot.MatchFinal ? 1 : 0);
            hash = Mix(hash, snapshot.MatchVictory ? 1 : 0);
            hash = Mix(hash, snapshot.DefeatedEnemies);
            hash = Mix(hash, snapshot.RemainingTimeFrames);

            var actors = snapshot.Actors ?? new List<ActorSnapshot>();
            for (var i = 0; i < actors.Count; i++)
            {
                var actor = actors[i];
                hash = Mix(hash, actor.ActorId);
                hash = Mix(hash, BitConverter.SingleToInt32Bits(actor.X));
                hash = Mix(hash, BitConverter.SingleToInt32Bits(actor.Z));
                hash = Mix(hash, BitConverter.SingleToInt32Bits(actor.Hp));
                hash = Mix(hash, actor.TeamId);
            }

            return hash;
        }
    }

    private static uint Mix(uint hash, int value)
    {
        unchecked
        {
            hash ^= (uint)value;
            return hash * 16777619u;
        }
    }

    private static byte[] ToArray(ArraySegment<byte> payload)
    {
        if (payload.Array == null || payload.Count <= 0)
        {
            return Array.Empty<byte>();
        }

        var bytes = new byte[payload.Count];
        Buffer.BlockCopy(payload.Array, payload.Offset, bytes, 0, payload.Count);
        return bytes;
    }
}

internal static class ShooterSmokeReplayOpCodes
{
    public const int ServerBattleSnapshot = 100_001;
}
