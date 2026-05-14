namespace AbilityKit.Orleans.Gateway;

/// <summary>
/// Gateway 响应
/// </summary>
public sealed class GatewayResponse
{
    public uint Seq { get; }
    public int StatusCode { get; }
    public byte[] Payload { get; }

    private GatewayResponse(uint seq, int statusCode, byte[] payload)
    {
        Seq = seq;
        StatusCode = statusCode;
        Payload = payload;
    }

    public static GatewayResponse Ok(uint seq, byte[]? payload = null) =>
        new(seq, GatewayStatusCode.Success, payload ?? Array.Empty<byte>());

    public static GatewayResponse Error(uint seq, int statusCode, byte[]? payload = null) =>
        new(seq, statusCode, payload ?? Array.Empty<byte>());
}
