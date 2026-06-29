using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Core.Recording.Lockstep;
using AbilityKit.Demo.Shooter;
using AbilityKit.Demo.Shooter.View;
using AbilityKit.Protocol.Shooter;

internal sealed class ShooterSmokeReplayRecordScope : IDisposable
{
    private const int StateHashVersion = 1;

    private readonly string _outputPath;
    private readonly ILockstepInputRecordWriter _writer;
    private bool _saved;

    private ShooterSmokeReplayRecordScope(string outputPath, LockstepInputRecordMeta meta)
    {
        _outputPath = outputPath;
        _writer = LockstepInputRecordCodecs.Current.CreateWriter(outputPath, meta);
    }

    public static ShooterSmokeReplayRecordScope? Create(string outputPath, in ShooterSmokeClientProcessOptions options)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return null;
        }

        var meta = new LockstepInputRecordMeta
        {
            WorldId = string.IsNullOrWhiteSpace(options.RoomId) ? options.ClientId : options.RoomId,
            WorldType = $"shooter.multiprocess.smoke/{options.Mode.ToString().ToLowerInvariant()}",
            TickRate = ShooterGameplay.DefaultTickRate,
            RandomSeed = options.Seed,
            PlayerId = options.PlayerId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            StartedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        };

        return new ShooterSmokeReplayRecordScope(outputPath, meta);
    }

    public string OutputPath => _outputPath;

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

        _writer.Append(in command);
        AppendStateHash(submit.Remote.AcceptedFrame, 0u);
    }

    public void RecordSnapshot(in ShooterSnapshotPushSmokeResult push, ArraySegment<byte> wirePayload)
    {
        var payload = ToArray(wirePayload);
        _writer.AppendSnapshot(push.WireFrame, push.PayloadOpCode, payload);

        if (push.PackedStateHash != 0u)
        {
            AppendStateHash(push.PackedFrame, push.PackedStateHash);
        }
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

        _writer.Dispose();
        _saved = true;
        return _outputPath;
    }

    public void Dispose()
    {
        Save();
    }

    private void AppendStateHash(int frame, uint hash)
    {
        _writer.AppendStateHash(Math.Max(0, frame), StateHashVersion, hash);
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
