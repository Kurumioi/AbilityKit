using System;
using System.Collections.Generic;
using AbilityKit.Ability.StateSync.Snapshot;

namespace AbilityKit.Ability.StateSync.Buffer
{
    public sealed class SnapshotBuffer
    {
        private readonly Dictionary<int, WorldStateSnapshot> _snapshots = new Dictionary<int, WorldStateSnapshot>();
        private readonly List<int> _capturedFrames = new List<int>();
        private readonly int _maxBufferSize;
        private readonly object _lock = new object();

        public int Count => _capturedFrames.Count;
        public int MaxBufferSize => _maxBufferSize;

        public SnapshotBuffer(int maxBufferSize = 128)
        {
            if (maxBufferSize <= 0) throw new ArgumentOutOfRangeException(nameof(maxBufferSize));
            _maxBufferSize = maxBufferSize;
        }

        public void Store(int frame, WorldStateSnapshot snapshot)
        {
            lock (_lock)
            {
                if (_snapshots.ContainsKey(frame))
                {
                    _snapshots[frame] = snapshot.Clone();
                }
                else
                {
                    _snapshots[frame] = snapshot.Clone();
                    _capturedFrames.Add(frame);
                    _capturedFrames.Sort();

                    TrimBuffer();
                }
            }
        }

        public bool TryGet(int frame, out WorldStateSnapshot snapshot)
        {
            lock (_lock)
            {
                if (_snapshots.TryGetValue(frame, out var s))
                {
                    snapshot = s.Clone();
                    return true;
                }
                snapshot = null;
                return false;
            }
        }

        public WorldStateSnapshot Get(int frame)
        {
            TryGet(frame, out var snapshot);
            return snapshot;
        }

        public bool Contains(int frame)
        {
            lock (_lock)
            {
                return _snapshots.ContainsKey(frame);
            }
        }

        public IReadOnlyList<int> GetCapturedFrames()
        {
            lock (_lock)
            {
                return _capturedFrames.ToArray();
            }
        }

        public int GetLatestFrame()
        {
            lock (_lock)
            {
                return _capturedFrames.Count > 0 ? _capturedFrames[_capturedFrames.Count - 1] : -1;
            }
        }

        public int GetEarliestFrame()
        {
            lock (_lock)
            {
                return _capturedFrames.Count > 0 ? _capturedFrames[0] : -1;
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _snapshots.Clear();
                _capturedFrames.Clear();
            }
        }

        public bool Remove(int frame)
        {
            lock (_lock)
            {
                if (_snapshots.Remove(frame))
                {
                    _capturedFrames.Remove(frame);
                    return true;
                }
                return false;
            }
        }

        public void RemoveBefore(int frame)
        {
            lock (_lock)
            {
                var framesToRemove = new List<int>();
                foreach (var f in _capturedFrames)
                {
                    if (f < frame) framesToRemove.Add(f);
                }

                foreach (var f in framesToRemove)
                {
                    _snapshots.Remove(f);
                    _capturedFrames.Remove(f);
                }
            }
        }

        public void RemoveAfter(int frame)
        {
            lock (_lock)
            {
                var framesToRemove = new List<int>();
                foreach (var f in _capturedFrames)
                {
                    if (f > frame) framesToRemove.Add(f);
                }

                foreach (var f in framesToRemove)
                {
                    _snapshots.Remove(f);
                    _capturedFrames.Remove(f);
                }
            }
        }

        private void TrimBuffer()
        {
            while (_capturedFrames.Count > _maxBufferSize)
            {
                int earliestFrame = _capturedFrames[0];
                _snapshots.Remove(earliestFrame);
                _capturedFrames.RemoveAt(0);
            }
        }
    }
}
