using System;
using AbilityKit.Ability.World.Services;

namespace AbilityKit.Coordinator
{
    /// <summary>
    /// 由环境提供的远程战斗同步传输端口。
    /// Coordinator 负责同步编排，具体环境通过该端口承载 grain 调用、
    /// gateway 请求、socket 或测试替身。
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
    /// 显式的空传输实现，用于环境尚未提供远程网络时。
    /// 它让远程适配器保持确定性且处于断开状态。
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
