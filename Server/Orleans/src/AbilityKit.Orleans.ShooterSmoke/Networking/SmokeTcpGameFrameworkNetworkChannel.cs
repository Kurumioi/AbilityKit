using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using AbilityKit.GameFramework.Network;
using AbilityKit.Network.Protocol;
using AbilityKit.Network.Runtime;
using GameFramework.Network;
internal sealed class SmokeTcpGameFrameworkNetworkChannel : INetworkChannel, IDisposable
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<int, IPacketHandler> _handlers = new();
    private TcpClient? _client;
    private NetworkStream? _stream;
    private EventHandler<Packet>? _defaultHandler;
    private CancellationTokenSource? _readCts;
    private Task? _readTask;
    private int _sentPacketCount;
    private int _receivedPacketCount;

    public SmokeTcpGameFrameworkNetworkChannel(string name)
    {
        Name = string.IsNullOrWhiteSpace(name) ? "ShooterSmokeGateway" : name;
    }

    public string Name { get; }

    public Socket Socket => _client?.Client ?? throw new InvalidOperationException("Channel is not connected.");

    public bool Connected => _client?.Connected == true;

    public ServiceType ServiceType => ServiceType.Tcp;

    public global::GameFramework.Network.AddressFamily AddressFamily => global::GameFramework.Network.AddressFamily.IPv4;

    public int SendPacketCount => 0;

    public int SentPacketCount => Volatile.Read(ref _sentPacketCount);

    public int ReceivePacketCount => 0;

    public int ReceivedPacketCount => Volatile.Read(ref _receivedPacketCount);

    public bool ResetHeartBeatElapseSecondsWhenReceivePacket { get; set; }

    public int MissHeartBeatCount => 0;

    public float HeartBeatInterval { get; set; }

    public float HeartBeatElapseSeconds => 0f;

    public void RegisterHandler(IPacketHandler handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        lock (_syncRoot)
        {
            _handlers[handler.Id] = handler;
        }
    }

    public void SetDefaultHandler(EventHandler<Packet> handler)
    {
        lock (_syncRoot)
        {
            _defaultHandler = handler;
        }
    }

    public void Connect(IPAddress ipAddress, int port)
    {
        Connect(ipAddress, port, new object());
    }

    public void Connect(IPAddress ipAddress, int port, object userData)
    {
        if (ipAddress == null) throw new ArgumentNullException(nameof(ipAddress));
        if (Connected) return;

        _client = new TcpClient(ipAddress.AddressFamily)
        {
            NoDelay = true
        };
        _client.Connect(ipAddress, port);
        _stream = _client.GetStream();
        _readCts = new CancellationTokenSource();
        _readTask = Task.Run(() => ReadLoopAsync(_readCts.Token));
    }

    public void Close()
    {
        _readCts?.Cancel();
        _stream?.Dispose();
        _client?.Close();
        _stream = null;
        _client = null;
    }

    public void Send<T>(T packet) where T : Packet
    {
        if (packet is not AbilityKitGatewayPacket gatewayPacket)
        {
            throw new InvalidOperationException($"Unsupported packet type: {typeof(T).FullName}");
        }

        var stream = _stream ?? throw new InvalidOperationException("Channel is not connected.");
        var payload = gatewayPacket.Payload;
        var payloadSpan = payload.Array == null
            ? ReadOnlySpan<byte>.Empty
            : new ReadOnlySpan<byte>(payload.Array, payload.Offset, payload.Count);
        var frame = new byte[NetworkFrameCodec.GetFrameSize(payloadSpan.Length)];
        var header = new NetworkPacketHeader(gatewayPacket.Header.Flags, gatewayPacket.Header.OpCode, gatewayPacket.Header.Seq, (uint)payloadSpan.Length);
        NetworkFrameCodec.WriteFrame(frame, header, payloadSpan);
        stream.Write(frame, 0, frame.Length);
        stream.Flush();
        Interlocked.Increment(ref _sentPacketCount);
    }

    public void Dispose()
    {
        Close();
        _readCts?.Dispose();
    }

    private async Task ReadLoopAsync(CancellationToken cancellationToken)
    {
        var lengthPrefix = new byte[4];
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var stream = _stream;
                if (stream == null)
                {
                    return;
                }

                await ReadExactlyAsync(stream, lengthPrefix, cancellationToken);
                var frameLength = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(lengthPrefix));
                if (frameLength < NetworkPacketHeader.Size)
                {
                    throw new InvalidOperationException($"Invalid frame length: {frameLength}.");
                }

                var frame = new byte[4 + frameLength];
                Buffer.BlockCopy(lengthPrefix, 0, frame, 0, lengthPrefix.Length);
                await ReadExactlyAsync(stream, frame.AsMemory(4, frameLength), cancellationToken);
                if (!NetworkFrameCodec.TryParseFrame(frame, out var header, out var payloadSpan))
                {
                    throw new InvalidOperationException("Failed to parse gateway frame.");
                }

                var payload = payloadSpan.ToArray();
                var packet = new AbilityKitGatewayPacket(header, new ArraySegment<byte>(payload));
                Interlocked.Increment(ref _receivedPacketCount);
                Dispatch(packet);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        catch (IOException)
        {
        }
    }

    private static async Task ReadExactlyAsync(NetworkStream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        await ReadExactlyAsync(stream, buffer.AsMemory(0, buffer.Length), cancellationToken);
    }

    private static async Task ReadExactlyAsync(NetworkStream stream, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.Slice(offset), cancellationToken);
            if (read <= 0)
            {
                throw new IOException("Remote gateway closed the connection.");
            }

            offset += read;
        }
    }

    private void Dispatch(AbilityKitGatewayPacket packet)
    {
        EventHandler<Packet>? defaultHandler;
        IPacketHandler? handler;
        lock (_syncRoot)
        {
            _handlers.TryGetValue(packet.Id, out handler);
            defaultHandler = _defaultHandler;
        }

        if (handler != null)
        {
            handler.Handle(this, packet);
            return;
        }

        defaultHandler?.Invoke(this, packet);
    }
}
