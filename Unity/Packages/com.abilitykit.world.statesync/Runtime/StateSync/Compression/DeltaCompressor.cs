using System;
using System.IO;
using System.IO.Compression;

namespace AbilityKit.Ability.StateSync.Compression
{
    public sealed class DeltaCompressor
    {
        public enum CompressionLevel
        {
            None = 0,
            Light = 1,
            Medium = 2,
            Heavy = 3
        }

        private readonly CompressionLevel _level;

        public DeltaCompressor(CompressionLevel level = CompressionLevel.Medium)
        {
            _level = level;
        }

        public byte[] Compress(byte[] data)
        {
            if (data == null || data.Length == 0) return Array.Empty<byte>();

            switch (_level)
            {
                case CompressionLevel.None:
                    return data;

                case CompressionLevel.Light:
                    return LZ4Compress(data);

                case CompressionLevel.Medium:
                    return GZipCompress(data);

                case CompressionLevel.Heavy:
                    return ZstdCompress(data);

                default:
                    return GZipCompress(data);
            }
        }

        public byte[] Decompress(byte[] data)
        {
            if (data == null || data.Length == 0) return Array.Empty<byte>();

            switch (_level)
            {
                case CompressionLevel.None:
                    return data;

                case CompressionLevel.Light:
                    return LZ4Decompress(data);

                case CompressionLevel.Medium:
                    return GZipDecompress(data);

                case CompressionLevel.Heavy:
                    return ZstdDecompress(data);

                default:
                    return GZipDecompress(data);
            }
        }

        private static byte[] GZipCompress(byte[] data)
        {
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: true))
            {
                gzip.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }

        private static byte[] GZipDecompress(byte[] data)
        {
            using var input = new MemoryStream(data);
            using var gzip = new GZipStream(input, CompressionMode.Decompress, leaveOpen: true);
            using var output = new MemoryStream();
            gzip.CopyTo(output);
            return output.ToArray();
        }

        private static byte[] LZ4Compress(byte[] data)
        {
            return GZipCompress(data);
        }

        private static byte[] LZ4Decompress(byte[] data)
        {
            return GZipDecompress(data);
        }

        private static byte[] ZstdCompress(byte[] data)
        {
            return GZipCompress(data);
        }

        private static byte[] ZstdDecompress(byte[] data)
        {
            return GZipDecompress(data);
        }

        public int EstimateCompressedSize(int originalSize)
        {
            return originalSize * 3 / 4;
        }
    }
}
