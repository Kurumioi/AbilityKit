using System.Buffers.Binary;
using System.Collections.Concurrent;
using AbilityKit.Orleans.Gateway.Abstractions;
using AbilityKit.Orleans.Gateway.Networking;

namespace AbilityKit.Orleans.Gateway.Core;

/// <summary>
/// Gateway 传输层事件处理
/// </summary>
public sealed class GatewayTransportHandler : IGatewayTransportEvents
{
    private readonly IGatewaySessionRegistry _sessionRegistry;
    private readonly IGatewayRequestRouter _router;
    private readonly ConcurrentDictionary<long, TcpTransportSession> _sessions = new();

    public GatewayTransportHandler(
        IGatewaySessionRegistry sessionRegistry,
        IGatewayRequestRouter router)
    {
        _sessionRegistry = sessionRegistry;
        _router = router;
    }

    public void OnConnected(TcpTransportSession session)
    {
        RegisterSession(session);
    }

    public void OnRequest(long connectionId, uint opCode, uint seq, byte[] payload)
    {
        if (!_sessions.TryGetValue(connectionId, out var session))
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                var response = await _router.RouteAsync(session.Context, opCode, seq, payload, CancellationToken.None);
                var responsePayload = BuildResponsePayload(response);
                await session.SendResponseAsync(opCode, response.Seq, responsePayload, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Route error: {ex.Message}");
            }
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
        _sessions.TryRemove(connectionId, out _);
        _sessionRegistry.Unregister(connectionId);
    }

    internal void RegisterSession(TcpTransportSession session)
    {
        _sessions[session.ConnectionId] = session;
        _sessionRegistry.Register(session.ConnectionId, session);
    }
}
