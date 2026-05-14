namespace AbilityKit.Orleans.Gateway;

/// <summary>
/// Gateway 会话上下文
/// </summary>
public sealed class GatewaySessionContext
{
    public long ConnectionId { get; }
    public string SessionId => ConnectionId.ToString();
    public string? AccountId { get; set; }
    public string? RoomId { get; set; }
    public string? SessionToken { get; set; }

    public GatewaySessionContext(long connectionId)
    {
        ConnectionId = connectionId;
    }
}
