#nullable enable

using System;
using AbilityKit.Network.Abstractions;
using AbilityKit.Network.Runtime;

namespace AbilityKit.Demo.Shooter.View
{
    public interface IShooterClientConnectionFactory
    {
        IConnection CreateConnection();
    }

    public sealed class ShooterClientConnectionFactory : IShooterClientConnectionFactory
    {
        private readonly Func<IConnection> _connectionFactory;

        public ShooterClientConnectionFactory(Func<IConnection> connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public IConnection CreateConnection()
        {
            var connection = _connectionFactory();
            if (connection == null)
            {
                throw new InvalidOperationException("Shooter client connection factory returned null.");
            }

            return connection;
        }

        public static ShooterClientConnectionFactory FromTransportFactory(Func<ITransport> transportFactory, ConnectionOptions? options = null, IDispatcher? callbackDispatcher = null, IDispatcher? ioDispatcher = null)
        {
            if (transportFactory == null)
            {
                throw new ArgumentNullException(nameof(transportFactory));
            }

            return new ShooterClientConnectionFactory(() =>
                new ConnectionManager(
                    transportFactory,
                    options ?? CreateDefaultOptions(),
                    callbackDispatcher ?? InlineDispatcher.Instance,
                    ioDispatcher ?? InlineDispatcher.Instance));
        }

        public static ShooterClientConnectionFactory Tcp(ConnectionOptions? options = null, IDispatcher? callbackDispatcher = null, IDispatcher? ioDispatcher = null)
        {
            return FromTransportFactory(() => new TcpTransport(), options, callbackDispatcher, ioDispatcher);
        }

        public static ConnectionOptions CreateDefaultOptions()
        {
            return new ConnectionOptions();
        }
    }
}
