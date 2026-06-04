using System;
using AbilityKit.Ability.World.Services;

namespace AbilityKit.Coordinator
{
    /// <summary>
    /// Environment-provided transport for remote battle synchronization.
    /// Coordinator owns sync orchestration; concrete environments own grain calls,
    /// gateway requests, sockets, or test doubles behind this port.
    /// </summary>
    public interface IRemoteBattleSyncTransport : IService
    {
        bool IsConnected { get; }

        event Action<bool> OnConnectionChanged;

        event Action<int, SnapshotEntityState[]> OnServerSnapshot;

        event Action<int, SnapshotEntityState[]> OnServerConfirmation;

        bool Connect(NetworkEndpoint endpoint, long roomId, long playerId, Core.SyncMode syncMode);

        void Disconnect();

        void Tick(float deltaTime);

        bool SubmitInput(PlayerInput input);
    }

    /// <summary>
    /// Explicit no-transport implementation used when an environment has not provided
    /// remote networking yet. It keeps remote adapters deterministic and disconnected.
    /// </summary>
    public sealed class NullRemoteBattleSyncTransport : IRemoteBattleSyncTransport
    {
        public static readonly NullRemoteBattleSyncTransport Instance = new NullRemoteBattleSyncTransport();

        public bool IsConnected => false;

        public event Action<bool> OnConnectionChanged
        {
            add { }
            remove { }
        }

        public event Action<int, SnapshotEntityState[]> OnServerSnapshot
        {
            add { }
            remove { }
        }

        public event Action<int, SnapshotEntityState[]> OnServerConfirmation
        {
            add { }
            remove { }
        }

        private NullRemoteBattleSyncTransport()
        {
        }

        public bool Connect(NetworkEndpoint endpoint, long roomId, long playerId, Core.SyncMode syncMode)
        {
            return false;
        }

        public void Disconnect()
        {
        }

        public void Tick(float deltaTime)
        {
        }

        public bool SubmitInput(PlayerInput input)
        {
            return false;
        }

        public void Dispose()
        {
        }
    }
}
