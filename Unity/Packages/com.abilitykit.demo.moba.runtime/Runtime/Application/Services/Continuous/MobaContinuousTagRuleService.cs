using System.Collections.Generic;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Core.Continuous;
using AbilityKit.GameplayTags;

namespace AbilityKit.Demo.Moba.Services
{
    public interface IMobaContinuousTagRuleService
    {
        bool CanActivate(IContinuous continuous, IContinuousManager manager, out string reason);
        void ReconcileOwner(int ownerActorId);
        void ReconcileOwnerFor(IContinuous continuous);
        MobaContinuousTagRuleResult Explain(IContinuous continuous);
        MobaContinuousTagRuleResult GetLastResult(IContinuous continuous);
    }

    public enum MobaContinuousTagRuleAction
    {
        None,
        CanActivate,
        BlockActivate,
        KeepActive,
        Pause,
        Resume,
        Remove,
    }

    public readonly struct MobaContinuousTagRuleTagView
    {
        public MobaContinuousTagRuleTagView(int tagId, string tagName)
        {
            TagId = tagId;
            TagName = tagName;
        }

        public int TagId { get; }
        public string TagName { get; }
    }

    public readonly struct MobaContinuousTagRequirementCheck
    {
        public MobaContinuousTagRequirementCheck(
            string category,
            bool satisfied,
            bool requiredSatisfied,
            bool blockedSatisfied,
            bool exact,
            IReadOnlyList<MobaContinuousTagRuleTagView> requiredTags,
            IReadOnlyList<MobaContinuousTagRuleTagView> blockedTags)
        {
            Category = category;
            Satisfied = satisfied;
            RequiredSatisfied = requiredSatisfied;
            BlockedSatisfied = blockedSatisfied;
            Exact = exact;
            RequiredTags = requiredTags ?? new List<MobaContinuousTagRuleTagView>(0);
            BlockedTags = blockedTags ?? new List<MobaContinuousTagRuleTagView>(0);
        }

        public string Category { get; }
        public bool Satisfied { get; }
        public bool RequiredSatisfied { get; }
        public bool BlockedSatisfied { get; }
        public bool Exact { get; }
        public IReadOnlyList<MobaContinuousTagRuleTagView> RequiredTags { get; }
        public IReadOnlyList<MobaContinuousTagRuleTagView> BlockedTags { get; }
    }

    public readonly struct MobaContinuousTagRuleResult
    {
        public MobaContinuousTagRuleResult(
            int ownerActorId,
            MobaContinuousTagRuleAction action,
            bool allowed,
            string reason,
            IReadOnlyList<MobaContinuousTagRuleTagView> effectiveTags,
            IReadOnlyList<MobaContinuousTagRequirementCheck> checks)
        {
            OwnerActorId = ownerActorId;
            Action = action;
            Allowed = allowed;
            Reason = reason;
            EffectiveTags = effectiveTags ?? new List<MobaContinuousTagRuleTagView>(0);
            Checks = checks ?? new List<MobaContinuousTagRequirementCheck>(0);
        }

        public int OwnerActorId { get; }
        public MobaContinuousTagRuleAction Action { get; }
        public bool Allowed { get; }
        public string Reason { get; }
        public IReadOnlyList<MobaContinuousTagRuleTagView> EffectiveTags { get; }
        public IReadOnlyList<MobaContinuousTagRequirementCheck> Checks { get; }
        public bool HasResult => Action != MobaContinuousTagRuleAction.None || !string.IsNullOrEmpty(Reason);

        public static MobaContinuousTagRuleResult None => default;
    }

    [WorldService(typeof(IMobaContinuousTagRuleService))]
    [WorldService(typeof(MobaContinuousTagRuleService))]
    public sealed class MobaContinuousTagRuleService : IMobaContinuousTagRuleService, IContinuousAdmissionPolicy, IContinuousLifecycleBinder, IWorldInitializable, IService
    {
        private const int MaxReconcilePasses = 8;

        private readonly HashSet<IContinuous> _pausedByTags = new HashSet<IContinuous>();
        private readonly Dictionary<IContinuous, MobaContinuousTagRuleResult> _lastResults = new Dictionary<IContinuous, MobaContinuousTagRuleResult>();
        private bool _reconciling;

        private IMobaEffectiveTagQueryService _effectiveTags;
        private MobaContinuousManager _continuous;

        public void OnInit(IWorldResolver services)
        {
            if (services == null) return;

            services.TryResolve(out _effectiveTags);
            services.TryResolve(out _continuous);

            _continuous?.AddAdmissionPolicy(this);
            _continuous?.AddLifecycleBinder(this);
        }

        public void Dispose()
        {
            if (_continuous != null)
            {
                _continuous.RemoveAdmissionPolicy(this);
                _continuous.RemoveLifecycleBinder(this);
            }

            _pausedByTags.Clear();
            _lastResults.Clear();
            _effectiveTags = null;
            _continuous = null;
            _reconciling = false;
        }

        public bool CanRegister(IContinuous continuous, IContinuousManager manager, out string reason)
        {
            reason = null;
            return continuous != null && continuous.Config != null;
        }

        public bool CanActivate(IContinuous continuous, IContinuousManager manager, out string reason)
        {
            reason = null;
            if (continuous == null || continuous.Config == null)
            {
                reason = "Continuous or config is null";
                SetResult(continuous, new MobaContinuousTagRuleResult(0, MobaContinuousTagRuleAction.BlockActivate, false, reason, null, null));
                return false;
            }

            if (!(continuous.Config is IMobaContinuousTagConfig tagConfig))
            {
                SetResult(continuous, new MobaContinuousTagRuleResult(ResolveOwnerActorId(continuous), MobaContinuousTagRuleAction.CanActivate, true, "No tag config", null, null));
                return true;
            }

            var requirements = tagConfig.TagRequirements;
            if (requirements == null)
            {
                SetResult(continuous, new MobaContinuousTagRuleResult(ResolveOwnerActorId(continuous), MobaContinuousTagRuleAction.CanActivate, true, "No tag requirements", null, null));
                return true;
            }

            var ownerActorId = ResolveOwnerActorId(continuous);
            var tags = _effectiveTags?.GetEffectiveTags(ownerActorId);
            var activationCheck = BuildRequirementCheck("Activation", requirements.ActivationRequired, tags);
            var removalCheck = BuildRemovalCheck(requirements, tags);
            if (!requirements.CanActivate(tags))
            {
                reason = "Blocked by MOBA continuous activation tags";
                SetResult(continuous, BuildResult(ownerActorId, MobaContinuousTagRuleAction.BlockActivate, false, reason, tags, activationCheck, removalCheck));
                return false;
            }

            if (requirements.ShouldRemove(tags))
            {
                reason = "Blocked by MOBA continuous removal tags";
                SetResult(continuous, BuildResult(ownerActorId, MobaContinuousTagRuleAction.BlockActivate, false, reason, tags, activationCheck, removalCheck));
                return false;
            }

            SetResult(continuous, BuildResult(ownerActorId, MobaContinuousTagRuleAction.CanActivate, true, "Activation tags satisfied", tags, activationCheck, removalCheck));
            return true;
        }

        public MobaContinuousTagRuleResult Explain(IContinuous continuous)
        {
            if (continuous == null || continuous.Config == null)
                return new MobaContinuousTagRuleResult(0, MobaContinuousTagRuleAction.None, false, "Continuous or config is null", null, null);

            if (!(continuous.Config is IMobaContinuousTagConfig tagConfig) || tagConfig.TagRequirements == null)
                return new MobaContinuousTagRuleResult(ResolveOwnerActorId(continuous), MobaContinuousTagRuleAction.None, true, "No tag requirements", null, null);

            var ownerActorId = ResolveOwnerActorId(continuous);
            var tags = _effectiveTags?.GetEffectiveTags(ownerActorId);
            var requirements = tagConfig.TagRequirements;
            var activationCheck = BuildRequirementCheck("Activation", requirements.ActivationRequired, tags);
            var removalCheck = BuildRemovalCheck(requirements, tags);
            var ongoingCheck = BuildRequirementCheck("Ongoing", requirements.OngoingRequired, tags);

            MobaContinuousTagRuleAction action;
            var allowed = true;
            var reason = "Tag rules satisfied";
            if (!activationCheck.Satisfied)
            {
                action = MobaContinuousTagRuleAction.BlockActivate;
                allowed = false;
                reason = "Activation requirements are not satisfied";
            }
            else if (removalCheck.Satisfied)
            {
                action = MobaContinuousTagRuleAction.Remove;
                allowed = false;
                reason = "Removal requirements are satisfied";
            }
            else if (!ongoingCheck.Satisfied)
            {
                action = MobaContinuousTagRuleAction.Pause;
                allowed = false;
                reason = "Ongoing requirements are not satisfied";
            }
            else
            {
                action = continuous.IsPaused && _pausedByTags.Contains(continuous)
                    ? MobaContinuousTagRuleAction.Resume
                    : MobaContinuousTagRuleAction.KeepActive;
            }

            return BuildResult(ownerActorId, action, allowed, reason, tags, activationCheck, removalCheck, ongoingCheck);
        }

        public MobaContinuousTagRuleResult GetLastResult(IContinuous continuous)
        {
            if (continuous == null) return MobaContinuousTagRuleResult.None;
            return _lastResults.TryGetValue(continuous, out var result) ? result : MobaContinuousTagRuleResult.None;
        }

        public void ReconcileOwnerFor(IContinuous continuous)
        {
            var ownerActorId = ResolveOwnerActorId(continuous);
            if (ownerActorId > 0)
                ReconcileOwner(ownerActorId);
        }

        public void ReconcileOwner(int ownerActorId)
        {
            if (ownerActorId <= 0 || _continuous == null || _effectiveTags == null) return;
            if (_reconciling) return;

            _reconciling = true;
            try
            {
                for (int pass = 0; pass < MaxReconcilePasses; pass++)
                {
                    _effectiveTags.MarkDirty(ownerActorId);
                    var tags = _effectiveTags.GetEffectiveTags(ownerActorId);
                    var changed = ReconcileOwnerPass(ownerActorId, tags);
                    if (!changed) break;
                }
            }
            finally
            {
                _reconciling = false;
            }
        }

        public void OnRegistered(IContinuous continuous, IContinuousManager manager)
        {
            SetResult(continuous, Explain(continuous));
            ReconcileOwnerFor(continuous);
        }

        public void OnActivated(IContinuous continuous, IContinuousManager manager)
        {
            _pausedByTags.Remove(continuous);
            SetResult(continuous, Explain(continuous));
            ReconcileOwnerFor(continuous);
        }

        public void OnPaused(IContinuous continuous, IContinuousManager manager)
        {
            MarkDirty(continuous);
            SetResult(continuous, Explain(continuous));
        }

        public void OnResumed(IContinuous continuous, IContinuousManager manager)
        {
            _pausedByTags.Remove(continuous);
            SetResult(continuous, Explain(continuous));
            ReconcileOwnerFor(continuous);
        }

        public void OnEnded(IContinuous continuous, ContinuousEndReason reason, IContinuousManager manager)
        {
            _pausedByTags.Remove(continuous);
            SetResult(continuous, Explain(continuous));
            ReconcileOwnerFor(continuous);
        }

        public void OnUnregistered(IContinuous continuous, ContinuousEndReason reason, IContinuousManager manager)
        {
            _pausedByTags.Remove(continuous);
            SetResult(continuous, Explain(continuous));
            ReconcileOwnerFor(continuous);
        }

        private bool ReconcileOwnerPass(int ownerActorId, GameplayTagContainer tags)
        {
            var ownerContinuous = _continuous.GetOwnerContinuous(ownerActorId);
            if (ownerContinuous == null || ownerContinuous.Count == 0) return false;

            var snapshot = new List<IContinuous>(ownerContinuous);
            var changed = false;
            for (int i = 0; i < snapshot.Count; i++)
            {
                var continuous = snapshot[i];
                if (!CanEvaluate(continuous)) continue;

                var requirements = GetRequirements(continuous);
                if (requirements == null) continue;

                if (continuous.IsActive && requirements.ShouldRemove(tags))
                {
                    _pausedByTags.Remove(continuous);
                    SetResult(continuous, Explain(continuous));
                    changed = _continuous.TryInterrupt(continuous, "Removed by MOBA continuous tags") || changed;
                    continue;
                }

                if (continuous.IsActive && !requirements.IsOngoingSatisfied(tags))
                {
                    SetResult(continuous, Explain(continuous));
                    if (_continuous.TryPause(continuous))
                    {
                        _pausedByTags.Add(continuous);
                        changed = true;
                    }

                    continue;
                }

                if (continuous.IsPaused && _pausedByTags.Contains(continuous))
                {
                    if (!requirements.ShouldRemove(tags) && requirements.IsOngoingSatisfied(tags) && _continuous.TryResume(continuous))
                    {
                        _pausedByTags.Remove(continuous);
                        SetResult(continuous, Explain(continuous));
                        changed = true;
                    }
                }
            }

            return changed;
        }

        private static MobaContinuousTagRuleResult BuildResult(int ownerActorId, MobaContinuousTagRuleAction action, bool allowed, string reason, GameplayTagContainer tags, params MobaContinuousTagRequirementCheck[] checks)
        {
            return new MobaContinuousTagRuleResult(ownerActorId, action, allowed, reason, CopyTagViews(tags), checks);
        }

        private static MobaContinuousTagRequirementCheck BuildRemovalCheck(ContinuousTagRequirements requirements, GameplayTagContainer tags)
        {
            var removal = requirements?.RemovalRequired ?? new GameplayTagRequirements();
            var satisfied = requirements != null && requirements.ShouldRemove(tags);
            var requiredSatisfied = removal.Required == null || removal.Required.IsEmpty || (tags != null && tags.HasAll(removal.Required, removal.Exact));
            var blockedSatisfied = removal.Blocked == null || removal.Blocked.IsEmpty || tags == null || !tags.HasAny(removal.Blocked, removal.Exact);
            return new MobaContinuousTagRequirementCheck("Removal", satisfied, requiredSatisfied, blockedSatisfied, removal.Exact, CopyTagViews(removal.Required), CopyTagViews(removal.Blocked));
        }

        private static MobaContinuousTagRequirementCheck BuildRequirementCheck(string category, GameplayTagRequirements requirements, GameplayTagContainer tags)
        {
            var requiredSatisfied = requirements.Required == null || requirements.Required.IsEmpty || (tags != null && tags.HasAll(requirements.Required, requirements.Exact));
            var blockedSatisfied = requirements.Blocked == null || requirements.Blocked.IsEmpty || tags == null || !tags.HasAny(requirements.Blocked, requirements.Exact);
            return new MobaContinuousTagRequirementCheck(category, requiredSatisfied && blockedSatisfied, requiredSatisfied, blockedSatisfied, requirements.Exact, CopyTagViews(requirements.Required), CopyTagViews(requirements.Blocked));
        }

        private static IReadOnlyList<MobaContinuousTagRuleTagView> CopyTagViews(GameplayTagContainer tags)
        {
            if (tags == null || tags.Count == 0) return new List<MobaContinuousTagRuleTagView>(0);

            var result = new List<MobaContinuousTagRuleTagView>(tags.Count);
            foreach (var tag in tags)
            {
                if (!tag.IsValid) continue;
                result.Add(new MobaContinuousTagRuleTagView(tag.Value, tag.TagName));
            }

            return result;
        }

        private void SetResult(IContinuous continuous, MobaContinuousTagRuleResult result)
        {
            if (continuous == null) return;
            _lastResults[continuous] = result;
        }

        private void MarkDirty(IContinuous continuous)
        {
            var ownerActorId = ResolveOwnerActorId(continuous);
            if (ownerActorId > 0)
                _effectiveTags?.MarkDirty(ownerActorId);
        }

        private static bool CanEvaluate(IContinuous continuous)
        {
            return continuous != null && !continuous.IsTerminated && continuous.Config is IMobaContinuousTagConfig;
        }

        private static ContinuousTagRequirements GetRequirements(IContinuous continuous)
        {
            return (continuous?.Config as IMobaContinuousTagConfig)?.TagRequirements;
        }

        private static int ResolveOwnerActorId(IContinuous continuous)
        {
            if (continuous?.Config is IMobaContinuousProjectionConfig projection && projection.OwnerActorId > 0)
                return projection.OwnerActorId;

            var ownerId = continuous?.Config?.OwnerId ?? 0L;
            return ownerId > 0 && ownerId <= int.MaxValue ? (int)ownerId : 0;
        }
    }
}
