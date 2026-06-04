using AbilityKit.Protocol.Serialization;

namespace AbilityKit.Protocol.Moba.CreateWorld
{
    public static class MobaCreateWorldInitCodec
    {
        public static byte[] Serialize(in MobaCreateWorldInitPayload payload)
        {
            return WireSerializer.Serialize(in payload);
        }

        public static bool TryDeserialize(byte[] bytes, out MobaCreateWorldInitPayload payload)
        {
            if (bytes == null || bytes.Length == 0)
            {
                payload = default;
                return false;
            }

            try
            {
                payload = WireSerializer.Deserialize<MobaCreateWorldInitPayload>(bytes);
                return true;
            }
            catch
            {
                payload = default;
                return false;
            }
        }
    }
}
