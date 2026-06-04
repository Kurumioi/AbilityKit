using System;
using System.Collections.Generic;
using AbilityKit.Core.Continuous;
using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Demo.Moba.Components;
using AbilityKit.GameplayTags;

namespace AbilityKit.Demo.Moba.Services
{
    public sealed class BuffContinuousRuntime : IMobaTickableContinuous, IMobaContinuousIntervalState, IMobaContinuousRuntimeStateSync, IMobaContinuousRuntimeDebugSource, IMobaContextSourceProvider
    {
        private readonly BuffContinuousConfig _config;

        public BuffContinuousRuntime(BuffMO buff, int sourceActorId, int targetActorId, float durationSeconds, ContinuousTagRequirements tagRequirements)
        {
            if (buff == null) throw new ArgumentNullException(nameof(buff));

            BuffId = buff.Id;
            SourceActorId = sourceActorId;
            TargetActorId = targetActorId;
            _config = new BuffContinuousConfig(this, durationSeconds, tagRequirements, buff);
        }

        public int BuffId { get; }
        public int SourceActorId { get; private set; }
        public int TargetActorId { get; }
        public long SourceContextId { get; private set; }
        public int ModifierSourceId { get; private set; }
        public BuffRuntime Runtime { get; private set; }

        public ContinuousTagRequirements TagRequirements => _config.TagRequirements;
        public IContinuousConfig Config => _config;
        public ContinuousState State { get; private set; } = ContinuousState.Inactive;
        public bool IsActive => State == ContinuousState.Active;
        public bool IsTerminated => State == ContinuousState.Expired || State == ContinuousState.Aborted;
        public bool IsPaused => State == ContinuousState.Paused;
        public float ElapsedSeconds { get; private set; }
        public float IntervalRemainingSeconds { get; set; }
        public float RemainingSeconds
        {
            get
            {
                var duration = _config.DurationSeconds;
                if (!duration.HasValue) return float.PositiveInfinity;
                var remaining = duration.Value - ElapsedSeconds;
                return remaining > 0f ? remaining : 0f;
            }
        }

        public event Action<IContinuous, ContinuousEndReason> OnEnded;

        public void BindRuntime(BuffRuntime runtime)
        {
            Runtime = runtime;
        }

        public void BindSourceContext(long sourceContextId)
        {
            SourceContextId = sourceContextId;
            ModifierSourceId = CreateModifierSourceId(sourceContextId, BuffId, TargetActorId);
        }

        public void Refresh(int sourceActorId, float remainingSeconds, int stackCount, int maxStack, ContinuousTagRequirements tagRequirements)
        {
            SourceActorId = sourceActorId;
            _config.DurationSeconds = remainingSeconds > 0f ? remainingSeconds : (float?)null;
            _config.Stack = stackCount;
            _config.MaxStack = maxStack > 0 ? maxStack : int.MaxValue;
            _config.TagRequirements = tagRequirements;
            ElapsedSeconds = 0f;
        }

        public void TickManaged(float deltaTimeSeconds)
        {
            if (!IsActive || deltaTimeSeconds <= 0f) return;

            ElapsedSeconds += deltaTimeSeconds;
            var duration = _config.DurationSeconds;
            if (duration.HasValue && ElapsedSeconds >= duration.Value)
            {
                End(ContinuousEndReason.Completed);
            }
        }

        public void SyncManagedState()
        {
            if (Runtime == null) return;

            Runtime.IntervalRemainingSeconds = IntervalRemainingSeconds;
            Runtime.Remaining = RemainingSeconds;
        }

        public void Activate()
        {
            if (State == ContinuousState.Active) return;
            if (IsTerminated) return;

            State = ContinuousState.Activating;
            State = ContinuousState.Active;
        }

        public void Pause()
        {
            if (State != ContinuousState.Active) return;
            State = ContinuousState.Paused;
        }

        public void Resume()
        {
            if (State != ContinuousState.Paused) return;
            State = ContinuousState.Active;
        }

        public void End(ContinuousEndReason reason)
        {
            if (IsTerminated) return;

            State = reason == ContinuousEndReason.Completed ? ContinuousState.Expired : ContinuousState.Aborted;
            OnEnded?.Invoke(this, reason);
        }

        public void Abort(string reason)
        {
            End(ContinuousEndReason.Interrupted);
        }

        public bool TryGetRuntimeDebugInfo(out MobaContinuousRuntimeDebugInfo info)
        {
            TryGetContextSource(out var source);
            var handle = Runtime != null ? Runtime.SkillRuntimeHandle : default;
            var sourceContextId = source.SourceContextId != 0 ? source.SourceContextId : SourceContextId;
            info = new MobaContinuousRuntimeDebugInfo(
                "Buff",
                BuffId,
                SourceActorId,
                TargetActorId,
                sourceContextId,
                source.ParentContextId,
                source.RootContextId,
                source.OwnerContextId,
                handle,
                source);
            return BuffId > 0 || SourceActorId > 0 || TargetActorId > 0 || sourceContextId != 0 || handle.IsValid;
        }

        public bool TryGetContextSource(out MobaContextSourceView source)
        {
            if (Runtime != null && Runtime.ContextSource.IsValid)
            {
                source = new MobaContextSourceView(
                    MobaContextSourceResolveKind.DirectProvider,
                    MobaContextSourceBoundary.LiveRuntime,
                    Runtime.ContextSource.ContextKind != EffectContextKind.Unknown ? Runtime.ContextSource.ContextKind : EffectContextKind.Buff,
                    Runtime.ContextSource.TraceKind,
                    Runtime.ContextSource.SourceActorId != 0 ? Runtime.ContextSource.SourceActorId : SourceActorId,
                    Runtime.ContextSource.TargetActorId != 0 ? Runtime.ContextSource.TargetActorId : TargetActorId,
                    Runtime.ContextSource.SourceContextId != 0 ? Runtime.ContextSource.SourceContextId : SourceContextId,
                    Runtime.ContextSource.ParentContextId,
                    Runtime.ContextSource.RootContextId,
                    Runtime.ContextSource.OwnerContextId,
                    Runtime.ContextSource.ConfigId != 0 ? Runtime.ContextSource.ConfigId : BuffId,
                    Runtime.ContextSource.TriggerId,
                    Runtime.ContextSource.Frame,
                    "Buff",
                    BuffId,
                    true,
                    Runtime.ContextSource.SkillRuntimeHandle.IsValid ? Runtime.ContextSource.SkillRuntimeHandle : Runtime.SkillRuntimeHandle);
                return source.IsValid;
            }

            var handle = Runtime != null ? Runtime.SkillRuntimeHandle : default;
            var origin = Runtime != null && Runtime.Origin.IsValid
                ? Runtime.Origin
                : MobaGameplayOrigin.FromLegacy(SourceActorId, TargetActorId, MobaTraceKind.BuffApply, BuffId, SourceContextId, in handle);
            var lineageContext = origin.ToLineageContext(EffectContextKind.Buff);
            source = MobaContextSourceView.FromLineage(
                in lineageContext,
                MobaContextSourceResolveKind.DirectProvider,
                MobaContextSourceBoundary.LiveRuntime,
                handle.IsValid ? handle : origin.SkillRuntimeHandle,
                Runtime != null,
                "Buff",
                BuffId);
            return source.IsValid;
        }

        private static int CreateModifierSourceId(long sourceContextId, int buffId, int targetActorId)
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + sourceContextId.GetHashCode();
                hash = hash * 31 + buffId;
                hash = hash * 31 + targetActorId;
                return hash == 0 ? buffId : hash;
            }
        }

        private sealed class BuffContinuousConfig : MobaContinuousConfigBase
        {
            private readonly BuffContinuousRuntime _runtime;

            private readonly BuffMO _buff;

            public BuffContinuousConfig(BuffContinuousRuntime runtime, float durationSeconds, ContinuousTagRequirements tagRequirements, BuffMO buff)
                : base(durationSeconds, tagRequirements, buff?.Modifiers)
            {
                _runtime = runtime;
                _buff = buff;
            }

            public override string Id => $"buff:{_runtime.TargetActorId}:{_runtime.BuffId}:{_runtime.SourceContextId}";
            public override long OwnerId => _runtime.TargetActorId;
            public override int OwnerActorId => _runtime.TargetActorId;
            public override int ModifierSourceId => _runtime.ModifierSourceId;
            public override GameplayTagSource TagSource => CreateSource(_runtime);
            public override float IntervalSeconds => _buff != null && _buff.IntervalMs > 0 ? _buff.IntervalMs / 1000f : 0f;
            public override IReadOnlyList<int> IntervalEffectIds => _buff?.OnIntervalEffects ?? Array.Empty<int>();

            private static GameplayTagSource CreateSource(BuffContinuousRuntime runtime)
            {
                if (runtime == null) return GameplayTagSource.System;
                if (runtime.SourceContextId != 0) return new GameplayTagSource(runtime.SourceContextId);
                if (runtime.SourceActorId != 0) return new GameplayTagSource(runtime.SourceActorId);
                return GameplayTagSource.System;
            }
        }
    }

    public sealed class BuffModifierBinding
    {
        public int AttributeType;
        public int Handle;
        public int SourceId;
    }
}
