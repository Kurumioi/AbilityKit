using MemoryPack;

namespace AbilityKit.Orleans.Gateway.TcpGateway;

/// <summary>
/// 基于 MemoryPack 的统一序列化器
/// </summary>
public static class GatewaySerializer
{
    public static T? Deserialize<T>(ReadOnlySpan<byte> data)
    {
        return MemoryPackSerializer.Deserialize<T>(data);
    }

    public static byte[] Serialize<T>(in T value)
    {
        return MemoryPackSerializer.Serialize(value);
    }
}
