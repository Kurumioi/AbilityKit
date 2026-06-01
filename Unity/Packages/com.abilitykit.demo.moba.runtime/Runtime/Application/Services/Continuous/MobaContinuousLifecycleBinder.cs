using System.Collections.Generic;
using AbilityKit.Core.Continuous;
using AbilityKit.GameplayTags;

namespace AbilityKit.Demo.Moba.Services
{
    internal sealed class MobaContinuousLifecycleBinder : IContinuousLifecycleBinder
    {
        private readonly IGameplayTagService _tags;
        private readonly MobaContinuousModifierProjectorRegistry _modifierProjectors;
        private readonly HashSet<string> _endedContinuousIds = new HashSet<string>();

        public MobaContinuousLifecycleBinder(IGameplayTagService tags, MobaContinuousModifierProjectorRegistry modifierProjectors)
        {
            _tags = tags;
            _modifierProjectors = modifierProjectors;
        }

        public void OnRegistered(IContinuous continuous, IContinuousManager manager)
        {
        }

        public void OnActivated(IContinuous continuous, IContinuousManager manager)
        {
            ApplyTags(continuous);
            ApplyModifiers(continuous);
        }

        public void OnPaused(IContinuous continuous, IContinuousManager manager)
        {
        }

        public void OnResumed(IContinuous continuous, IContinuousManager manager)
        {
        }

        public void OnEnded(IContinuous continuous, ContinuousEndReason reason, IContinuousManager manager)
        {
            RememberEndedContinuous(continuous);
            CleanupTagsAndModifiers(continuous, applyRemovalTags: IsTerminalEnd(reason));
        }

        public void OnUnregistered(IContinuous continuous, ContinuousEndReason reason, IContinuousManager manager)
        {
            var applyRemovalTags = !ConsumeEndedContinuous(continuous) && IsTerminalEnd(reason);
            CleanupTagsAndModifiers(continuous, applyRemovalTags);
        }

        private void ApplyTags(IContinuous continuous)
        {
            if (_tags == null) return;
            if (!(continuous?.Config is IMobaContinuousTagConfig tagConfig)) return;
            if (!TryGetProjection(continuous, out var projection)) return;

            ApplyTagContainer(projection.OwnerActorId, tagConfig.TagRequirements?.ApplicationTags, projection.TagSource);
        }

        private void CleanupTagsAndModifiers(IContinuous continuous, bool applyRemovalTags)
        {
            if (!TryGetProjection(continuous, out var projection)) return;
            var tagConfig = continuous.Config as IMobaContinuousTagConfig;

            if (_tags != null && tagConfig != null)
            {
                RemoveTagContainer(projection.OwnerActorId, tagConfig.TagRequirements?.ApplicationTags, projection.TagSource);
                if (applyRemovalTags)
                {
                    ApplyTagContainer(projection.OwnerActorId, tagConfig.TagRequirements?.RemovalTags, projection.TagSource);
                }
            }

            ClearModifiers(projection);
        }

        private void ApplyModifiers(IContinuous continuous)
        {
            if (!(continuous?.Config is IMobaContinuousModifierConfig config)) return;
            if (config.Modifiers == null || config.Modifiers.Count == 0) return;
            if (!TryGetProjection(continuous, out var projection)) return;

            _modifierProjectors?.Apply(projection, config.Modifiers);
        }

        private void ClearModifiers(IMobaContinuousProjectionConfig projection)
        {
            _modifierProjectors?.Clear(projection);
        }

        private static bool TryGetProjection(IContinuous continuous, out IMobaContinuousProjectionConfig projection)
        {
            projection = continuous?.Config as IMobaContinuousProjectionConfig;
            return projection != null && projection.OwnerActorId > 0;
        }

        private void ApplyTagContainer(int ownerId, GameplayTagContainer container, GameplayTagSource source)
        {
            if (_tags == null) return;
            if (ownerId <= 0) return;
            if (container == null || container.Count == 0) return;

            foreach (var tag in container)
            {
                _tags.AddTag(ownerId, tag, source);
            }
        }

        private void RemoveTagContainer(int ownerId, GameplayTagContainer container, GameplayTagSource source)
        {
            if (_tags == null) return;
            if (ownerId <= 0) return;
            if (container == null || container.Count == 0) return;

            foreach (var tag in container)
            {
                _tags.RemoveTag(ownerId, tag, source);
            }
        }

        private void RememberEndedContinuous(IContinuous continuous)
        {
            var id = continuous?.Config?.Id;
            if (string.IsNullOrEmpty(id)) return;
            _endedContinuousIds.Add(id);
        }

        private bool ConsumeEndedContinuous(IContinuous continuous)
        {
            var id = continuous?.Config?.Id;
            return !string.IsNullOrEmpty(id) && _endedContinuousIds.Remove(id);
        }

        private static bool IsTerminalEnd(ContinuousEndReason reason)
        {
            return reason == ContinuousEndReason.Completed || reason == ContinuousEndReason.Interrupted;
        }
    }
}
