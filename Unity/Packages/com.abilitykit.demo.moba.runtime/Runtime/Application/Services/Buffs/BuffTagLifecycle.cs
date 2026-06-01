using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Demo.Moba.Components;
using AbilityKit.GameplayTags;

namespace AbilityKit.Demo.Moba.Services
{
    internal static class BuffTagLifecycle
    {
        public static ContinuousTagRequirements ResolveRequirements(BuffMO buff, IMobaContinuousTagTemplateRegistry registry)
        {
            if (buff == null) return null;
            if (buff.ContinuousTagTemplateId <= 0) return null;
            if (registry == null) return null;

            return registry.TryGet(buff.ContinuousTagTemplateId, out var requirements) ? requirements : null;
        }

        public static void AssignRequirements(BuffRuntime runtime, BuffMO buff, IMobaContinuousTagTemplateRegistry registry)
        {
            if (runtime == null) return;
            runtime.TagRequirements = ResolveRequirements(buff, registry);
        }

        public static bool CanActivate(IGameplayTagService tags, int targetActorId, ContinuousTagRequirements requirements)
        {
            if (requirements == null) return true;
            var current = tags?.GetTags(targetActorId);
            return requirements.CanActivate(current);
        }

        public static bool ShouldEnd(IGameplayTagService tags, int targetActorId, ContinuousTagRequirements requirements)
        {
            if (requirements == null) return false;
            var current = tags?.GetTags(targetActorId);
            return !requirements.IsOngoingSatisfied(current) || requirements.ShouldRemove(current);
        }

        public static void ApplyApplicationTags(IGameplayTagService tags, int targetActorId, BuffRuntime runtime)
        {
            if (runtime == null) return;
            ApplyTags(tags, targetActorId, runtime.TagRequirements?.ApplicationTags, CreateSource(runtime));
        }

        public static void RemoveApplicationTags(IGameplayTagService tags, int targetActorId, BuffRuntime runtime)
        {
            if (runtime == null) return;
            RemoveTags(tags, targetActorId, runtime.TagRequirements?.ApplicationTags, CreateSource(runtime));
        }

        public static void ApplyRemovalTags(IGameplayTagService tags, int targetActorId, BuffRuntime runtime)
        {
            if (runtime == null) return;
            ApplyTags(tags, targetActorId, runtime.TagRequirements?.RemovalTags, CreateSource(runtime));
        }

        private static GameplayTagSource CreateSource(BuffRuntime runtime)
        {
            if (runtime == null) return GameplayTagSource.System;
            if (runtime.SourceContextId != 0) return new GameplayTagSource(runtime.SourceContextId);
            if (runtime.SourceId != 0) return new GameplayTagSource(runtime.SourceId);
            return GameplayTagSource.System;
        }

        private static void ApplyTags(IGameplayTagService tags, int targetActorId, GameplayTagContainer container, GameplayTagSource source)
        {
            if (tags == null) return;
            if (targetActorId <= 0) return;
            if (container == null || container.Count == 0) return;

            foreach (var tag in container)
            {
                tags.AddTag(targetActorId, tag, source);
            }
        }

        private static void RemoveTags(IGameplayTagService tags, int targetActorId, GameplayTagContainer container, GameplayTagSource source)
        {
            if (tags == null) return;
            if (targetActorId <= 0) return;
            if (container == null || container.Count == 0) return;

            foreach (var tag in container)
            {
                tags.RemoveTag(targetActorId, tag, source);
            }
        }
    }
}
