using System;
using MemoryPack;

namespace AbilityKit.Protocol.Moba.Room
{
    public static class WireRoomGatewayBinary
    {
        public static ArraySegment<byte> Serialize<T>(in T value)
        {
            var bytes = MemoryPackSerializer.Serialize(value);
            return new ArraySegment<byte>(bytes);
        }

        public static T Deserialize<T>(ArraySegment<byte> payload)
        {
            if (payload.Array == null || payload.Count == 0)
                return default;

            var span = new ReadOnlySpan<byte>(payload.Array, payload.Offset, payload.Count);
            return MemoryPackSerializer.Deserialize<T>(span);
        }

        public static T Deserialize<T>(ReadOnlySpan<byte> payload)
        {
            if (payload.Length == 0)
                return default;

            return MemoryPackSerializer.Deserialize<T>(payload);
        }
    }
}
