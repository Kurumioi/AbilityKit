using System;
using System.Collections.Generic;
using AbilityKit.Network.Abstractions;

namespace AbilityKit.Network.Runtime
{
    public sealed class AbilityKitConnectionRegistry : IAbilityKitConnectionRegistry
    {
        private readonly Dictionary<AbilityKitConnectionRole, IConnection> _connections = new Dictionary<AbilityKitConnectionRole, IConnection>();
        private bool _disposed;

        public bool TryGet(AbilityKitConnectionRole role, out IConnection connection)
        {
            ThrowIfDisposed();
            return _connections.TryGetValue(role, out connection);
        }

        public IConnection GetRequired(AbilityKitConnectionRole role)
        {
            ThrowIfDisposed();
            if (_connections.TryGetValue(role, out var connection)) return connection;
            throw new InvalidOperationException($"Connection role '{role}' is not registered.");
        }

        public void Register(AbilityKitConnectionRole role, IConnection connection, bool disposeOnReplace = true)
        {
            ThrowIfDisposed();
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            if (_connections.TryGetValue(role, out var existing) && !ReferenceEquals(existing, connection))
            {
                if (disposeOnReplace)
                {
                    existing.Dispose();
                }
            }

            _connections[role] = connection;
        }

        public IConnection GetOrCreate(AbilityKitConnectionDescriptor descriptor, Func<AbilityKitConnectionDescriptor, IConnection> factory)
        {
            ThrowIfDisposed();
            if (_connections.TryGetValue(descriptor.Role, out var connection)) return connection;
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            connection = factory(descriptor);
            if (connection == null)
            {
                throw new InvalidOperationException($"Connection factory returned null for role '{descriptor.Role}'.");
            }

            _connections.Add(descriptor.Role, connection);
            return connection;
        }

        public bool Remove(AbilityKitConnectionRole role, bool dispose = true)
        {
            ThrowIfDisposed();
            if (!_connections.TryGetValue(role, out var connection)) return false;

            _connections.Remove(role);
            if (dispose)
            {
                connection.Dispose();
            }

            return true;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var pair in _connections)
            {
                pair.Value.Dispose();
            }

            _connections.Clear();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(AbilityKitConnectionRegistry));
        }
    }
}
