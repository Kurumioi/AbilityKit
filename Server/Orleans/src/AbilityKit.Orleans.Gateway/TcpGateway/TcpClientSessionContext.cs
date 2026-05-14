namespace AbilityKit.Orleans.Gateway.TcpGateway;

public sealed class TcpClientSessionContext
{
    public long ConnectionId { get; }
    public string SessionId { get; }

    public TcpClientSessionContext(long connectionId)
    {
        ConnectionId = connectionId;
        SessionId = connectionId.ToString();
    }
}
