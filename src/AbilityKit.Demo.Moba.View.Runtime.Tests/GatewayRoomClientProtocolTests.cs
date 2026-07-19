using System;
using System.Buffers.Binary;
using AbilityKit.Game.Battle.Agent;
using AbilityKit.Network.Abstractions;
using AbilityKit.Network.Protocol;
using AbilityKit.Network.Runtime.TcpGateway;
using AbilityKit.Protocol.Room;
using Xunit;

namespace AbilityKit.Demo.Moba.View.Runtime.Tests;

public sealed class GatewayRoomClientProtocolTests
{
    [Fact]
    public async Task SubscribeStateSyncUsesCanonicalAuthenticatedRoomRequest()
    {
        const uint subscribeOpCode = 7301U;
        using var connection = new RecordingConnection();
        var opCodes = new GatewayRoomOpCodes(
            createRoom: 7101U,
            joinRoom: 7102U,
            subscribeStateSync: subscribeOpCode,
            setReady: 7104U,
            pickHero: 7105U,
            startBattle: 7106U,
            submitBattleInput: 7107U);
        var client = new GatewayRoomClient(connection, opCodes);

        var result = await client.SubscribeStateSyncAsync(
            "session-token",
            "battle-42",
            "room-42",
            timeout: TimeSpan.FromSeconds(1));

        Assert.True(result.Success);
        Assert.Equal(subscribeOpCode, connection.LastOpCode);
        var request = WireRoomGatewayBinary.Deserialize<WireSubscribeStateSyncReq>(connection.LastPayload);
        Assert.Equal("session-token", request.SessionToken);
        Assert.Equal("battle-42", request.BattleId);
        Assert.Equal("room-42", request.RoomId);
    }

    private sealed class RecordingConnection : IConnection
    {
        public ConnectionState State => ConnectionState.Connected;
        public bool IsConnected => true;
        public uint LastOpCode { get; private set; }
        public ArraySegment<byte> LastPayload { get; private set; }

        public event Action? Connected;
        public event Action? Disconnected;
        public event Action<Exception>? Error { add { } remove { } }
        public event Action<uint, uint, ArraySegment<byte>>? PacketReceived;
        public event Action<uint, ArraySegment<byte>>? ServerPushReceived { add { } remove { } }
        public event Action<string, string>? Kicked { add { } remove { } }

        public void Open(string host, int port) => Connected?.Invoke();
        public void Close() => Disconnected?.Invoke();
        public void Tick(float deltaTime) { }

        public void Send(uint opCode, ArraySegment<byte> payload, ushort flags = 0, uint seq = 0)
        {
            LastOpCode = opCode;
            LastPayload = Copy(payload);

            var response = new WireSubscribeStateSyncRes
            {
                Success = true,
                Message = "subscribed"
            };
            var responsePayload = WireRoomGatewayBinary.Serialize(in response);
            PacketReceived?.Invoke(opCode, seq, EncodeGatewayResponse(TcpGatewayStatusCode.Ok, responsePayload));
        }

        public void Dispose() { }

        private static ArraySegment<byte> Copy(ArraySegment<byte> source)
        {
            if (source.Array == null || source.Count == 0)
            {
                return default;
            }

            var copy = new byte[source.Count];
            Buffer.BlockCopy(source.Array, source.Offset, copy, 0, source.Count);
            return new ArraySegment<byte>(copy);
        }

        private static ArraySegment<byte> EncodeGatewayResponse(
            TcpGatewayStatusCode statusCode,
            ArraySegment<byte> payload)
        {
            var bytes = new byte[sizeof(int) + payload.Count];
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0, sizeof(int)), (int)statusCode);
            if (payload.Array != null && payload.Count > 0)
            {
                Buffer.BlockCopy(payload.Array, payload.Offset, bytes, sizeof(int), payload.Count);
            }

            return new ArraySegment<byte>(bytes);
        }
    }
}
