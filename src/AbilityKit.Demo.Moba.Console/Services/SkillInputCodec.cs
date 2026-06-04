using AbilityKit.Core.Generic;
using AbilityKit.Protocol.Moba;

namespace AbilityKit.Demo.Moba.Console.Services
{
    /// <summary>
    /// Console skill input codec backed by the official MOBA protocol DTO.
    /// </summary>
    public static class SkillInputCodec
    {
        public static byte[] Serialize(in SkillInputEvent evt)
        {
            return BinaryObjectCodec.Encode(evt);
        }

        public static SkillInputEvent Deserialize(byte[] payload)
        {
            return BinaryObjectCodec.Decode<SkillInputEvent>(payload);
        }
    }
}
