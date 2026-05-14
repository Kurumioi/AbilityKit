using MemoryPack;

namespace AbilityKit.Orleans.Gateway.TcpGateway.Messages;

/// <summary>
/// Gateway 请求消息
/// </summary>
[MemoryPackable]
public readonly partial struct GatewayRequest
{
    [MemoryPackOrder(0)] public readonly uint Seq;
    [MemoryPackOrder(1)] public readonly byte[] Payload;

    [MemoryPackConstructor]
    public GatewayRequest(uint seq, byte[] payload)
    {
        Seq = seq;
        Payload = payload;
    }
}

/// <summary>
/// Gateway 响应消息
/// </summary>
[MemoryPackable]
public readonly partial struct GatewayResponse
{
    [MemoryPackOrder(0)] public readonly uint Seq;
    [MemoryPackOrder(1)] public readonly int StatusCode;
    [MemoryPackOrder(2)] public readonly byte[] Payload;

    [MemoryPackConstructor]
    public GatewayResponse(uint seq, int statusCode, byte[] payload)
    {
        Seq = seq;
        StatusCode = statusCode;
        Payload = payload;
    }

    public static GatewayResponse Ok(uint seq, byte[]? payload = null) =>
        new(seq, (int)TcpGatewayStatusCode.Ok, payload ?? Array.Empty<byte>());

    public static GatewayResponse Ok(uint seq, ArraySegment<byte> payload) =>
        new(seq, (int)TcpGatewayStatusCode.Ok, payload.Array ?? Array.Empty<byte>());

    public static GatewayResponse Error(uint seq, TcpGatewayStatusCode statusCode, byte[]? payload = null) =>
        new(seq, (int)statusCode, payload ?? Array.Empty<byte>());

    public static GatewayResponse Error(uint seq, TcpGatewayStatusCode statusCode, ArraySegment<byte> payload) =>
        new(seq, (int)statusCode, payload.Array ?? Array.Empty<byte>());
}

/// <summary>
/// Gateway 响应状态码
/// </summary>
public enum TcpGatewayStatusCode : int
{
    Ok = 0,
    UnhandledOpCode = 1,
    Timeout = 2,
    Exception = 3,
    BadRequest = 4
}
