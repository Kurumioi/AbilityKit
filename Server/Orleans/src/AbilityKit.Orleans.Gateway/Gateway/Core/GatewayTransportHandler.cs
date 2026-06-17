using System.Buffers.Binary;
using System.Collections.Concurrent;
using AbilityKit.Orleans.Contracts.Rooms;
using AbilityKit.Orleans.Gateway.Abstractions;
using AbilityKit.Orleans.Gateway.Networking;
using Orleans;

namespace AbilityKit.Orleans.Gateway.Core;

/// <summary>
/// Gateway 传输层事件处理
/// </summary>
public sealed class GatewayTransportHandler : IGatewayTransportEvents
{
    private readonly IGatewaySessionRegistry _sessionRegistry;
    private readonly IGatewayRequestRouter _router;
    private readonly IClusterClient _clusterClient;
    private readonly ConcurrentDictionary<long, TcpTransportSession> _sessions = new();

    private readonly GatewayBackgroundTaskQueue _backgroundTasks;

    public GatewayTransportHandler(
        IGatewaySessionRegistry sessionRegistry,
        IGatewayRequestRouter router,
        IClusterClient clusterClient,
        GatewayBackgroundTaskQueue backgroundTasks)
    {
        _sessionRegistry = sessionRegistry;
        _router = router;
        _clusterClient = clusterClient;
        _backgroundTasks = backgroundTasks;
    }

    public void OnConnected(TcpTransportSession session)
    {
        RegisterSession(session);
    }

    public void OnRequest(long connectionId, uint opCode, uint seq, byte[] payload)
    {
        if (!_sessions.TryGetValue(connectionId, out var session))
            return;

        _backgroundTasks.TryQueue(async cancellationToken =>
        {
            var response = await _router.RouteAsync(session.Context, opCode, seq, payload, cancellationToken);
            var responsePayload = BuildResponsePayload(response);
            await session.SendResponseAsync(opCode, response.Seq, responsePayload, cancellationToken);
        });
    }

    private static byte[] BuildResponsePayload(GatewayResponse response)
    {
        var payload = response.Payload ?? Array.Empty<byte>();
        var responsePayload = new byte[sizeof(int) + payload.Length];
        BinaryPrimitives.WriteInt32LittleEndian(responsePayload.AsSpan(0, sizeof(int)), response.StatusCode);
        payload.CopyTo(responsePayload.AsSpan(sizeof(int)));
        return responsePayload;
    }

    public void OnClosed(long connectionId)
    {
        if (_sessions.TryRemove(connectionId, out var session))
        {
            MarkRoomMemberOffline(session.Context);
        }

        _sessionRegistry.Unregister(connectionId);
    }

    private void MarkRoomMemberOffline(GatewaySessionContext context)
    {
        var accountId = context.AccountId;
        var roomId = context.RoomId;
        if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(roomId))
        {
            return;
        }

        _backgroundTasks.TryQueue(async _ =>
        {
            var room = _clusterClient.GetGrain<IRoomGrain>(roomId);
            await room.MarkOfflineAsync(accountId);
        });
    }

    internal void RegisterSession(TcpTransportSession session)
    {
        _sessions[session.ConnectionId] = session;
        _sessionRegistry.Register(session.ConnectionId, session);
    }
}
