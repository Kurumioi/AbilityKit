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
    }

    [WorldService(typeof(IMobaContinuousTagRuleService))]
    [WorldService(typeof(MobaContinuousTagRuleService))]
    public sealed class MobaContinuousTagRuleService : IMobaContinuousTagRuleService, IContinuousAdmissionPolicy, IContinuousLifecycleBinder, IWorldInitializable, IService
    {
        private const int MaxReconcilePasses = 8;

        private readonly HashSet<IContinuous> _pausedByTags = new HashSet<IContinuous>();
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
                return false;
            }

            if (!(continuous.Config is IMobaContinuousTagConfig tagConfig))
                return true;

            var requirements = tagConfig.TagRequirements;
            if (requirements == null)
                return true;

            var ownerActorId = ResolveOwnerActorId(continuous);
            var tags = _effectiveTags?.GetEffectiveTags(ownerActorId);
            if (!requirements.CanActivate(tags))
            {
                reason = "Blocked by MOBA continuous activation tags";
                return false;
            }

            if (requirements.ShouldRemove(tags))
            {
                reason = "Blocked by MOBA continuous removal tags";
                return false;
            }

            return true;
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
            ReconcileOwnerFor(continuous);
        }

        public void OnActivated(IContinuous continuous, IContinuousManager manager)
        {
            _pausedByTags.Remove(continuous);
            ReconcileOwnerFor(continuous);
        }

        public void OnPaused(IContinuous continuous, IContinuousManager manager)
        {
            MarkDirty(continuous);
        }

        public void OnResumed(IContinuous continuous, IContinuousManager manager)
        {
            _pausedByTags.Remove(continuous);
            ReconcileOwnerFor(continuous);
        }

        public void OnEnded(IContinuous continuous, ContinuousEndReason reason, IContinuousManager manager)
        {
            _pausedByTags.Remove(continuous);
            ReconcileOwnerFor(continuous);
        }

        public void OnUnregistered(IContinuous continuous, ContinuousEndReason reason, IContinuousManager manager)
        {
            _pausedByTags.Remove(continuous);
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
                    changed = _continuous.TryInterrupt(continuous, "Removed by MOBA continuous tags") || changed;
                    continue;
                }

                if (continuous.IsActive && !requirements.IsOngoingSatisfied(tags))
                {
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
                        changed = true;
                    }
                }
            }

            return changed;
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
