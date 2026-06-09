using System;
using System.Threading;
using System.Threading.Tasks;
using AbilityKit.Demo.Shooter.View;
using AbilityKit.Protocol.Room;

namespace AbilityKit.Demo.Shooter.Runtime.Tests;

internal sealed class RecordingShooterRoomFlowTransport : IShooterRoomGatewayRequestTransport
{
    private ArraySegment<byte> _response = new ArraySegment<byte>(Array.Empty<byte>());

    public uint LastOpCode { get; private set; }

    public ArraySegment<byte> LastPayload { get; private set; }

    public void SetResponse<T>(in T response)
    {
        _response = WireRoomGatewayBinary.Serialize(in response);
    }

    public Task<ArraySegment<byte>> SendRequestAsync(uint opCode, ArraySegment<byte> payload, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        LastOpCode = opCode;
        LastPayload = TestByteSegments.Copy(payload);
        return Task.FromResult(TestByteSegments.Copy(_response));
    }
}
