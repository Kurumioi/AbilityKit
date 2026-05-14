using System.Buffers.Binary;

namespace AbilityKit.Orleans.Gateway.TcpGateway;

/// <summary>
/// 网络包头接口
/// </summary>
public interface IPacketHeader
{
    int PacketLength { get; }
}

/// <summary>
/// 统一的网络包头实现
/// 格式: [Flags:2][HeaderSize:2][OpCode:4][Seq:4][PayloadLength:4] = 16 bytes
/// </summary>
public readonly struct NetworkPacketHeader : IPacketHeader
{
    public const int Size = 16;

    public readonly NetworkPacketFlags Flags;
    public readonly ushort HeaderSize;
    public readonly uint OpCode;
    public readonly uint Seq;
    public readonly uint PayloadLength;

    public int PacketLength => (int)PayloadLength;

    public NetworkPacketHeader(NetworkPacketFlags flags, uint opCode, uint seq, uint payloadLength)
    {
        Flags = flags;
        HeaderSize = Size;
        OpCode = opCode;
        Seq = seq;
        PayloadLength = payloadLength;
    }

    public static NetworkPacketHeader Read(ReadOnlySpan<byte> headerBytes)
    {
        var flags = (NetworkPacketFlags)BinaryPrimitives.ReadUInt16LittleEndian(headerBytes.Slice(0, 2));
        var headerSize = BinaryPrimitives.ReadUInt16LittleEndian(headerBytes.Slice(2, 2));
        var opCode = BinaryPrimitives.ReadUInt32LittleEndian(headerBytes.Slice(4, 4));
        var seq = BinaryPrimitives.ReadUInt32LittleEndian(headerBytes.Slice(8, 4));
        var payloadLength = BinaryPrimitives.ReadUInt32LittleEndian(headerBytes.Slice(12, 4));

        if (headerSize != Size) throw new InvalidOperationException($"Unsupported header size: {headerSize}.");

        return new NetworkPacketHeader(flags, opCode, seq, payloadLength);
    }

    public void Write(Span<byte> headerBytes)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(headerBytes.Slice(0, 2), (ushort)Flags);
        BinaryPrimitives.WriteUInt16LittleEndian(headerBytes.Slice(2, 2), HeaderSize);
        BinaryPrimitives.WriteUInt32LittleEndian(headerBytes.Slice(4, 4), OpCode);
        BinaryPrimitives.WriteUInt32LittleEndian(headerBytes.Slice(8, 4), Seq);
        BinaryPrimitives.WriteUInt32LittleEndian(headerBytes.Slice(12, 4), PayloadLength);
    }
}

/// <summary>
/// 网络包标志位
/// </summary>
[System.Flags]
public enum NetworkPacketFlags : ushort
{
    None = 0,
    Compressed = 1 << 0,
    Encrypted = 1 << 1,
    Heartbeat = 1 << 2,
    Request = 1 << 3,
    Response = 1 << 4,
    ServerPush = 1 << 5
}
