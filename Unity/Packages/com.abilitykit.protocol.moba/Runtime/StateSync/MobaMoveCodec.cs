using System;
using AbilityKit.Protocol.Serialization;
using MemoryPack;

namespace AbilityKit.Protocol.Moba.StateSync
{
    [MemoryPackable]
    public partial struct MobaMovePayload
    {
        [MemoryPackOrder(0)] public float X;
        [MemoryPackOrder(1)] public float Z;
    }

    public static class MobaMoveCodec
    {
        public static byte[] Serialize(float x, float z)
        {
            var payload = new MobaMovePayload { X = x, Z = z };
            return WireSerializer.Serialize(in payload);
        }

        public static void Deserialize(byte[] payload, out float x, out float z)
        {
            if (payload == null || payload.Length == 0)
            {
                x = 0f;
                z = 0f;
                return;
            }

            var p = WireSerializer.Deserialize<MobaMovePayload>(payload);
            x = p.X;
            z = p.Z;
        }
    }
}
