using System;
using System.Threading;
using System.Threading.Tasks;
using AbilityKit.Core.Logging;
using AbilityKit.Network.Abstractions;
using AbilityKit.Network.Protocol;
using AbilityKit.Network.Runtime;

namespace AbilityKit.Game.Battle.Transport
{
    /// <summary>
    /// 通用网络客户端
    /// 使用 ConnectionManager + RequestClient 实现
    /// 可替换为 TcpNetworkClient 或其他实现
    /// </summary>
    public sealed class GenericNetworkClient : INetworkClient
    {
        private readonly ConnectionManager _connection;
        private readonly RequestClient _requestClient;
        private bool _disposed;

        public bool IsConnected => _connection.IsConnected;

        public event Action OnConnected
        {
            add => _connection.Connected += value;
            remove => _connection.Connected -= value;
        }

        public event Action<string> OnDisconnected;

        public event Action<Exception> OnError
        {
            add => _connection.Error += value;
            remove => _connection.Error -= value;
        }

        private event Action<uint, byte[]> _onServerPush;

        public event Action<uint, byte[]> OnServerPush
        {
            add
            {
                _onServerPush += value;
                _connection.ServerPushReceived += HandleServerPush;
            }
            remove
            {
                _onServerPush -= value;
                if (_onServerPush == null)
                {
                    _connection.ServerPushReceived -= HandleServerPush;
                }
            }
        }

        private void HandleServerPush(uint opCode, ArraySegment<byte> payload)
        {
            byte[] bytes;
            if (payload.Array != null && payload.Count > 0)
            {
                bytes = new byte[payload.Count];
                Buffer.BlockCopy(payload.Array, payload.Offset, bytes, 0, payload.Count);
            }
            else
            {
                bytes = Array.Empty<byte>();
            }
            _onServerPush?.Invoke(opCode, bytes);
        }

        public GenericNetworkClient(Func<ITransport> transportFactory, IFrameCodec frameCodec, IDispatcher dispatcher = null)
        {
            var options = new ConnectionOptions
            {
                FrameCodec = frameCodec
            };
            _connection = new ConnectionManager(transportFactory, options, dispatcher ?? InlineDispatcher.Instance);
            _requestClient = new RequestClient(_connection);
        }

        public void Connect(string host, int port)
        {
            ThrowIfDisposed();
            _connection.Open(host, port);
        }

        public void Disconnect()
        {
            ThrowIfDisposed();
            _connection.Close();
        }

        public async Task<byte[]> SendRequestAsync(uint opCode, byte[] payload, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var segment = payload != null ? new ArraySegment<byte>(payload) : default;
            var response = await _requestClient.SendRequestAsync(opCode, segment, cancellationToken: cancellationToken);

            if (response.Array == null || response.Count == 0)
            {
                return Array.Empty<byte>();
            }

            var result = new byte[response.Count];
            Buffer.BlockCopy(response.Array, response.Offset, result, 0, response.Count);
            return result;
        }

        public Task SendServerPushAsync(uint opCode, byte[] payload, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var segment = payload != null ? new ArraySegment<byte>(payload) : default;
            _connection.Send(opCode, segment, flags: (ushort)NetworkPacketFlags.None);
            return Task.CompletedTask;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(GenericNetworkClient));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _requestClient.Dispose();
            _connection.Dispose();
        }
    }
}
