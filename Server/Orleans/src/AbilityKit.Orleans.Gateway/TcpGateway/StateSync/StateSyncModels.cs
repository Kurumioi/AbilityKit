using AbilityKit.Ability.StateSync.Network;

namespace AbilityKit.Orleans.Gateway.TcpGateway.StateSync;

/// <summary>
/// StateSync 客户端接口
/// </summary>
public interface IStateSyncClient
{
    string SessionId { get; }
    void OnSnapshotReceived(SnapshotMessage notification);
}

/// <summary>
/// StateSync 事件处理器接口
/// </summary>
public interface IStateSyncHandler
{
    void Subscribe(string sessionId, IStateSyncClient client);
    void Unsubscribe(string sessionId);
    void HandleSnapshotPush(SnapshotMessage notification);
    int SubscriberCount { get; }
}
