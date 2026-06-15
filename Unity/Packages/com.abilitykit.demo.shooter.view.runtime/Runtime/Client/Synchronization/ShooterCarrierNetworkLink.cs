#nullable enable

using System;
using AbilityKit.Demo.Shooter.Runtime;
using AbilityKit.Network.Abstractions;
using AbilityKit.Network.Protocol;
using AbilityKit.Network.Runtime;
using AbilityKit.Network.Runtime.Conditioning;
using AbilityKit.Protocol.Room;
using AbilityKit.Protocol.Shooter;

namespace AbilityKit.Demo.Shooter.View
{
    /// <summary>
    /// Routes Shooter authoritative snapshot pushes through the shared network middleware pipeline
    /// before dispatching them to a client sync controller.
    /// </summary>
    public sealed class ShooterCarrierNetworkLink
    {
        private readonly IShooterClientSyncController _controller;
        private readonly NetworkConditioningMiddleware _conditioning;
        private readonly NetworkPipeline _pipeline;
        private readonly LoopbackSessionContext _context = new LoopbackSessionContext();

        private uint _sequence;
        private long _clockMs;
        private ShooterSnapshotApplyResult _lastApplyResult = ShooterSnapshotApplyResult.Ignored;

        public ShooterCarrierNetworkLink(
            IShooterClientSyncController controller,
            NetworkConditionProfile profile,
            int seed = 0)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _conditioning = new NetworkConditioningMiddleware(profile, () => _clockMs, seed);
            _pipeline = new NetworkPipeline();
            _pipeline.Add(_conditioning);
        }

        public NetworkConditioningStats Stats => _conditioning.GetStats();

        public ShooterSnapshotApplyResult LastApplyResult => _lastApplyResult;

        public void PublishSnapshot(in ShooterPackedSnapshotPayload snapshot, double timestamp)
        {
            var isDelta = (snapshot.SnapshotFlags & ShooterPackedSnapshotFlags.Delta) != 0u;
            var payloadOpCode = isDelta
                ? ShooterOpCodes.Snapshot.PackedStateDelta
                : ShooterOpCodes.Snapshot.PackedState;
            var pushOpCode = isDelta
                ? RoomGatewayOpCodes.DeltaSnapshotPushed
                : RoomGatewayOpCodes.SnapshotPushed;

            var packedBytes = ShooterPackedSnapshotCodec.Serialize(in snapshot);
            var wire = new WireStateSyncSnapshotPush
            {
                WorldId = snapshot.WorldId,
                Frame = snapshot.Frame,
                Timestamp = timestamp,
                IsFullSnapshot = !isDelta,
                Actors = null,
                PayloadOpCode = payloadOpCode,
                Payload = packedBytes,
                ServerTicks = snapshot.ServerTick
            };

            var pushPayload = WireRoomGatewayBinary.Serialize(in wire);
            var header = new NetworkPacketHeader(
                NetworkPacketFlags.None,
                pushOpCode,
                ++_sequence,
                (uint)pushPayload.Count);

            _pipeline.ProcessInbound(_context, header, pushPayload, DeliverToController);
        }

        public void Advance(long clockMs)
        {
            _clockMs = clockMs;
            _conditioning.Advance(clockMs);
        }

        private void DeliverToController(NetworkPacketHeader header, ArraySegment<byte> payload)
        {
            _lastApplyResult = _controller.ApplyGatewayPush(header.OpCode, payload);
        }

        private sealed class LoopbackSessionContext : ISessionContext, ISession, IDispatcher
        {
            public ISession Session => this;

            public IDispatcher Dispatcher => this;

            public bool IsConnected => true;

            public event Action? Connected;

            public event Action? Disconnected;

            public event Action<Exception>? Error;

            public event Action<uint, uint, ArraySegment<byte>>? PacketReceived;

            public event Action<uint, ArraySegment<byte>>? ServerPushReceived;

            public void Send(NetworkPacketHeader header, ArraySegment<byte> payload)
            {
                PacketReceived?.Invoke(header.OpCode, header.Seq, payload);
                ServerPushReceived?.Invoke(header.OpCode, payload);
            }

            public void Post(Action action)
            {
                action?.Invoke();
            }

            public void Start()
            {
                Connected?.Invoke();
            }

            public void Stop()
            {
                Disconnected?.Invoke();
            }

            public void Send(uint opCode, ArraySegment<byte> payload, ushort flags = 0, uint seq = 0)
            {
                PacketReceived?.Invoke(opCode, seq, payload);
                ServerPushReceived?.Invoke(opCode, payload);
            }

            public void Dispose()
            {
            }
        }
    }
}
