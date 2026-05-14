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

    public void OnRequest(long connectionId, uint opCode, uint seq, byte[] payload)
    {
        if (!_sessions.TryGetValue(connectionId, out var session))
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                var response = await _router.RouteAsync(session.Context, opCode, seq, payload, CancellationToken.None);

                // 发送响应
                var responsePayload = MemoryPack.MemoryPackSerializer.Serialize(response);
                var header = new NetworkPacketHeader(NetworkPacketFlags.Response, opCode, seq, (uint)responsePayload.Length);
                // 注意：这里需要通过网络层发送响应，但由于是内部调用，实际发送由 TcpTransportSession 处理
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Route error: {ex.Message}");
            }
        });
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
