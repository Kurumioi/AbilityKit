namespace AbilityKit.Orleans.Gateway;

/// <summary>
/// Gateway 请求
/// </summary>
public sealed class GatewayRequest
{
    public uint Seq { get; }
    public byte[] Payload { get; }

    public GatewayRequest(uint seq, byte[] payload)
    {
        Seq = seq;
        Payload = payload;
    }
}
