using System;
using System.Threading;
using System.Threading.Tasks;
using AbilityKit.Demo.Shooter.View;
using AbilityKit.Protocol.Room;

namespace AbilityKit.Demo.Shooter.Runtime.Tests;

internal sealed class RecordingShooterRoomGatewayTransport : IShooterRoomGatewayRequestTransport
{
    private readonly WireSubmitBattleInputRes _response;

    public RecordingShooterRoomGatewayTransport(in WireSubmitBattleInputRes response)
    {
        _response = response;
    }

    public uint LastOpCode { get; private set; }

    public ArraySegment<byte> LastPayload { get; private set; }

    public Task<ArraySegment<byte>> SendRequestAsync(uint opCode, ArraySegment<byte> payload, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        LastOpCode = opCode;
        LastPayload = TestByteSegments.Copy(payload);
        var response = WireRoomGatewayBinary.Serialize(in _response);
        return Task.FromResult(response);
    }
}
