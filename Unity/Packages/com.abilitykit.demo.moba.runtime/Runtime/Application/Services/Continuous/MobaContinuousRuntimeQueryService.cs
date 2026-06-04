using System;
using System.Collections.Generic;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Core.Continuous;
using AbilityKit.GameplayTags;
using AbilityKit.Modifiers;

namespace AbilityKit.Demo.Moba.Services
{
    public interface IMobaContinuousRuntimeQueryService
    {
        IReadOnlyList<MobaContinuousRuntimeView> GetOwnerContinuous(int ownerActorId, bool includeTerminated = false);
        IReadOnlyList<MobaContinuousRuntimeView> GetAllContinuous(bool includeTerminated = false);
        bool TryGetRuntimeView(IContinuous continuous, out MobaContinuousRuntimeView view);
        MobaContinuousLifecycleReason GetLifecycleReason(IContinuous continuous);
        MobaContinuousTagRuleResult GetTagRuleResult(IContinuous continuous);
    }

    public interface IMobaContinuousRuntimeDebugSource
    {
        bool TryGetRuntimeDebugInfo(out MobaContinuousRuntimeDebugInfo info);
    }

    public readonly struct MobaContinuousRuntimeDebugInfo
    {
        public MobaContinuousRuntimeDebugInfo(
            string kind,
            int configId,
            int sourceActorId,
            int targetActorId,
            long sourceContextId,
            long parentContextId,
            long rootContextId,
            long ownerContextId,
            MobaSkillCastRuntimeHandle skillRuntimeHandle,
            MobaContextSourceView contextSource = default)
        {
            Kind = kind;
            ConfigId = configId;
            SourceActorId = sourceActorId;
            TargetActorId = targetActorId;
            SourceContextId = sourceContextId;
            ParentContextId = parentContextId;
            RootContextId = rootContextId;
            OwnerContextId = ownerContextId;
            SkillRuntimeHandle = skillRuntimeHandle;
            ContextSource = contextSource;
        }

        public string Kind { get; }
        public int ConfigId { get; }
        public int SourceActorId { get; }
        public int TargetActorId { get; }
        public long SourceContextId { get; }
        public long ParentContextId { get; }
        public long RootContextId { get; }
        public long OwnerContextId { get; }
        public MobaSkillCastRuntimeHandle SkillRuntimeHandle { get; }
        public MobaContextSourceView ContextSource { get; }
        public bool HasContextSource => ContextSource.IsValid;
    }

    public readonly struct MobaContinuousLifecycleReason
    {
        public MobaContinuousLifecycleReason(string lastEvent, string reason, ContinuousEndReason endReason)
        {
            LastEvent = lastEvent;
            Reason = reason;
            EndReason = endReason;
        }

        public string LastEvent { get; }
        public string Reason { get; }
        public ContinuousEndReason EndReason { get; }
        public bool HasReason => !string.IsNullOrEmpty(LastEvent) || !string.IsNullOrEmpty(Reason);

        public static MobaContinuousLifecycleReason None => default;
    }

    public sealed class MobaContinuousRuntimeView
    {
        public MobaContinuousRuntimeView(
            IContinuous continuous,
            string id,
            string kind,
            int configId,
            long ownerId,
            int ownerActorId,
            int sourceActorId,
            int targetActorId,
            long sourceContextId,
            long parentContextId,
            long rootContextId,
            long ownerContextId,
            MobaSkillCastRuntimeHandle skillRuntimeHandle,
            ContinuousState state,
            bool isActive,
            bool isPaused,
            bool isTerminated,
            float elapsedSeconds,
            float durationSeconds,
            float remainingSeconds,
            int stack,
            int maxStack,
            float intervalSeconds,
            float intervalRemainingSeconds,
            IReadOnlyList<int> intervalEffectIds,
            IReadOnlyList<MobaContinuousRuntimeTagView> grantedTags,
            IReadOnlyList<MobaContinuousRuntimeModifierView> modifiers,
            IReadOnlyList<MobaContinuousModifierExplainResult> modifierExplanations,
            MobaContinuousLifecycleReason lifecycleReason,
            MobaContinuousTagRuleResult tagRuleResult,
            MobaContextSourceView contextSource)
        {
            Continuous = continuous;
            Id = id;
            Kind = kind;
            ConfigId = configId;
            OwnerId = ownerId;
            OwnerActorId = ownerActorId;
            SourceActorId = sourceActorId;
            TargetActorId = targetActorId;
            SourceContextId = sourceContextId;
            ParentContextId = parentContextId;
            RootContextId = rootContextId;
            OwnerContextId = ownerContextId;
            SkillRuntimeHandle = skillRuntimeHandle;
            State = state;
            IsActive = isActive;
            IsPaused = isPaused;
            IsTerminated = isTerminated;
            ElapsedSeconds = elapsedSeconds;
            DurationSeconds = durationSeconds;
            RemainingSeconds = remainingSeconds;
            Stack = stack;
            MaxStack = maxStack;
            IntervalSeconds = intervalSeconds;
            IntervalRemainingSeconds = intervalRemainingSeconds;
            IntervalEffectIds = intervalEffectIds ?? Array.Empty<int>();
            GrantedTags = grantedTags ?? Array.Empty<MobaContinuousRuntimeTagView>();
            Modifiers = modifiers ?? Array.Empty<MobaContinuousRuntimeModifierView>();
            ModifierExplanations = modifierExplanations ?? Array.Empty<MobaContinuousModifierExplainResult>();
            LifecycleReason = lifecycleReason;
            TagRuleResult = tagRuleResult;
            ContextSource = contextSource;
        }

        public IContinuous Continuous { get; }
        public string Id { get; }
        public string Kind { get; }
        public int ConfigId { get; }
        public long OwnerId { get; }
        public int OwnerActorId { get; }
        public int SourceActorId { get; }
        public int TargetActorId { get; }
        public long SourceContextId { get; }
        public long ParentContextId { get; }
        public long RootContextId { get; }
        public long OwnerContextId { get; }
        public MobaSkillCastRuntimeHandle SkillRuntimeHandle { get; }
        public ContinuousState State { get; }
        public bool IsActive { get; }
        public bool IsPaused { get; }
        public bool IsTerminated { get; }
        public float ElapsedSeconds { get; }
        public float DurationSeconds { get; }
        public float RemainingSeconds { get; }
        public int Stack { get; }
        public int MaxStack { get; }
        public float IntervalSeconds { get; }
        public float IntervalRemainingSeconds { get; }
        public IReadOnlyList<int> IntervalEffectIds { get; }
        public IReadOnlyList<MobaContinuousRuntimeTagView> GrantedTags { get; }
        public IReadOnlyList<MobaContinuousRuntimeModifierView> Modifiers { get; }
        public IReadOnlyList<MobaContinuousModifierExplainResult> ModifierExplanations { get; }
        public MobaContinuousLifecycleReason LifecycleReason { get; }
        public MobaContinuousTagRuleResult TagRuleResult { get; }
        public MobaContextSourceView ContextSource { get; }
        public MobaContextSourceResolveKind ContextSourceResolveKind => ContextSource.ResolveKind;
        public MobaContextSourceBoundary ContextSourceBoundary => ContextSource.Boundary;
        public bool HasLiveRuntimeSource => ContextSource.HasLiveRuntime;
    }

    public readonly struct MobaContinuousRuntimeTagView
    {
        public MobaContinuousRuntimeTagView(int tagId, string tagName, GameplayTagSource source)
        {
            TagId = tagId;
            TagName = tagName;
            Source = source;
        }

        public int TagId { get; }
        public string TagName { get; }
        public GameplayTagSource Source { get; }
    }

    public readonly struct MobaContinuousRuntimeModifierView
    {
        public MobaContinuousRuntimeModifierView(
            int targetKind,
            int targetId,
            int op,
            float value,
            MagnitudeSourceType magnitudeType,
            float magnitudeBaseValue,
            float magnitudeCoefficient,
            int evaluationPolicy,
            int priority,
            int stack,
            int modifierSourceId)
        {
            TargetKind = targetKind;
            TargetId = targetId;
            Op = op;
            Value = value;
            MagnitudeType = magnitudeType;
            MagnitudeBaseValue = magnitudeBaseValue;
            MagnitudeCoefficient = magnitudeCoefficient;
            EvaluationPolicy = evaluationPolicy;
            Priority = priority;
            Stack = stack;
            ModifierSourceId = modifierSourceId;
        }

        public int TargetKind { get; }
        public int TargetId { get; }
        public int Op { get; }
        public float Value { get; }
        public MagnitudeSourceType MagnitudeType { get; }
        public float MagnitudeBaseValue { get; }
        public float MagnitudeCoefficient { get; }
        public int EvaluationPolicy { get; }
        public int Priority { get; }
        public int Stack { get; }
        public int ModifierSourceId { get; }
    }

    [WorldService(typeof(IMobaContinuousRuntimeQueryService))]
    [WorldService(typeof(MobaContinuousRuntimeQueryService))]
    public sealed class MobaContinuousRuntimeQueryService : IMobaContinuousRuntimeQueryService, IContinuousLifecycleBinder, IWorldInitializable, IService
    {
        private readonly Dictionary<IContinuous, MobaContinuousLifecycleReason> _reasons = new Dictionary<IContinuous, MobaContinuousLifecycleReason>();
        private IContinuousManager _continuous;
        private DefaultContinuousManager _continuousEvents;
        private IMobaContinuousTagRuleService _tagRules;
        private IMobaContinuousModifierQueryService _modifiers;

        public void OnInit(IWorldResolver services)
        {
            services?.TryResolve(out _continuous);
            services?.TryResolve(out _tagRules);
            services?.TryResolve(out _modifiers);
            _continuousEvents = _continuous as DefaultContinuousManager;
            _continuousEvents?.AddLifecycleBinder(this);
        }

        public void Dispose()
        {
            _continuousEvents?.RemoveLifecycleBinder(this);
            _reasons.Clear();
            _continuous = null;
            _continuousEvents = null;
            _tagRules = null;
            _modifiers = null;
        }

        public IReadOnlyList<MobaContinuousRuntimeView> GetOwnerContinuous(int ownerActorId, bool includeTerminated = false)
        {
            if (ownerActorId <= 0 || _continuous == null) return Array.Empty<MobaContinuousRuntimeView>();

            var continuousList = _continuous.GetOwnerContinuous(ownerActorId);
            return BuildViews(continuousList, includeTerminated);
        }

        public IReadOnlyList<MobaContinuousRuntimeView> GetAllContinuous(bool includeTerminated = false)
        {
            if (_continuousEvents == null) return Array.Empty<MobaContinuousRuntimeView>();
            return BuildViews(_continuousEvents.GetAllContinuous(), includeTerminated);
        }

        public bool TryGetRuntimeView(IContinuous continuous, out MobaContinuousRuntimeView view)
        {
            view = null;
            if (continuous == null) return false;

            view = CreateView(continuous);
            return view != null;
        }

        public MobaContinuousLifecycleReason GetLifecycleReason(IContinuous continuous)
        {
            if (continuous == null) return MobaContinuousLifecycleReason.None;
            return _reasons.TryGetValue(continuous, out var reason) ? reason : MobaContinuousLifecycleReason.None;
        }

        public MobaContinuousTagRuleResult GetTagRuleResult(IContinuous continuous)
        {
            if (continuous == null || _tagRules == null) return MobaContinuousTagRuleResult.None;

            var result = _tagRules.GetLastResult(continuous);
            return result.HasResult ? result : _tagRules.Explain(continuous);
        }

        public void OnRegistered(IContinuous continuous, IContinuousManager manager)
        {
            SetReason(continuous, "Registered", null, default);
        }

        public void OnActivated(IContinuous continuous, IContinuousManager manager)
        {
            SetReason(continuous, "Activated", null, default);
        }

        public void OnPaused(IContinuous continuous, IContinuousManager manager)
        {
            SetReason(continuous, "Paused", "Paused by lifecycle or tag rule", default);
        }

        public void OnResumed(IContinuous continuous, IContinuousManager manager)
        {
            SetReason(continuous, "Resumed", "Resumed by lifecycle or tag rule", default);
        }

        public void OnEnded(IContinuous continuous, ContinuousEndReason reason, IContinuousManager manager)
        {
            SetReason(continuous, "Ended", reason.ToString(), reason);
        }

        public void OnUnregistered(IContinuous continuous, ContinuousEndReason reason, IContinuousManager manager)
        {
            SetReason(continuous, "Unregistered", reason.ToString(), reason);
        }

        private IReadOnlyList<MobaContinuousRuntimeView> BuildViews(IReadOnlyList<IContinuous> continuousList, bool includeTerminated)
        {
            if (continuousList == null || continuousList.Count == 0) return Array.Empty<MobaContinuousRuntimeView>();

            List<MobaContinuousRuntimeView> result = null;
            for (int i = 0; i < continuousList.Count; i++)
            {
                var continuous = continuousList[i];
                if (continuous == null) continue;
                if (!includeTerminated && continuous.IsTerminated) continue;

                var view = CreateView(continuous);
                if (view == null) continue;

                result ??= new List<MobaContinuousRuntimeView>();
                result.Add(view);
            }

            return result ?? (IReadOnlyList<MobaContinuousRuntimeView>)Array.Empty<MobaContinuousRuntimeView>();
        }

        private MobaContinuousRuntimeView CreateView(IContinuous continuous)
        {
            var config = continuous?.Config;
            if (config == null) return null;

            var debug = ResolveDebugInfo(continuous);
            var ownerActorId = ResolveOwnerActorId(config, in debug);
            var duration = config is IDurationConfig durationConfig && durationConfig.DurationSeconds.HasValue ? durationConfig.DurationSeconds.Value : 0f;
            var remaining = ResolveRemainingSeconds(continuous, duration);
            var stack = config is IStackConfig stackConfig ? stackConfig.Stack : 1;
            var maxStack = config is IStackConfig stackConfigForMax ? stackConfigForMax.MaxStack : 1;
            var periodic = config as IMobaContinuousPeriodicConfig;
            var intervalState = continuous as IMobaContinuousIntervalState;
            var projection = config as IMobaContinuousProjectionConfig;

            return new MobaContinuousRuntimeView(
                continuous,
                config.Id,
                string.IsNullOrEmpty(debug.Kind) ? continuous.GetType().Name : debug.Kind,
                debug.ConfigId,
                config.OwnerId,
                ownerActorId,
                debug.SourceActorId,
                debug.TargetActorId,
                debug.SourceContextId,
                debug.ParentContextId,
                debug.RootContextId,
                debug.OwnerContextId,
                debug.SkillRuntimeHandle,
                continuous.State,
                continuous.IsActive,
                continuous.IsPaused,
                continuous.IsTerminated,
                continuous.ElapsedSeconds,
                duration,
                remaining,
                stack,
                maxStack,
                periodic?.IntervalSeconds ?? 0f,
                intervalState?.IntervalRemainingSeconds ?? 0f,
                CopyIntervalEffectIds(periodic),
                BuildTagViews(config as IMobaContinuousTagConfig, projection),
                BuildModifierViews(config as IMobaContinuousModifierConfig, projection, stack),
                BuildModifierExplanations(continuous, config as IMobaContinuousModifierConfig, projection, stack),
                GetLifecycleReason(continuous),
                GetTagRuleResult(continuous),
                ResolveContextSource(continuous, in debug));
        }

        private static MobaContinuousRuntimeDebugInfo ResolveDebugInfo(IContinuous continuous)
        {
            if (continuous is IMobaContinuousRuntimeDebugSource source && source.TryGetRuntimeDebugInfo(out var info))
                return info;

            return default;
        }

        private static MobaContextSourceView ResolveContextSource(IContinuous continuous, in MobaContinuousRuntimeDebugInfo debug)
        {
            if (debug.ContextSource.IsValid) return debug.ContextSource;
            if (continuous is IMobaContextSourceProvider provider && provider.TryGetContextSource(out var source) && source.IsValid) return source;
            if (debug.Kind != null || debug.ConfigId != 0 || debug.SourceActorId != 0 || debug.TargetActorId != 0 || debug.SourceContextId != 0 || debug.SkillRuntimeHandle.IsValid)
                return MobaContextSourceView.FromRuntimeDebug(in debug);
            return default;
        }

        private static int ResolveOwnerActorId(IContinuousConfig config, in MobaContinuousRuntimeDebugInfo debug)
        {
            if (config is IMobaContinuousProjectionConfig projection && projection.OwnerActorId > 0)
                return projection.OwnerActorId;

            if (debug.TargetActorId > 0)
                return debug.TargetActorId;

            var ownerId = config.OwnerId;
            return ownerId > 0 && ownerId <= int.MaxValue ? (int)ownerId : 0;
        }

        private static float ResolveRemainingSeconds(IContinuous continuous, float duration)
        {
            if (duration <= 0f) return 0f;
            var remaining = duration - continuous.ElapsedSeconds;
            return remaining > 0f ? remaining : 0f;
        }

        private static IReadOnlyList<int> CopyIntervalEffectIds(IMobaContinuousPeriodicConfig periodic)
        {
            var ids = periodic?.IntervalEffectIds;
            if (ids == null || ids.Count == 0) return Array.Empty<int>();

            var result = new int[ids.Count];
            for (int i = 0; i < ids.Count; i++)
                result[i] = ids[i];
            return result;
        }

        private static IReadOnlyList<MobaContinuousRuntimeTagView> BuildTagViews(IMobaContinuousTagConfig tagConfig, IMobaContinuousProjectionConfig projection)
        {
            var tags = tagConfig?.TagRequirements?.ApplicationTags;
            if (tags == null || tags.Count == 0) return Array.Empty<MobaContinuousRuntimeTagView>();

            var result = new List<MobaContinuousRuntimeTagView>(tags.Count);
            var source = projection?.TagSource ?? GameplayTagSource.System;
            foreach (var tag in tags)
            {
                if (!tag.IsValid) continue;
                result.Add(new MobaContinuousRuntimeTagView(tag.Value, tag.TagName, source));
            }

            return result;
        }

        private IReadOnlyList<MobaContinuousModifierExplainResult> BuildModifierExplanations(IContinuous continuous, IMobaContinuousModifierConfig modifierConfig, IMobaContinuousProjectionConfig projection, int stack)
        {
            var modifiers = modifierConfig?.Modifiers;
            if (continuous == null || modifiers == null || modifiers.Count == 0 || projection == null || _modifiers == null)
                return Array.Empty<MobaContinuousModifierExplainResult>();

            List<MobaContinuousModifierExplainResult> result = null;
            var normalizedStack = stack < 1 ? 1 : stack;
            for (int i = 0; i < modifiers.Count; i++)
            {
                var spec = modifiers[i];
                if (spec == null) continue;

                var entry = new MobaContinuousModifierEntry(continuous, projection, spec, normalizedStack);
                if (!_modifiers.TryExplainModifier(entry, out var explanation)) continue;

                result ??= new List<MobaContinuousModifierExplainResult>(modifiers.Count);
                result.Add(explanation);
            }

            return result ?? (IReadOnlyList<MobaContinuousModifierExplainResult>)Array.Empty<MobaContinuousModifierExplainResult>();
        }

        private static IReadOnlyList<MobaContinuousRuntimeModifierView> BuildModifierViews(IMobaContinuousModifierConfig modifierConfig, IMobaContinuousProjectionConfig projection, int stack)
        {
            var modifiers = modifierConfig?.Modifiers;
            if (modifiers == null || modifiers.Count == 0) return Array.Empty<MobaContinuousRuntimeModifierView>();

            var result = new List<MobaContinuousRuntimeModifierView>(modifiers.Count);
            var sourceId = projection?.ModifierSourceId ?? 0;
            for (int i = 0; i < modifiers.Count; i++)
            {
                var spec = modifiers[i];
                if (spec == null) continue;
                var magnitude = spec.Magnitude;
                result.Add(new MobaContinuousRuntimeModifierView(
                    spec.TargetKind,
                    spec.TargetId,
                    spec.Op,
                    spec.Value,
                    magnitude.Type,
                    magnitude.BaseValue,
                    magnitude.Coefficient,
                    spec.EvaluationPolicy,
                    spec.Priority,
                    stack < 1 ? 1 : stack,
                    sourceId));
            }

            return result;
        }

        private void SetReason(IContinuous continuous, string lastEvent, string reason, ContinuousEndReason endReason)
        {
            if (continuous == null) return;
            _reasons[continuous] = new MobaContinuousLifecycleReason(lastEvent, reason, endReason);
        }
    }
}
