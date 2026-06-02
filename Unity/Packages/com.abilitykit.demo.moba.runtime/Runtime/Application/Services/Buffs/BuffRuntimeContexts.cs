using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Demo.Moba.Components;
using AbilityKit.GameplayTags;
using AbilityKit.Trace;

namespace AbilityKit.Demo.Moba.Services
{
    public readonly struct BuffOriginContext : IMobaOriginContextProvider
    {
        public readonly long ParentContextId;
        public readonly int OriginSourceActorId;
        public readonly int OriginTargetActorId;
        public readonly MobaTraceKind OriginKind;
        public readonly int OriginConfigId;
        public readonly long OriginContextId;
        public readonly MobaSkillCastRuntimeHandle SkillRuntimeHandle;
        public readonly MobaGameplayOrigin Origin;

        public BuffOriginContext(long parentContextId, int originSourceActorId, int originTargetActorId, MobaTraceKind originKind = MobaTraceKind.None, int originConfigId = 0, long originContextId = 0, MobaSkillCastRuntimeHandle skillRuntimeHandle = default)
        {
            ParentContextId = parentContextId;
            OriginSourceActorId = originSourceActorId;
            OriginTargetActorId = originTargetActorId;
            OriginKind = originKind;
            OriginConfigId = originConfigId;
            OriginContextId = originContextId;
            SkillRuntimeHandle = skillRuntimeHandle;
            Origin = MobaGameplayOrigin.FromLegacy(originSourceActorId, originTargetActorId, originKind, originConfigId, parentContextId != 0 ? parentContextId : originContextId, in skillRuntimeHandle);
        }

        public BuffOriginContext(in MobaGameplayOrigin origin)
        {
            Origin = origin;
            ParentContextId = origin.EffectiveParentContextId;
            OriginSourceActorId = origin.SourceActorId;
            OriginTargetActorId = origin.TargetActorId;
            OriginKind = origin.ImmediateKind;
            OriginConfigId = origin.ImmediateConfigId;
            OriginContextId = origin.ImmediateContextId;
            SkillRuntimeHandle = origin.SkillRuntimeHandle;
        }

        public static BuffOriginContext FromActors(long parentContextId, int originSourceActorId, int originTargetActorId)
        {
            return new BuffOriginContext(parentContextId, originSourceActorId, originTargetActorId);
        }

        public static BuffOriginContext FromActors(long parentContextId, int originSourceActorId, int originTargetActorId, in MobaSkillCastRuntimeHandle skillRuntimeHandle)
        {
            return new BuffOriginContext(parentContextId, originSourceActorId, originTargetActorId, skillRuntimeHandle: skillRuntimeHandle);
        }

        public static BuffOriginContext FromOrigin(in MobaGameplayOrigin origin)
        {
            return new BuffOriginContext(in origin);
        }

        public bool TryGetOrigin(out MobaGameplayOrigin origin)
        {
            origin = Origin.IsValid
                ? Origin
                : MobaGameplayOrigin.FromLegacy(OriginSourceActorId, OriginTargetActorId, OriginKind, OriginConfigId, ParentContextId != 0 ? ParentContextId : OriginContextId, in SkillRuntimeHandle);
            return origin.IsValid;
        }

        public TraceEndpoint ToOriginSourceEndpoint()
        {
            return TraceEndpoint.Actor(OriginSourceActorId);
        }

        public TraceEndpoint ToOriginTargetEndpoint()
        {
            return TraceEndpoint.Actor(OriginTargetActorId);
        }
    }

    internal sealed class BuffApplyRequest
    {
        public int TargetActorId;
        public int BuffId;
        public int SourceActorId;
        public int DurationOverrideMs;
        public BuffOriginContext Origin;
        public MobaSkillCastRuntimeHandle SkillRuntimeHandle => Origin.SkillRuntimeHandle;

        public bool IsValid => TargetActorId > 0 && BuffId > 0;
    }

    internal sealed class BuffRemoveRequest
    {
        public int TargetActorId;
        public int BuffId;
        public int SourceActorId;
        public TraceLifecycleReason Reason;

        public bool IsValid => TargetActorId > 0 && BuffId > 0;
    }

    internal sealed class BuffOperationContext
    {
        public BuffApplyRequest ApplyRequest;
        public BuffRemoveRequest RemoveRequest;
        public BuffMO Buff;
        public BuffRuntime Runtime;
        public int TargetActorId;
        public float DurationSeconds;
        public ContinuousTagRequirements Requirements;
        public bool IsExistingRuntime;

        public BuffRuntimeView RuntimeView => new BuffRuntimeView(Runtime);
    }

    internal interface IBuffReadOnlyView
    {
        bool IsValid { get; }
        int BuffId { get; }
        int SourceActorId { get; }
        int StackCount { get; }
        float RemainingSeconds { get; }
        float IntervalRemainingSeconds { get; }
        long SourceContextId { get; }
        BuffContinuousRuntime Continuous { get; }
        MobaSkillCastRuntimeHandle SkillRuntimeHandle { get; }
        MobaSkillRuntimeRetainHandle SkillRuntimeRetainHandle { get; }
    }

    internal interface IBuffLiveViewProvider
    {
        bool TryGetLiveBuffView(out BuffRuntimeView view);
    }

    internal static class BuffLiveViewResolver
    {
        public static bool TryResolve(object payload, out BuffRuntimeView view)
        {
            view = default;
            return payload is IBuffLiveViewProvider provider
                   && provider.TryGetLiveBuffView(out view)
                   && view.IsValid;
        }
    }

    internal interface IBuffMutableState : IBuffReadOnlyView
    {
        void SetSourceActorId(int sourceActorId);
        void SetStackCount(int stackCount);
        void SetRemainingSeconds(float remainingSeconds);
        void SetIntervalRemainingSeconds(float intervalRemainingSeconds);
        void BindSourceContext(long sourceContextId);
        void SetContinuous(BuffContinuousRuntime continuous);
        void SetTagRequirements(ContinuousTagRequirements requirements);
        void BindSkillRuntime(in MobaSkillCastRuntimeHandle runtimeHandle, in MobaSkillRuntimeRetainHandle retainHandle);
        void ClearSkillRuntimeBinding();
        void ClearRuntimeBindings();
    }

    internal readonly struct BuffRuntimeView : IBuffMutableState
    {
        private readonly BuffRuntime _runtime;

        public BuffRuntimeView(BuffRuntime runtime)
        {
            _runtime = runtime;
        }

        public bool IsValid => _runtime != null;
        public int BuffId => _runtime != null ? _runtime.BuffId : 0;
        public int SourceActorId => _runtime != null ? _runtime.SourceId : 0;
        public int StackCount => _runtime != null ? _runtime.StackCount : 0;
        public float RemainingSeconds => _runtime != null && _runtime.Continuous != null ? _runtime.Continuous.RemainingSeconds : _runtime != null ? _runtime.Remaining : 0f;
        public float IntervalRemainingSeconds => _runtime != null && _runtime.Continuous != null ? _runtime.Continuous.IntervalRemainingSeconds : _runtime != null ? _runtime.IntervalRemainingSeconds : 0f;
        public long SourceContextId => _runtime != null ? _runtime.SourceContextId : 0L;
        public BuffContinuousRuntime Continuous => _runtime != null ? _runtime.Continuous : null;
        public MobaSkillCastRuntimeHandle SkillRuntimeHandle => _runtime != null ? _runtime.SkillRuntimeHandle : default;
        public MobaSkillRuntimeRetainHandle SkillRuntimeRetainHandle => _runtime != null ? _runtime.SkillRuntimeRetainHandle : default;

        public void SetSourceActorId(int sourceActorId)
        {
            if (_runtime == null) return;
            _runtime.SourceId = sourceActorId;
        }

        public void SetStackCount(int stackCount)
        {
            if (_runtime == null) return;
            _runtime.StackCount = stackCount;
        }

        public void SetRemainingSeconds(float remainingSeconds)
        {
            if (_runtime == null) return;
            _runtime.Remaining = remainingSeconds;
        }

        public void SetIntervalRemainingSeconds(float intervalRemainingSeconds)
        {
            if (_runtime == null) return;

            _runtime.IntervalRemainingSeconds = intervalRemainingSeconds;
            if (_runtime.Continuous != null)
            {
                _runtime.Continuous.IntervalRemainingSeconds = intervalRemainingSeconds;
            }
        }

        public void BindSourceContext(long sourceContextId)
        {
            if (_runtime == null) return;
            _runtime.SourceContextId = sourceContextId;
        }

        public void SetContinuous(BuffContinuousRuntime continuous)
        {
            if (_runtime == null) return;
            _runtime.Continuous = continuous;
        }

        public void SetTagRequirements(ContinuousTagRequirements requirements)
        {
            if (_runtime == null) return;
            _runtime.TagRequirements = requirements;
        }

        public void BindSkillRuntime(in MobaSkillCastRuntimeHandle runtimeHandle, in MobaSkillRuntimeRetainHandle retainHandle)
        {
            if (_runtime == null) return;
            _runtime.SkillRuntimeHandle = runtimeHandle;
            _runtime.SkillRuntimeRetainHandle = retainHandle;
        }

        public void ClearSkillRuntimeBinding()
        {
            if (_runtime == null) return;
            _runtime.SkillRuntimeHandle = default;
            _runtime.SkillRuntimeRetainHandle = default;
        }

        public void ClearRuntimeBindings()
        {
            if (_runtime == null) return;
            _runtime.SourceContextId = 0;
            _runtime.Continuous = null;
            _runtime.TagRequirements = null;
            ClearSkillRuntimeBinding();
        }
    }
}
