using System;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Protocol.Moba.StateSync;

namespace AbilityKit.Demo.Moba.Services
{
    [MobaSnapshotEmitter(70)]
    [WorldService(typeof(MobaStateHashSnapshotService))]
    public sealed class MobaStateHashSnapshotService : IService, IMobaSnapshotEmitter
    {
        private const int DefaultIntervalFrames = 10;
        private static readonly Comparison<StateHashEntry> CompareEntriesByActorId =
            (a, b) => a.ActorId.CompareTo(b.ActorId);

        private readonly MobaGamePhaseService _phase;
        private readonly MobaActorRegistry _registry;
        private readonly MobaSnapshotBuffer<StateHashEntry> _entries = new MobaSnapshotBuffer<StateHashEntry>(16, 256);

        private FrameIndex _lastFrame;

        public int IntervalFrames { get; set; } = DefaultIntervalFrames;

        public MobaStateHashSnapshotService(MobaGamePhaseService phase, MobaActorRegistry registry)
        {
            _phase = phase ?? throw new ArgumentNullException(nameof(phase));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _lastFrame = new FrameIndex(-999999);
        }

        public bool TryGetSnapshot(FrameIndex frame, out WorldStateSnapshot snapshot)
        {
            if (!_phase.InGame)
            {
                snapshot = default;
                return false;
            }

            if (frame.Value == _lastFrame.Value)
            {
                snapshot = default;
                return false;
            }

            var interval = IntervalFrames;
            if (interval <= 0) interval = DefaultIntervalFrames;

            if ((frame.Value % interval) != 0)
            {
                snapshot = default;
                return false;
            }

            _lastFrame = frame;

            var hash = ComputeStateHash();
            var payload = MobaStateHashSnapshotCodec.Serialize(frame.Value, hash);
            snapshot = new WorldStateSnapshot((int)MobaOpCode.StateHashSnapshot, payload);
            return true;
        }

        private uint ComputeStateHash()
        {
            _entries.Clear();
            foreach (var kv in _registry.Entries)
            {
                var actorId = kv.Key;
                var e = kv.Value;
                if (e == null) continue;
                if (!e.hasTransform) continue;
                var p = e.transform.Value.Position;
                _entries.Add(new StateHashEntry(actorId, p.X, p.Y, p.Z));
            }

            _entries.Sort(CompareEntriesByActorId);

            var entries = _entries.Items;
            uint h = 2166136261u;

            AddByte(ref h, _phase.InGame ? (byte)1 : (byte)0);
            AddInt(ref h, entries.Count);

            for (int i = 0; i < entries.Count; i++)
            {
                var it = entries[i];
                AddInt(ref h, it.ActorId);
                AddFloat(ref h, it.X);
                AddFloat(ref h, it.Y);
                AddFloat(ref h, it.Z);
            }

            _entries.ClearAndTrim();
            return h;
        }

        private static void AddByte(ref uint h, byte v)
        {
            h ^= v;
            h *= 16777619u;
        }

        private static void AddInt(ref uint h, int v)
        {
            unchecked
            {
                AddUInt(ref h, (uint)v);
            }
        }

        private static void AddUInt(ref uint h, uint v)
        {
            AddByte(ref h, (byte)(v & 0xFF));
            AddByte(ref h, (byte)((v >> 8) & 0xFF));
            AddByte(ref h, (byte)((v >> 16) & 0xFF));
            AddByte(ref h, (byte)((v >> 24) & 0xFF));
        }

        private static void AddFloat(ref uint h, float v)
        {
            var bits = BitConverter.SingleToInt32Bits(v);
            AddInt(ref h, bits);
        }

        public void Dispose()
        {
            _entries.Clear();
            _lastFrame = new FrameIndex(-999999);
        }

        private readonly struct StateHashEntry
        {
            public readonly int ActorId;
            public readonly float X;
            public readonly float Y;
            public readonly float Z;

            public StateHashEntry(int actorId, float x, float y, float z)
            {
                ActorId = actorId;
                X = x;
                Y = y;
                Z = z;
            }
        }
    }
}
