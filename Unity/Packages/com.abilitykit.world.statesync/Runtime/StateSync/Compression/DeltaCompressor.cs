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

    public sealed class EntityDeltaCompressor
    {
        public byte[] CompressEntityDiff(
            Snapshot.EntityStateSnapshot current,
            Snapshot.EntityStateSnapshot? previous)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(current.EntityId);

            bool posChanged = previous == null || !current.Position.ApproximatelyEquals(previous.Value.Position);
            writer.Write(posChanged);
            if (posChanged)
            {
                writer.Write(current.Position.X);
                writer.Write(current.Position.Y);
                writer.Write(current.Position.Z);
            }

            bool rotChanged = previous == null || !current.Rotation.ApproximatelyEquals(previous.Value.Rotation);
            writer.Write(rotChanged);
            if (rotChanged)
            {
                writer.Write(current.Rotation.X);
                writer.Write(current.Rotation.Y);
                writer.Write(current.Rotation.Z);
                writer.Write(current.Rotation.W);
            }

            bool velChanged = previous == null || !current.Velocity.ApproximatelyEquals(previous.Value.Velocity);
            writer.Write(velChanged);
            if (velChanged)
            {
                writer.Write(current.Velocity.X);
                writer.Write(current.Velocity.Y);
                writer.Write(current.Velocity.Z);
            }

            bool healthChanged = previous == null || current.HealthPercent != previous.Value.HealthPercent;
            writer.Write(healthChanged);
            if (healthChanged) writer.Write(current.HealthPercent);

            bool flagsChanged = previous == null || current.StateFlags != previous.Value.StateFlags;
            writer.Write(flagsChanged);
            if (flagsChanged) writer.Write(current.StateFlags);

            bool abilityChanged = previous == null || current.ActiveAbilityMask != previous.Value.ActiveAbilityMask;
            writer.Write(abilityChanged);
            if (abilityChanged) writer.Write(current.ActiveAbilityMask);

            bool controlChanged = previous == null || current.ControlFlags != previous.Value.ControlFlags;
            writer.Write(controlChanged);
            if (controlChanged) writer.Write(current.ControlFlags);

            return stream.ToArray();
        }

        public Snapshot.EntityStateSnapshot DecompressEntityDiff(
            byte[] data,
            Snapshot.EntityStateSnapshot? baseState)
        {
            using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream);

            long entityId = reader.ReadInt64();
            var result = baseState.HasValue ? baseState.Value : new Snapshot.EntityStateSnapshot(entityId);
            result.EntityId = entityId;

            if (reader.ReadBoolean())
            {
                result.Position = new Snapshot.Vec3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            }

            if (reader.ReadBoolean())
            {
                result.Rotation = new Snapshot.Quat(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            }

            if (reader.ReadBoolean())
            {
                result.Velocity = new Snapshot.Vec3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            }

            if (reader.ReadBoolean())
            {
                result.HealthPercent = reader.ReadByte();
            }

            if (reader.ReadBoolean())
            {
                result.StateFlags = reader.ReadUInt32();
            }

            if (reader.ReadBoolean())
            {
                result.ActiveAbilityMask = reader.ReadInt64();
            }

            if (reader.ReadBoolean())
            {
                result.ControlFlags = reader.ReadByte();
            }

            return result;
        }
    }
}
