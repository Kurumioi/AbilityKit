using System.Buffers.Binary;

namespace AbilityKit.Orleans.Gateway.TcpGateway;

/// <summary>
/// 网络帧编解码器 - 处理黏包
/// 格式: [FrameLength:4][Header:16][Payload:N]
/// </summary>
internal static class NetworkFrameCodec
{
    public const int HeaderLength = 4 + NetworkPacketHeader.Size;

    public static int GetFrameSize(int payloadLength) => HeaderLength + payloadLength;

    public static void WriteFrame(Span<byte> destination, NetworkPacketHeader header, ReadOnlySpan<byte> payload)
    {
        if (payload.Length != header.PayloadLength)
            throw new ArgumentException("Payload length mismatch.", nameof(payload));

        var frameLength = NetworkPacketHeader.Size + payload.Length;
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(0, 4), (uint)frameLength);
        header.Write(destination.Slice(4, NetworkPacketHeader.Size));
        payload.CopyTo(destination.Slice(HeaderLength, payload.Length));
    }

    public static bool TryParseFrame(ReadOnlySpan<byte> source, out int totalSize, out NetworkPacketHeader header, out ReadOnlySpan<byte> payload)
    {
        header = default;
        payload = default;
        totalSize = 0;

        if (source.Length < HeaderLength) return false;

        var frameLength = BinaryPrimitives.ReadUInt32LittleEndian(source.Slice(0, 4));
        if (frameLength < NetworkPacketHeader.Size)
            throw new InvalidOperationException("Invalid frame length.");

        totalSize = 4 + (int)frameLength;
        if (source.Length < totalSize) return false;

        header = NetworkPacketHeader.Read(source.Slice(4, NetworkPacketHeader.Size));
        if (header.PayloadLength != frameLength - NetworkPacketHeader.Size)
            throw new InvalidOperationException("Payload length mismatch.");

        payload = source.Slice(HeaderLength, (int)header.PayloadLength);
        return true;
    }
}
