using System;

namespace AbilityKit.Core.Common.SnapshotRouting
{
    /// <summary>
    /// Minimal interface for snapshot dispatching, allowing View Runtime to provide
    /// its own FrameSnapshotDispatcher implementation that subscribes to BattleLogicSession.
    /// </summary>
    public interface ISnapshotDispatcher : ISnapshotDecoderRegistry
    {
        /// <summary>
        /// Event fired when a snapshot envelope is received.
        /// </summary>
        event Action<AbilityKit.Ability.Host.ISnapshotEnvelope, AbilityKit.Ability.Host.WorldStateSnapshot> SnapshotReceived;

        /// <summary>
        /// Subscribe to a specific snapshot type.
        /// </summary>
        IDisposable Subscribe<T>(int opCode, Action<AbilityKit.Ability.Host.ISnapshotEnvelope, T> handler);
    }
}
