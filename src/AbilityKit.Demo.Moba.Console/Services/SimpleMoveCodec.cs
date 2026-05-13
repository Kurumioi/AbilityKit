using System;
using System.IO;

namespace AbilityKit.Demo.Moba.Console.Services
{
    /// <summary>
    /// 简单的移动数据编解码器
    /// </summary>
    public static class SimpleMoveCodec
    {
        public static byte[] Serialize(float x, float z)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(x);
            writer.Write(z);
            return stream.ToArray();
        }

        public static (float x, float z) Deserialize(byte[] payload)
        {
            if (payload == null || payload.Length < 8)
                return (0f, 0f);

            using var stream = new MemoryStream(payload);
            using var reader = new BinaryReader(stream);
            return (reader.ReadSingle(), reader.ReadSingle());
        }
    }
}
