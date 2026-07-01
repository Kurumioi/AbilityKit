using System;
using System.Collections.Generic;
using AbilityKit.Combat.MotionSystem.Collision;
using AbilityKit.Combat.MotionSystem.Events;
using AbilityKit.Core.Mathematics;

namespace AbilityKit.Combat.MotionSystem.Core
{
    public sealed class MotionPipeline : IDisposable
    {
        private List<IMotionSource> _sources;
        private Dictionary<int, int> _bestIndexByGroup;
        private List<int> _suppressedGroups;
        private bool _disposed;

        public MotionPipeline()
        {
            _sources = MotionPipelinePool.RentSourceList();
            _bestIndexByGroup = MotionPipelinePool.RentBestIndexDictionary();
            _suppressedGroups = MotionPipelinePool.RentIntList();
        }

        public IMotionSolver Solver { get; set; } = NoMotionSolver.Instance;
        public IMotionEventSink Events { get; set; }

        public MotionPipelinePolicy Policy { get; set; }

        public int SourceCount => _sources?.Count ?? 0;

        public void AddSource(IMotionSource source)
        {
            EnsureNotDisposed();
            if (source == null) throw new ArgumentNullException(nameof(source));
            _sources.Add(source);
            _sources.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }

        public bool RemoveSource(IMotionSource source)
        {
            EnsureNotDisposed();
            if (source == null) return false;
            return _sources.Remove(source);
        }

        public void ClearSources()
        {
            EnsureNotDisposed();
            _sources.Clear();
        }

        public void Reset()
        {
            EnsureNotDisposed();
            _sources.Clear();
            _bestIndexByGroup.Clear();
            _suppressedGroups.Clear();
            Solver = NoMotionSolver.Instance;
            Events = null;
            Policy = null;
        }

        public MotionSolveResult Tick(int id, ref MotionState state, float dt, ref MotionOutput output)
        {
            EnsureNotDisposed();
            output.Clear();

            var desired = Vec3.Zero;

            _bestIndexByGroup.Clear();
            _suppressedGroups.Clear();

            for (int i = _sources.Count - 1; i >= 0; i--)
            {
                var s = _sources[i];
                if (s == null)
                {
                    _sources.RemoveAt(i);
                    continue;
                }

                if (!s.IsActive)
                {
                    _sources.RemoveAt(i);
                    continue;
                }

                if (s.Stacking == MotionStacking.Additive) continue;

                var gid = s.GroupId;
                if (_bestIndexByGroup.TryGetValue(gid, out var bestIdx))
                {
                    if (_sources[bestIdx].Priority < s.Priority)
                    {
                        _bestIndexByGroup[gid] = i;
                    }
                }
                else
                {
                    _bestIndexByGroup.Add(gid, i);
                }
            }

            if (Policy != null)
            {
                foreach (var kv in _bestIndexByGroup)
                {
                    var bestIdx = kv.Value;
                    if (bestIdx < 0 || bestIdx >= _sources.Count) continue;

                    var s = _sources[bestIdx];
                    if (s == null) continue;
                    if (!s.IsActive) continue;

                    if (s.Stacking != MotionStacking.OverrideLowerPriority) continue;

                    if (Policy.TryGetSuppressedGroups(s.GroupId, out var suppressed) && suppressed != null)
                    {
                        for (int j = 0; j < suppressed.Length; j++)
                        {
                            AddSuppressed(suppressed[j]);
                        }
                    }
                }
            }

            for (int i = _sources.Count - 1; i >= 0; i--)
            {
                var s = _sources[i];
                if (s == null) continue;
                if (!s.IsActive) continue;

                if (IsSuppressed(s.GroupId)) continue;

                if (s.Stacking != MotionStacking.Additive)
                {
                    if (_bestIndexByGroup.TryGetValue(s.GroupId, out var bestIdx) && bestIdx != i)
                    {
                        continue;
                    }
                }

                s.Tick(id, ref state, dt, ref desired);

                if (!s.IsActive)
                {
                    NotifyFinished(id, in state, s);
                }
            }

            output.DesiredDelta = desired;

            var solver = Solver ?? NoMotionSolver.Instance;
            var result = solver.Solve(id, state, output, dt);

            output.AppliedDelta = result.AppliedDelta;
            output.NewVelocity = dt > 0f ? result.AppliedDelta / dt : Vec3.Zero;
            output.NewForward = state.Forward;

            state.Position = new Vec3(state.Position.X + result.AppliedDelta.X, state.Position.Y + result.AppliedDelta.Y, state.Position.Z + result.AppliedDelta.Z);
            state.Velocity = output.NewVelocity;
            state.Time += dt;

            if (result.Hit.Hit)
            {
                Events?.OnHit(id, in state, in result.Hit);
            }

            return result;
        }

        private void NotifyFinished(int id, in MotionState state, IMotionSource source)
        {
            if (Events == null) return;

            var evt = MotionFinishEvent.Arrive;
            if (source is IMotionFinishEventSource finish)
            {
                evt = finish.FinishEvent;
            }

            switch (evt)
            {
                case MotionFinishEvent.Expired:
                    Events.OnExpired(id, in state);
                    break;
                case MotionFinishEvent.Arrive:
                    Events.OnArrive(id, in state);
                    break;
            }
        }

        private void AddSuppressed(int groupId)
        {
            for (int i = 0; i < _suppressedGroups.Count; i++)
            {
                if (_suppressedGroups[i] == groupId) return;
            }

            _suppressedGroups.Add(groupId);
        }

        private bool IsSuppressed(int groupId)
        {
            for (int i = 0; i < _suppressedGroups.Count; i++)
            {
                if (_suppressedGroups[i] == groupId) return true;
            }

            return false;
        }

        public void Dispose()
        {
            if (_disposed) return;
            MotionPipelinePool.ReleaseSourceList(_sources);
            MotionPipelinePool.ReleaseBestIndexDictionary(_bestIndexByGroup);
            MotionPipelinePool.ReleaseIntList(_suppressedGroups);
            _sources = null;
            _bestIndexByGroup = null;
            _suppressedGroups = null;
            Solver = NoMotionSolver.Instance;
            Events = null;
            Policy = null;
            _disposed = true;
        }

        private void EnsureNotDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(MotionPipeline));
        }
    }
}
