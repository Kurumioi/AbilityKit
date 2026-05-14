using System.Buffers.Binary;

namespace AbilityKit.Orleans.Gateway.Networking;

/// <summary>
/// 网络帧编解码器
/// </summary>
public static class NetworkFrameCodec
{
    public const int HeaderLength = 16;

    public static int GetFrameSize(int payloadLength) => 4 + HeaderLength + payloadLength;

    public static bool TryParseFrame(
        ReadOnlySpan<byte> source,
        out int totalSize,
        out NetworkPacketHeader header,
        out byte[] payload)
    {
        totalSize = 0;
        header = default;
        payload = Array.Empty<byte>();

        if (source.Length < 4 + HeaderLength) return false;

        var frameLength = BinaryPrimitives.ReadUInt32LittleEndian(source);
        if (frameLength < HeaderLength) return false;

        totalSize = 4 + (int)frameLength;
        if (source.Length < totalSize) return false;

        header = NetworkPacketHeader.Read(source.Slice(4, HeaderLength));
        if (header.PayloadLength != frameLength - HeaderLength)
            throw new InvalidOperationException("Payload length mismatch.");

        payload = new byte[header.PayloadLength];
        source.Slice(4 + HeaderLength, (int)header.PayloadLength).CopyTo(payload);
        return true;
    }

    public static void WriteFrame(Span<byte> destination, NetworkPacketHeader header, ReadOnlySpan<byte> payload)
    {
        var frameLength = HeaderLength + payload.Length;
        BinaryPrimitives.WriteUInt32LittleEndian(destination, (uint)frameLength);
        header.Write(destination.Slice(4));
        payload.CopyTo(destination.Slice(4 + HeaderLength));
    }
}
