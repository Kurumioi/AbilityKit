using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AbilityKit.Network.Protocol;
using AbilityKit.Orleans.Gateway.Abstractions;

namespace AbilityKit.Orleans.Gateway.Networking;

/// <summary>
/// TCP 传输层配置
/// </summary>
public sealed class TcpTransportOptions : GatewayTransportOptions
{
    public int MaxFrameLength { get; set; } = 1024 * 1024;
    public int RequestTimeoutMs { get; set; } = 30000;
}

/// <summary>
/// TCP 传输层会话
/// </summary>
public sealed class TcpTransportSession : IGatewayTransportSession
{
    private readonly TcpTransportServer _server;
    private readonly Stream _stream;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public long ConnectionId { get; }
    public bool IsConnected => _stream.CanWrite;

    internal GatewaySessionContext Context { get; }

    internal TcpTransportSession(long connectionId, TcpTransportServer server, Stream stream)
    {
        ConnectionId = connectionId;
        _server = server;
        _stream = stream;
        Context = new GatewaySessionContext(connectionId);
    }

    public async Task SendResponseAsync(uint opCode, uint seq, byte[] payload, CancellationToken cancellationToken = default)
    {
        if (!_stream.CanWrite) return;

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            var header = new NetworkPacketHeader(NetworkPacketFlags.Response, opCode, seq, (uint)payload.Length);
            await WriteFrameAsync(header, payload, cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task SendServerPushAsync(uint opCode, byte[] payload, CancellationToken cancellationToken = default)
    {
        if (!_stream.CanWrite) return;

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            var header = new NetworkPacketHeader(NetworkPacketFlags.ServerPush, opCode, 0, (uint)payload.Length);
            await WriteFrameAsync(header, payload, cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private async Task WriteFrameAsync(NetworkPacketHeader header, byte[] payload, CancellationToken cancellationToken)
    {
        var frameSize = NetworkFrameCodec.GetFrameSize(payload.Length);
        var frame = new byte[frameSize];
        NetworkFrameCodec.WriteFrame(frame, header, payload);
        await _stream.WriteAsync(frame, cancellationToken);
    }

    internal void Close() => _stream.Close();
}

/// <summary>
/// TCP 传输层服务器
/// </summary>
public sealed class TcpTransportServer : IGatewayTransportServer
{
    public string Name => "TCP";
    public bool IsEnabled => _options.Enabled;

    private readonly TcpTransportOptions _options;
    private readonly IGatewayTransportEvents _events;
    private readonly ILogger<TcpTransportServer> _logger;
    private System.Net.Sockets.TcpListener? _listener;

    public TcpTransportServer(
        IOptions<TcpTransportOptions> options,
        IGatewayTransportEvents events,
        ILogger<TcpTransportServer> logger)
    {
        _options = options.Value;
        _events = events;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("TcpTransport is disabled.");
            return;
        }

        var ip = System.Net.IPAddress.TryParse(_options.Host, out var parsed) ? parsed : System.Net.IPAddress.Any;
        _listener = new System.Net.Sockets.TcpListener(ip, _options.Port);
        _listener.Start();

        _logger.LogInformation("TcpTransport listening on {Host}:{Port}", _options.Host, _options.Port);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(cancellationToken);
                client.NoDelay = true;

                _ = Task.Run(() => HandleClientAsync(client, cancellationToken), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _listener.Stop();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _listener?.Stop();
        return Task.CompletedTask;
    }

    private async Task HandleClientAsync(System.Net.Sockets.TcpClient client, CancellationToken cancellationToken)
    {
        var connectionId = GenerateConnectionId();
        var session = new TcpTransportSession(connectionId, this, client.GetStream());
        _events.OnConnected(session);

        _logger.LogInformation("TCP client connected: ConnectionId={ConnectionId}", connectionId);

        var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(64 * 1024);
        var buffered = 0;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var n = await client.GetStream().ReadAsync(buffer.AsMemory(buffered, buffer.Length - buffered), cancellationToken);
                if (n <= 0) break;

                buffered += n;

                var offset = 0;
                while (true)
                {
                    if (!NetworkFrameCodec.TryParseFrame(
                        new ReadOnlySpan<byte>(buffer, offset, buffered - offset),
                        out var header,
                        out var payloadSpan))
                    {
                        break;
                    }

                    var totalSize = NetworkFrameCodec.GetFrameSize((int)header.PayloadLength);
                    if (totalSize > _options.MaxFrameLength)
                    {
                        _logger.LogError("Frame too large: {Size}", totalSize);
                        break;
                    }

                    _events.OnRequest(connectionId, header.OpCode, header.Seq, payloadSpan.ToArray());

                    offset += totalSize;
                    if (offset >= buffered) break;
                }

                if (offset > 0)
                {
                    Buffer.BlockCopy(buffer, offset, buffer, 0, buffered - offset);
                    buffered -= offset;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TCP client error: ConnectionId={ConnectionId}", connectionId);
        }
        finally
        {
            System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
            _events.OnClosed(connectionId);
            client.Close();
            _logger.LogInformation("TCP client disconnected: ConnectionId={ConnectionId}", connectionId);
        }
    }

    private static long _nextConnectionId;
    private static long GenerateConnectionId() => Interlocked.Increment(ref _nextConnectionId);
}

/// <summary>
/// 传输层事件接口
/// </summary>
public interface IGatewayTransportEvents
{
    void OnConnected(TcpTransportSession session);
    void OnRequest(long connectionId, uint opCode, uint seq, byte[] payload);
    void OnClosed(long connectionId);
}
