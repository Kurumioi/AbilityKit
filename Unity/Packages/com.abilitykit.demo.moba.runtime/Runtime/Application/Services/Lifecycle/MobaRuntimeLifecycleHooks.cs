using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AbilityKit.Demo.Moba.Components;
using AbilityKit.Trace;

namespace AbilityKit.Demo.Moba.Services
{
    public enum MobaRuntimeLifecycleEventKind
    {
        Activated,
        Ended,
        Cleared,
        Failed
    }

    public readonly struct MobaRuntimeLifecycleEvent
    {
        public MobaRuntimeLifecycleEvent(MobaRuntimeLifecycleEventKind kind, object runtime, in MobaContextSourceView source, string reason)
        {
            Kind = kind;
            Runtime = runtime;
            Source = source;
            Reason = reason;
        }

        public MobaRuntimeLifecycleEventKind Kind { get; }
        public object Runtime { get; }
        public MobaContextSourceView Source { get; }
        public string Reason { get; }
    }

    public interface IMobaRuntimeLifecycleHook
    {
        void OnRuntimeLifecycle(in MobaRuntimeLifecycleEvent lifecycleEvent);
    }

    public sealed class MobaRuntimeLifecycleHookService
    {
        private readonly List<IMobaRuntimeLifecycleHook> _hooks = new List<IMobaRuntimeLifecycleHook>(4);

        public void Register(IMobaRuntimeLifecycleHook hook)
        {
            if (hook == null || _hooks.Contains(hook)) return;
            _hooks.Add(hook);
        }

        public void Notify(in MobaRuntimeLifecycleEvent lifecycleEvent)
        {
            for (var i = 0; i < _hooks.Count; i++)
            {
                _hooks[i]?.OnRuntimeLifecycle(in lifecycleEvent);
            }
        }
    }

    public sealed class MobaSkillRuntimeLifecycleBridgeHook : IMobaSkillRuntimeLifecycleHook
    {
        private readonly MobaRuntimeLifecycleHookService _runtimeHooks;

        public MobaSkillRuntimeLifecycleBridgeHook(MobaRuntimeLifecycleHookService runtimeHooks)
        {
            _runtimeHooks = runtimeHooks;
        }

        public void OnSkillRuntimeLifecycle(in MobaSkillRuntimeLifecycleEvent lifecycleEvent)
        {
            if (_runtimeHooks == null || lifecycleEvent.Runtime == null) return;
            if (!TryMapKind(lifecycleEvent.Kind, lifecycleEvent.Forced, out var kind)) return;

            lifecycleEvent.Runtime.TryGetContextSource(out var source);
            var reason = lifecycleEvent.Reason.ToString();
            var runtimeEvent = new MobaRuntimeLifecycleEvent(kind, lifecycleEvent.Runtime, in source, reason);
            _runtimeHooks.Notify(in runtimeEvent);
        }

        private static bool TryMapKind(MobaSkillRuntimeLifecycleEventKind skillKind, bool forced, out MobaRuntimeLifecycleEventKind kind)
        {
            switch (skillKind)
            {
                case MobaSkillRuntimeLifecycleEventKind.Created:
                    kind = MobaRuntimeLifecycleEventKind.Activated;
                    return true;
                case MobaSkillRuntimeLifecycleEventKind.Finalized:
                    kind = forced ? MobaRuntimeLifecycleEventKind.Failed : MobaRuntimeLifecycleEventKind.Ended;
                    return true;
                case MobaSkillRuntimeLifecycleEventKind.Cleared:
                    kind = MobaRuntimeLifecycleEventKind.Cleared;
                    return true;
                default:
                    kind = default;
                    return false;
            }
        }
    }

    public interface IMobaRuntimeTraceRetention
    {
        bool IsRetained(object runtime);
        void Retain(object runtime, in MobaContextSourceView source, string reason);
        void Release(object runtime);
    }

    public sealed class MobaRuntimeTraceRetentionService : IMobaRuntimeTraceRetention
    {
        private sealed class RetentionSlot
        {
            public MobaTraceRetentionHandle Handle;
        }

        private readonly MobaTraceRegistry _trace;
        private readonly ConditionalWeakTable<object, RetentionSlot> _retentions = new ConditionalWeakTable<object, RetentionSlot>();

        public MobaRuntimeTraceRetentionService(MobaTraceRegistry trace)
        {
            _trace = trace;
        }

        public bool IsRetained(object runtime)
        {
            return runtime != null
                   && _retentions.TryGetValue(runtime, out var slot)
                   && slot.Handle != null
                   && slot.Handle.IsValid;
        }

        public void Retain(object runtime, in MobaContextSourceView source, string reason)
        {
            if (_trace == null || runtime == null) return;
            var slot = _retentions.GetOrCreateValue(runtime);
            if (slot.Handle != null && slot.Handle.IsValid) return;

            if (_trace.TryRetainContextSource(in source, reason, out var handle)
                || _trace.TryRetainPayloadSource(runtime, reason, out handle))
            {
                slot.Handle = handle;
            }
        }

        public void Release(object runtime)
        {
            if (runtime == null) return;
            if (!_retentions.TryGetValue(runtime, out var slot)) return;

            slot.Handle?.Dispose();
            slot.Handle = null;
            _retentions.Remove(runtime);
        }
    }

    public sealed class MobaTraceRetentionLifecycleHook : IMobaRuntimeLifecycleHook
    {
        private readonly IMobaRuntimeTraceRetention _retention;

        public MobaTraceRetentionLifecycleHook(MobaTraceRegistry trace)
            : this(trace != null ? new MobaRuntimeTraceRetentionService(trace) : null)
        {
        }

        public MobaTraceRetentionLifecycleHook(IMobaRuntimeTraceRetention retention)
        {
            _retention = retention;
        }

        public void OnRuntimeLifecycle(in MobaRuntimeLifecycleEvent lifecycleEvent)
        {
            if (lifecycleEvent.Runtime == null || _retention == null) return;

            switch (lifecycleEvent.Kind)
            {
                case MobaRuntimeLifecycleEventKind.Activated:
                    var source = lifecycleEvent.Source;
                    _retention.Retain(lifecycleEvent.Runtime, in source, lifecycleEvent.Reason);
                    break;
                case MobaRuntimeLifecycleEventKind.Ended:
                case MobaRuntimeLifecycleEventKind.Cleared:
                case MobaRuntimeLifecycleEventKind.Failed:
                    _retention.Release(lifecycleEvent.Runtime);
                    break;
            }
        }

        public bool IsRetained(object runtime)
        {
            return _retention != null && _retention.IsRetained(runtime);
        }
    }

    public static class MobaRuntimeLifecycleHookFactory
    {
        public static MobaSkillRuntimeLifecycleBridgeHook CreateSkillRuntimeBridge(MobaRuntimeLifecycleHookService runtimeHooks)
        {
            return runtimeHooks != null ? new MobaSkillRuntimeLifecycleBridgeHook(runtimeHooks) : null;
        }

        public static MobaRuntimeLifecycleHookService CreateDefault(MobaTraceRegistry trace)
        {
            return CreateDefault(trace != null ? new MobaRuntimeTraceRetentionService(trace) : null);
        }

        public static MobaRuntimeLifecycleHookService CreateDefault(IMobaRuntimeTraceRetention traceRetention)
        {
            var hooks = new MobaRuntimeLifecycleHookService();
            if (traceRetention != null)
            {
                hooks.Register(new MobaTraceRetentionLifecycleHook(traceRetention));
            }
 
            return hooks;
        }
    }
}
