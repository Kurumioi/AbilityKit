using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;

namespace AbilityKit.Demo.Moba.Services
{
    [WorldService(typeof(MobaSnapshotRouter))]
    [WorldService(typeof(IWorldStateSnapshotProvider))]
    [WorldService(typeof(IMobaSnapshotHealthProvider))]
    public sealed class MobaSnapshotRouter : IWorldStateSnapshotProvider, IMobaSnapshotBatchProvider, IMobaSnapshotHealthProvider, IWorldInitializable
    {
        private const string MetricRequest = "moba.snapshot.request";
        private const string MetricBatchRequest = "moba.snapshot.batch.request";
        private const string MetricHit = "moba.snapshot.hit";
        private const string MetricEmpty = "moba.snapshot.empty";
        private const string MetricEmitterCount = "moba.snapshot.emitters";
        private const string MetricBatchSize = "moba.snapshot.batch.size";
        private const string WarningNoEmitters = "snapshot.no.emitters";

        private List<IMobaSnapshotEmitter> _emitters;
        private List<MobaSnapshotEmitterHealthEntry> _emitterHealthEntries;
        private IMobaBattleDiagnosticsService _diagnostics;
        private long _singleRequests;
        private long _batchRequests;
        private long _hitCount;
        private long _emptyCount;
        private int _lastFrame;
        private int _lastSnapshotOpCode;
        private int _lastBatchSnapshotCount;
        private bool _usedAttributeRegistry;

        public MobaSnapshotRouter()
        {
            _emitters = new List<IMobaSnapshotEmitter>(8);
            _emitterHealthEntries = new List<MobaSnapshotEmitterHealthEntry>(8);
            _lastFrame = -1;
            _lastSnapshotOpCode = 0;
            _lastBatchSnapshotCount = 0;
        }
 
        public void OnInit(IWorldResolver services)
        {
            services?.TryResolve(out _diagnostics);

            var registry = MobaSnapshotEmitterRegistry.CreateDefault();
            var resolved = registry.ResolveEmitters(services);
            _emitters = resolved;
            RebuildEmitterHealthEntries();
            _usedAttributeRegistry = resolved.Count > 0;

            RecordEmitterCount();
        }
 
        public bool TryGetSnapshot(FrameIndex frame, out WorldStateSnapshot snapshot)
        {
            _singleRequests++;
            _lastFrame = frame.Value;
            _diagnostics?.Counter(MetricRequest);

            for (int i = 0; i < _emitters.Count; i++)
            {
                if (!_emitters[i].TryGetSnapshot(frame, out snapshot)) continue;

                RecordHit(snapshot.OpCode, 1);
                return true;
            }
 
            snapshot = default;
            RecordEmpty();
            return false;
        }
 
        public int CollectSnapshots(FrameIndex frame, IList<WorldStateSnapshot> snapshots, int maxSnapshots = 32)
        {
            _batchRequests++;
            _lastFrame = frame.Value;
            _diagnostics?.Counter(MetricBatchRequest);

            if (snapshots == null)
            {
                throw new ArgumentNullException(nameof(snapshots));
            }

            if (maxSnapshots <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxSnapshots), maxSnapshots, "maxSnapshots must be positive.");
            }
 
            int count = 0;
            int lastOpCode = 0;
            for (int i = 0; i < _emitters.Count && count < maxSnapshots; i++)
            {
                if (!_emitters[i].TryGetSnapshot(frame, out WorldStateSnapshot snapshot)) continue;
 
                snapshots.Add(snapshot);
                count++;
                lastOpCode = snapshot.OpCode;
            }

            if (count > 0) RecordHit(lastOpCode, count);
            else RecordEmpty();
 
            return count;
        }

        public MobaSnapshotRouterHealth GetHealth()
        {
            return new MobaSnapshotRouterHealth(_emitters.Count, _singleRequests, _batchRequests, _hitCount, _emptyCount, _lastFrame, _lastSnapshotOpCode, _lastBatchSnapshotCount, _usedAttributeRegistry, _emitterHealthEntries);
        }

        private void RecordHit(int opCode, int batchCount)
        {
            _hitCount++;
            _lastSnapshotOpCode = opCode;
            _lastBatchSnapshotCount = batchCount;
            _diagnostics?.Counter(MetricHit);
            _diagnostics?.Gauge(MetricBatchSize, batchCount);
        }

        private void RecordEmpty()
        {
            _emptyCount++;
            _lastBatchSnapshotCount = 0;
            _diagnostics?.Counter(MetricEmpty);
        }

        private void RebuildEmitterHealthEntries()
        {
            _emitterHealthEntries.Clear();
            if (_emitters == null) return;

            for (int i = 0; i < _emitters.Count; i++)
            {
                var emitter = _emitters[i];
                if (emitter == null) continue;

                _emitterHealthEntries.Add(new MobaSnapshotEmitterHealthEntry(emitter.GetType()));
            }
        }

        private void RecordEmitterCount()
        {
            var count = _emitters != null ? _emitters.Count : 0;
            _diagnostics?.Gauge(MetricEmitterCount, count);
            if (count == 0)
            {
                _diagnostics?.Warning(WarningNoEmitters, "[MobaSnapshotRouter] No snapshot emitters resolved; battle state output will be empty.");
            }
        }
 
        public void Dispose()
        {
            _diagnostics = null;
            _emitters?.Clear();
            _emitterHealthEntries?.Clear();
        }
    }
}
