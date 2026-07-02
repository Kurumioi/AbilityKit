using System;
using AbilityKit.Context;
using AbilityKit.Demo.Moba.Components;

namespace AbilityKit.Demo.Moba.Services
{
    public enum MobaRuntimeContextLifecycleState
    {
        None = 0,
        Active = 1,
        Refreshed = 2,
        Interval = 3,
        Ended = 4,
        Destroyed = 5
    }

    public readonly struct MobaBuffRuntimeContextData
    {
        public MobaBuffRuntimeContextData(
            int buffId,
            int sourceActorId,
            int targetActorId,
            long traceContextId,
            long rootTraceContextId,
            long ownerTraceContextId,
            int stackCount,
            float remainingSeconds,
            float intervalRemainingSeconds,
            MobaRuntimeContextLifecycleState lifecycleState,
            int frame,
            in MobaSkillCastRuntimeHandle skillRuntimeHandle)
        {
            BuffId = buffId;
            SourceActorId = sourceActorId;
            TargetActorId = targetActorId;
            TraceContextId = traceContextId;
            RootTraceContextId = rootTraceContextId;
            OwnerTraceContextId = ownerTraceContextId;
            StackCount = stackCount;
            RemainingSeconds = remainingSeconds;
            IntervalRemainingSeconds = intervalRemainingSeconds;
            LifecycleState = lifecycleState;
            Frame = frame;
            SkillRuntimeHandle = skillRuntimeHandle;
        }

        public int BuffId { get; }
        public int SourceActorId { get; }
        public int TargetActorId { get; }
        public long TraceContextId { get; }
        public long RootTraceContextId { get; }
        public long OwnerTraceContextId { get; }
        public int StackCount { get; }
        public float RemainingSeconds { get; }
        public float IntervalRemainingSeconds { get; }
        public MobaRuntimeContextLifecycleState LifecycleState { get; }
        public int Frame { get; }
        public MobaSkillCastRuntimeHandle SkillRuntimeHandle { get; }
    }

    public sealed class MobaBuffContextProperty : IProperty, IContextValueProvider
    {
        public MobaBuffContextProperty(in MobaBuffRuntimeContextData data)
        {
            Update(data);
        }

        public int TypeId => PropertyTypeRegistry.Instance.Register<MobaBuffContextProperty>().Id;
        public long ContextId { get; private set; }
        public long Version { get; private set; }
        public int BuffId { get; private set; }
        public int SourceActorId { get; private set; }
        public int TargetActorId { get; private set; }
        public long TraceContextId { get; private set; }
        public long RootTraceContextId { get; private set; }
        public long OwnerTraceContextId { get; private set; }
        public int StackCount { get; private set; }
        public float RemainingSeconds { get; private set; }
        public float IntervalRemainingSeconds { get; private set; }
        public MobaRuntimeContextLifecycleState LifecycleState { get; private set; }
        public int Frame { get; private set; }
        public MobaSkillCastRuntimeHandle SkillRuntimeHandle { get; private set; }

        public void SetContextId(long contextId)
        {
            ContextId = contextId;
        }

        public void Update(in MobaBuffRuntimeContextData data)
        {
            BuffId = data.BuffId;
            SourceActorId = data.SourceActorId;
            TargetActorId = data.TargetActorId;
            TraceContextId = data.TraceContextId;
            RootTraceContextId = data.RootTraceContextId;
            OwnerTraceContextId = data.OwnerTraceContextId;
            StackCount = data.StackCount;
            RemainingSeconds = data.RemainingSeconds;
            IntervalRemainingSeconds = data.IntervalRemainingSeconds;
            LifecycleState = data.LifecycleState;
            Frame = data.Frame;
            SkillRuntimeHandle = data.SkillRuntimeHandle;
            Version++;
        }

        public void Mark(MobaRuntimeContextLifecycleState state, int frame)
        {
            LifecycleState = state;
            Frame = frame;
            Version++;
        }

        public bool TryGetValue<T>(string key, out T value)
        {
            object raw = null;
            switch (key)
            {
                case MobaRuntimeContextKeys.ContextId:
                    raw = ContextId;
                    break;
                case MobaRuntimeContextKeys.Version:
                    raw = Version;
                    break;
                case MobaRuntimeContextKeys.BuffId:
                    raw = BuffId;
                    break;
                case MobaRuntimeContextKeys.SourceActorId:
                    raw = SourceActorId;
                    break;
                case MobaRuntimeContextKeys.TargetActorId:
                    raw = TargetActorId;
                    break;
                case MobaRuntimeContextKeys.TraceContextId:
                    raw = TraceContextId;
                    break;
                case MobaRuntimeContextKeys.RootTraceContextId:
                    raw = RootTraceContextId;
                    break;
                case MobaRuntimeContextKeys.OwnerTraceContextId:
                    raw = OwnerTraceContextId;
                    break;
                case MobaRuntimeContextKeys.StackCount:
                    raw = StackCount;
                    break;
                case MobaRuntimeContextKeys.RemainingSeconds:
                    raw = RemainingSeconds;
                    break;
                case MobaRuntimeContextKeys.IntervalRemainingSeconds:
                    raw = IntervalRemainingSeconds;
                    break;
                case MobaRuntimeContextKeys.LifecycleState:
                    raw = LifecycleState;
                    break;
                case MobaRuntimeContextKeys.Frame:
                    raw = Frame;
                    break;
                case MobaRuntimeContextKeys.SkillRuntimeHandle:
                    raw = SkillRuntimeHandle;
                    break;
            }

            if (raw is T typed)
            {
                value = typed;
                return true;
            }

            value = default;
            return false;
        }

        public static MobaBuffContextProperty FromRuntime(BuffRuntime runtime, int targetActorId, int frame, MobaRuntimeContextLifecycleState state)
        {
            var origin = runtime != null ? runtime.Origin : default;
            var data = new MobaBuffRuntimeContextData(
                runtime != null ? runtime.BuffId : 0,
                runtime != null ? runtime.SourceId : 0,
                targetActorId,
                runtime != null ? runtime.SourceContextId : 0L,
                origin.EffectiveRootContextId,
                origin.OwnerContextId,
                runtime != null ? runtime.StackCount : 0,
                runtime != null ? runtime.Remaining : 0f,
                runtime != null ? runtime.IntervalRemainingSeconds : 0f,
                state,
                frame,
                runtime != null ? runtime.SkillRuntimeHandle : default);
            var property = new MobaBuffContextProperty(in data);
            if (runtime != null)
            {
                property.SetContextId(runtime.RuntimeContextId);
                runtime.RuntimeContextVersion = property.Version;
            }

            return property;
        }
    }

    public sealed class MobaBuffContextSnapshot : IVersionedContextSnapshot, ISnapshotAccessor, ISourceContext, IOwnerContext, IDestroyableSnapshot, IContextValueProvider
    {
        private bool _destroyed;
        private readonly MobaBuffContextProperty _property;

        public MobaBuffContextSnapshot(MobaBuffContextProperty property)
        {
            _property = property ?? throw new ArgumentNullException(nameof(property));
            EntityId = property.ContextId;
            CreatedAtMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Version = property.Version;
            Frame = property.Frame;
        }

        public long EntityId { get; }
        public long CreatedAtMs { get; }
        public long Version { get; }
        public int Frame { get; }
        public long SourceEntityId => _property.TraceContextId;
        public long OwnerEntityId => _property.OwnerTraceContextId;
        public bool IsRealtimeAvailable => !_destroyed;
        public bool IsDestroyed => _destroyed;

        public T GetValue<T>(string key, T snapshotDefault = default)
        {
            return TryGetValue(key, out T value) ? value : snapshotDefault;
        }

        public void MarkDestroyed()
        {
            _destroyed = true;
        }

        public bool TryGetValue<T>(string key, out T value)
        {
            return _property.TryGetValue(key, out value);
        }
    }

    public static class MobaRuntimeContextKeys
    {
        public const string ContextId = "ContextId";
        public const string Version = "Version";
        public const string BuffId = "BuffId";
        public const string SourceActorId = "SourceActorId";
        public const string TargetActorId = "TargetActorId";
        public const string TraceContextId = "TraceContextId";
        public const string RootTraceContextId = "RootTraceContextId";
        public const string OwnerTraceContextId = "OwnerTraceContextId";
        public const string StackCount = "StackCount";
        public const string RemainingSeconds = "RemainingSeconds";
        public const string IntervalRemainingSeconds = "IntervalRemainingSeconds";
        public const string LifecycleState = "LifecycleState";
        public const string Frame = "Frame";
        public const string SkillRuntimeHandle = "SkillRuntimeHandle";
    }
}
