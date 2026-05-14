using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using AbilityKit.Ability.StateSync.Network;

namespace AbilityKit.Orleans.Gateway.TcpGateway.StateSync;

/// <summary>
/// TCP Gateway StateSync 处理器
/// 管理连接的客户端的快照订阅
/// </summary>
public sealed class TcpGatewayStateSyncHandler : IStateSyncHandler
{
    private readonly ILogger<TcpGatewayStateSyncHandler> _logger;
    private readonly ConcurrentDictionary<string, IStateSyncClient> _subscribedClients = new();

    public TcpGatewayStateSyncHandler(ILogger<TcpGatewayStateSyncHandler> logger)
    {
        _logger = logger;
    }

    public void Subscribe(string sessionId, IStateSyncClient client)
    {
        if (_subscribedClients.TryAdd(sessionId, client))
        {
            _logger.LogInformation("[TcpGatewayStateSync] Client subscribed: {SessionId}. Total: {Count}",
                sessionId, _subscribedClients.Count);
        }
    }

    public void Unsubscribe(string sessionId)
    {
        if (_subscribedClients.TryRemove(sessionId, out _))
        {
            _logger.LogInformation("[TcpGatewayStateSync] Client unsubscribed: {SessionId}. Total: {Count}",
                sessionId, _subscribedClients.Count);
        }
    }

    public void HandleSnapshotPush(SnapshotMessage notification)
    {
        foreach (var kvp in _subscribedClients)
        {
            try
            {
                kvp.Value.OnSnapshotReceived(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TcpGatewayStateSync] Error sending snapshot to client: {SessionId}", kvp.Key);
            }
        }
    }

    public int SubscriberCount => _subscribedClients.Count;
}
