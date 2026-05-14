using MemoryPack;
using Orleans.Serialization;

namespace AbilityKit.Orleans.Gateway.Serialization;

/// <summary>
/// 基于 MemoryPack 的序列化器（简单类型）
/// Orleans 复杂类型请使用 Orleans.Serialization.Serializer
/// </summary>
public static class GatewaySerializer
{
    public static T? Deserialize<T>(ReadOnlySpan<byte> data) where T : struct =>
        MemoryPackSerializer.Deserialize<T>(data);

    public static byte[] Serialize<T>(in T value) where T : struct =>
        MemoryPackSerializer.Serialize(value);

    public static T? Deserialize<T>(byte[] data) =>
        MemoryPackSerializer.Deserialize<T>(data);

    public static byte[] SerializeToBytes<T>(in T value) =>
        MemoryPackSerializer.Serialize(value);
}
