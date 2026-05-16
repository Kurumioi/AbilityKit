using AbilityKit.Core.Math;
using AbilityKit.Protocol.Serialization;
using MemoryPack;

namespace AbilityKit.Protocol.Moba.StateSync
{
    [MemoryPackable]
    public partial struct MobaEnterGamePayload
    {
        [MemoryPackOrder(0)] public float X;
        [MemoryPackOrder(1)] public float Y;
        [MemoryPackOrder(2)] public float Z;

        public Vec3 ToVec3() => new Vec3(X, Y, Z);
    }

    public static class MobaEnterGamePayloadCodec
    {
        public const int PayloadOpCode = 1;

        public static byte[] Serialize(in Vec3 pos)
        {
            var p = new MobaEnterGamePayload { X = pos.X, Y = pos.Y, Z = pos.Z };
            return WireSerializer.Serialize(in p);
        }

        public static bool TryDeserializePosition(int opCode, byte[] payload, out Vec3 pos)
        {
            if (opCode != PayloadOpCode)
            {
                pos = default;
                return false;
            }

            if (payload == null || payload.Length == 0)
            {
                pos = default;
                return false;
            }

            try
            {
                var p = WireSerializer.Deserialize<MobaEnterGamePayload>(payload);
                pos = p.ToVec3();
                return true;
            }
            catch
            {
                pos = default;
                return false;
            }
        }
    }
}
