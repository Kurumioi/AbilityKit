using System;
using System.Collections.Generic;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Demo.Moba.Components;
using AbilityKit.Trace;
using AbilityKit.Core.Common.Log;

namespace AbilityKit.Demo.Moba.Services
{
    [WorldService(typeof(MobaSkillCastRuntimeService))]
    public sealed class MobaSkillCastRuntimeService : IService
    {
        private readonly Dictionary<long, MobaSkillCastRuntime> _runtimes = new Dictionary<long, MobaSkillCastRuntime>();
        private readonly Dictionary<long, long> _runtimeByTraceContextId = new Dictionary<long, long>();
        private readonly Dictionary<long, MobaSkillRuntimeRetainHandle> _retains = new Dictionary<long, MobaSkillRuntimeRetainHandle>();
        private readonly List<long> _endingBuffer = new List<long>(8);
        private long _nextRuntimeId = 1L;
        private long _nextRetainId = 1L;
        private int _nextGeneration = 1;

        [WorldInject(required: false)]
        private MobaTraceRegistry _trace;

        public int Count => _runtimes.Count;

        public MobaSkillCastRuntime Create(in MobaSkillCastRuntimeCreateRequest request)
        {
            var runtimeId = _nextRuntimeId++;
            var generation = _nextGeneration++;
            if (_nextGeneration <= 0) _nextGeneration = 1;
            var runtime = new MobaSkillCastRuntime(runtimeId, generation, in request);
            _runtimes.Add(runtimeId, runtime);

            if (runtime.RootTraceContextId != 0L)
            {
                _runtimeByTraceContextId[runtime.RootTraceContextId] = runtimeId;
            }

            return runtime;
        }

        public bool TryCreate(in MobaSkillCastRuntimeCreateRequest request, out MobaSkillCastRuntimeHandle handle)
        {
            var runtime = Create(in request);
            handle = runtime.Handle;
            return handle.IsValid;
        }

        public bool TryGet(long runtimeId, out MobaSkillCastRuntime runtime)
        {
            if (runtimeId == 0L)
            {
                runtime = null;
                return false;
            }

            return _runtimes.TryGetValue(runtimeId, out runtime) && runtime != null && !runtime.IsEnded;
        }

        public bool TryGet(in MobaSkillCastRuntimeHandle handle, out MobaSkillCastRuntime runtime)
        {
            runtime = null;
            if (!handle.IsValid) return false;
            if (!TryGet(handle.RuntimeId, out var found)) return false;
            if (found.Generation != handle.Generation) return false;
            runtime = found;
            return true;
        }

        public bool TryGetByTraceContext(long traceContextId, out MobaSkillCastRuntime runtime)
        {
            runtime = null;
            if (traceContextId == 0L) return false;
            return _runtimeByTraceContextId.TryGetValue(traceContextId, out var runtimeId) && TryGet(runtimeId, out runtime);
        }

        public bool UpdateStage(long runtimeId, SkillCastStage stage)
        {
            if (!TryGet(runtimeId, out var runtime)) return false;
            runtime.Stage = stage;
            return true;
        }

        public bool UpdateInput(long runtimeId, in AbilityKit.Core.Math.Vec3 aimPos, in AbilityKit.Core.Math.Vec3 aimDir, int targetActorId)
        {
            if (!TryGet(runtimeId, out var runtime)) return false;
            runtime.UpdateInput(in aimPos, in aimDir, targetActorId);
            return true;
        }

        public bool TryGetBlackboard(in MobaSkillCastRuntimeHandle handle, out MobaSkillRuntimeBlackboard blackboard)
        {
            blackboard = null;
            if (!TryGet(in handle, out var runtime)) return false;
            blackboard = runtime.Blackboard;
            return blackboard != null;
        }

        public bool SetBlackboardValue(in MobaSkillCastRuntimeHandle handle, in MobaSkillRuntimeBlackboardKey key, in MobaSkillRuntimeValue value)
        {
            return TryGetBlackboard(in handle, out var blackboard) && blackboard.Set(in key, in value);
        }

        public bool TryGetBlackboardValue(in MobaSkillCastRuntimeHandle handle, in MobaSkillRuntimeBlackboardKey key, out MobaSkillRuntimeValue value)
        {
            value = default;
            return TryGetBlackboard(in handle, out var blackboard) && blackboard.TryGet(in key, out value);
        }

        public int AddBlackboardInt(in MobaSkillCastRuntimeHandle handle, in MobaSkillRuntimeBlackboardKey key, int delta = 1)
        {
            return TryGetBlackboard(in handle, out var blackboard) ? blackboard.AddInt(in key, delta) : 0;
        }

        public bool AddBlackboardActorId(in MobaSkillCastRuntimeHandle handle, in MobaSkillRuntimeBlackboardKey key, int actorId)
        {
            return TryGetBlackboard(in handle, out var blackboard) && blackboard.AddActorId(in key, actorId);
        }

        public bool AddBlackboardContextId(in MobaSkillCastRuntimeHandle handle, in MobaSkillRuntimeBlackboardKey key, long contextId)
        {
            return TryGetBlackboard(in handle, out var blackboard) && blackboard.AddContextId(in key, contextId);
        }

        public bool RetainChild(long runtimeId, in MobaSkillRuntimeChildRef child)
        {
            if (!TryGet(runtimeId, out var runtime)) return false;
            return runtime.RetainChild(in child);
        }

        public bool RetainChild(in MobaSkillCastRuntimeHandle runtimeHandle, in MobaSkillRuntimeChildRef child, out MobaSkillRuntimeRetainHandle retainHandle)
        {
            retainHandle = default;
            if (!TryGet(in runtimeHandle, out var runtime)) return false;
            if (!runtime.RetainChild(in child)) return false;

            var retainId = _nextRetainId++;
            if (_nextRetainId == 0L) _nextRetainId = 1L;
            retainHandle = new MobaSkillRuntimeRetainHandle(retainId, runtime.Handle, in child);
            _retains.Add(retainId, retainHandle);
            return true;
        }

        public bool ReleaseChild(long runtimeId, in MobaSkillRuntimeChildRef child)
        {
            if (!TryGet(runtimeId, out var runtime)) return false;
            var released = runtime.ReleaseChild(in child);
            if (released)
            {
                TryFinalize(runtime);
            }

            return released;
        }

        public bool ReleaseChild(in MobaSkillRuntimeRetainHandle retainHandle)
        {
            if (!retainHandle.IsValid) return false;
            if (!_retains.TryGetValue(retainHandle.RetainId, out var stored)) return false;
            if (!stored.Equals(retainHandle)) return false;

            var runtimeHandle = retainHandle.Runtime;
            var child = retainHandle.Child;
            if (!TryGet(in runtimeHandle, out var runtime)) return false;

            _retains.Remove(retainHandle.RetainId);
            var released = runtime.ReleaseChild(in child);
            if (released)
            {
                TryFinalize(runtime);
            }

            return released;
        }

        public bool MarkPipelineEnded(long runtimeId, MobaSkillRuntimeEndReason reason)
        {
            if (!TryGet(runtimeId, out var runtime)) return false;
            runtime.PipelineEnded = true;
            runtime.Stage = ToStage(reason);
            runtime.EndReason = reason == MobaSkillRuntimeEndReason.None ? MobaSkillRuntimeEndReason.PipelineCompleted : reason;
            TryFinalize(runtime);
            return true;
        }

        public bool MarkPipelineEnded(in MobaSkillCastRuntimeHandle handle, MobaSkillRuntimeEndReason reason)
        {
            if (!TryGet(in handle, out var runtime)) return false;
            runtime.PipelineEnded = true;
            runtime.Stage = ToStage(reason);
            runtime.EndReason = reason == MobaSkillRuntimeEndReason.None ? MobaSkillRuntimeEndReason.PipelineCompleted : reason;
            TryFinalize(runtime);
            return true;
        }

        public bool Cancel(long runtimeId, MobaSkillRuntimeEndReason reason = MobaSkillRuntimeEndReason.Cancelled)
        {
            if (!TryGet(runtimeId, out var runtime)) return false;
            runtime.PipelineEnded = true;
            runtime.Stage = SkillCastStage.Cancelled;
            runtime.EndReason = reason == MobaSkillRuntimeEndReason.None ? MobaSkillRuntimeEndReason.Cancelled : reason;
            TryFinalize(runtime);
            return true;
        }

        public bool Cancel(in MobaSkillCastRuntimeHandle handle, MobaSkillRuntimeEndReason reason = MobaSkillRuntimeEndReason.Cancelled)
        {
            if (!TryGet(in handle, out var runtime)) return false;
            runtime.PipelineEnded = true;
            runtime.Stage = SkillCastStage.Cancelled;
            runtime.EndReason = reason == MobaSkillRuntimeEndReason.None ? MobaSkillRuntimeEndReason.Cancelled : reason;
            TryFinalize(runtime);
            return true;
        }

        public bool ForceTerminate(in MobaSkillCastRuntimeHandle handle, MobaSkillRuntimeEndReason reason = MobaSkillRuntimeEndReason.RollbackCleanup)
        {
            if (!TryGet(in handle, out var runtime)) return false;
            runtime.PipelineEnded = true;
            runtime.Stage = SkillCastStage.Cancelled;
            runtime.EndReason = reason == MobaSkillRuntimeEndReason.None ? MobaSkillRuntimeEndReason.RollbackCleanup : reason;
            TryFinalize(runtime, force: true);
            return true;
        }

        public void Clear()
        {
            _runtimes.Clear();
            _runtimeByTraceContextId.Clear();
            _retains.Clear();
            _endingBuffer.Clear();
            _nextRuntimeId = 1L;
            _nextRetainId = 1L;
            _nextGeneration = 1;
        }

        public void Dispose()
        {
            Clear();
        }

        private void TryFinalize(MobaSkillCastRuntime runtime, bool force = false)
        {
            if (runtime == null || runtime.IsEnded || runtime.IsEnding) return;
            if (!force && (!runtime.PipelineEnded || runtime.PendingChildren > 0)) return;

            runtime.IsEnding = true;
            var reason = runtime.EndReason == MobaSkillRuntimeEndReason.None ? MobaSkillRuntimeEndReason.PipelineCompleted : runtime.EndReason;
            runtime.NotifyEnding(reason);

            if (runtime.RootTraceContextId != 0L)
            {
                try
                {
                    _trace?.EndContext(runtime.RootTraceContextId, ToTraceReason(reason));
                }
                catch (Exception ex)
                {
                    Log.Exception(ex, $"[MobaSkillCastRuntimeService] Trace.EndContext failed (runtimeId={runtime.RuntimeId}, rootTraceContextId={runtime.RootTraceContextId}, reason={reason})");
                }

                _runtimeByTraceContextId.Remove(runtime.RootTraceContextId);
            }

            RemoveRetains(runtime.Handle);
            _endingBuffer.Add(runtime.RuntimeId);
            runtime.IsEnded = true;
            runtime.IsEnding = false;
            FlushEnded();
        }

        private void FlushEnded()
        {
            if (_endingBuffer.Count == 0) return;
            for (int i = 0; i < _endingBuffer.Count; i++)
            {
                _runtimes.Remove(_endingBuffer[i]);
            }
            _endingBuffer.Clear();
        }

        private void RemoveRetains(in MobaSkillCastRuntimeHandle handle)
        {
            if (!handle.IsValid || _retains.Count == 0) return;

            var removeIds = new List<long>();
            foreach (var kv in _retains)
            {
                if (kv.Value.Runtime.Equals(handle))
                {
                    removeIds.Add(kv.Key);
                }
            }

            for (int i = 0; i < removeIds.Count; i++)
            {
                _retains.Remove(removeIds[i]);
            }
        }

        private static SkillCastStage ToStage(MobaSkillRuntimeEndReason reason)
        {
            switch (reason)
            {
                case MobaSkillRuntimeEndReason.Cancelled:
                case MobaSkillRuntimeEndReason.OwnerRemoved:
                case MobaSkillRuntimeEndReason.RollbackCleanup:
                    return SkillCastStage.Cancelled;
                case MobaSkillRuntimeEndReason.Failed:
                    return SkillCastStage.Failed;
                default:
                    return SkillCastStage.Completed;
            }
        }

        private static TraceLifecycleReason ToTraceReason(MobaSkillRuntimeEndReason reason)
        {
            switch (reason)
            {
                case MobaSkillRuntimeEndReason.Cancelled:
                    return TraceLifecycleReason.Cancelled;
                case MobaSkillRuntimeEndReason.Failed:
                    return TraceLifecycleReason.Failed;
                case MobaSkillRuntimeEndReason.OwnerRemoved:
                    return TraceLifecycleReason.Dead;
                case MobaSkillRuntimeEndReason.RollbackCleanup:
                    return TraceLifecycleReason.Cancelled;
                default:
                    return TraceLifecycleReason.Completed;
            }
        }
    }
}
