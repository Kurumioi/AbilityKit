using System;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Demo.Moba.Components;
using AbilityKit.GameplayTags;

namespace AbilityKit.Demo.Moba.Services
{
    public sealed class CheckCooldownHandler : ISkillHandler
    {
        private readonly MobaActorLookupService _actors;
        private readonly IFrameTime _time;

        public int HandlerType => (int)EHandlerType.CheckCooldown;

        public CheckCooldownHandler(MobaActorLookupService actors, IFrameTime time)
        {
            _actors = actors;
            _time = time;
        }

        public HandlerResult Execute(in HandlerContext context)
        {
            if (!SkillHandlerRuntimeAccess.TryGetActiveSkill(_actors, context.CasterActorId, context.PipelineContext.SkillSlot, context.PipelineContext.SkillId, out var runtime))
            {
                return HandlerResult.Ok;
            }

            var currentTimeMs = SkillHandlerRuntimeAccess.GetCurrentTimeMs(_time);
            if (runtime.CooldownEndTimeMs <= currentTimeMs)
            {
                return HandlerResult.Ok;
            }

            var remainingMs = runtime.CooldownEndTimeMs - currentTimeMs;
            var remainingSec = remainingMs / 1000.0;
            return HandlerResult.Fail($"冷却中 ({remainingSec:F1}s)", "cooldown", remainingSec);
        }
    }

    public sealed class CheckResourceHandler : ISkillHandler
    {
        private readonly MobaActorLookupService _actors;

        public int HandlerType => (int)EHandlerType.CheckResource;

        public CheckResourceHandler(MobaActorLookupService actors)
        {
            _actors = actors;
        }

        public HandlerResult Execute(in HandlerContext context)
        {
            var dto = context.CurrentDto as CheckResourceDTO;
            if (dto == null) return HandlerResult.Ok;

            var resourceType = (ResourceType)dto.ResourceType;
            var requiredAmount = dto.MinAmount?.ConstValue ?? 0;
            if (requiredAmount <= 0f) return HandlerResult.Ok;

            if (!SkillHandlerRuntimeAccess.TryGetResourceAmount(_actors, context.CasterActorId, resourceType, out var currentAmount))
            {
                return HandlerResult.Fail("资源不足", "not_enough_resource", requiredAmount, 0f);
            }

            if (currentAmount < requiredAmount)
            {
                return HandlerResult.Fail("资源不足", "not_enough_resource", requiredAmount, currentAmount);
            }

            return HandlerResult.Ok;
        }
    }

    public sealed class ConsumeResourceHandler : ISkillHandler
    {
        private readonly MobaActorLookupService _actors;

        public int HandlerType => (int)EHandlerType.ConsumeResource;

        public ConsumeResourceHandler(MobaActorLookupService actors)
        {
            _actors = actors;
        }

        public HandlerResult Execute(in HandlerContext context)
        {
            var dto = context.CurrentDto as ConsumeResourceDTO;
            if (dto == null) return HandlerResult.Ok;

            var resourceType = (ResourceType)dto.ResourceType;
            var amount = (float)(dto.Amount?.ConstValue ?? 0);
            if (amount <= 0f) return HandlerResult.Ok;

            if (!SkillHandlerRuntimeAccess.TryConsumeResource(_actors, context.CasterActorId, resourceType, amount, out var currentAmount))
            {
                return HandlerResult.Fail(dto.FailMessageKey ?? "资源不足", "not_enough_resource", amount, currentAmount);
            }

            return HandlerResult.Ok;
        }
    }

    public sealed class StartCooldownHandler : ISkillHandler
    {
        private readonly MobaActorLookupService _actors;
        private readonly IFrameTime _time;

        public int HandlerType => (int)EHandlerType.StartCooldown;

        public StartCooldownHandler(MobaActorLookupService actors, IFrameTime time)
        {
            _actors = actors;
            _time = time;
        }

        public HandlerResult Execute(in HandlerContext context)
        {
            var dto = context.CurrentDto as StartCooldownDTO;
            if (dto == null) return HandlerResult.Ok;

            var cooldownMs = (long)MathF.Round((float)(dto.CooldownMs?.ConstValue ?? context.PipelineContext.SkillCooldownMs));
            if (cooldownMs <= 0L) return HandlerResult.Ok;

            var endTimeMs = SkillHandlerRuntimeAccess.GetCurrentTimeMs(_time) + cooldownMs;
            SkillHandlerRuntimeAccess.TrySetActiveSkillCooldown(_actors, context.CasterActorId, context.PipelineContext.SkillSlot, context.PipelineContext.SkillId, endTimeMs);
            return HandlerResult.Ok;
        }
    }

    public sealed class ApplyBuffHandler : ISkillHandler
    {
        private readonly MobaActorLookupService _actors;

        public int HandlerType => (int)EHandlerType.ApplyBuff;

        public ApplyBuffHandler(MobaActorLookupService actors)
        {
            _actors = actors;
        }

        public HandlerResult Execute(in HandlerContext context)
        {
            var dto = context.CurrentDto as ApplyBuffDTO;
            if (dto == null) return HandlerResult.Ok;

            var targetActorId = SkillHandlerRuntimeAccess.ResolveTargetActorId(context, dto.Target);
            if (targetActorId <= 0) return HandlerResult.Ok;
            if (_actors == null || !_actors.TryGetActorEntity(targetActorId, out var entity) || entity == null) return HandlerResult.Ok;

            return HandlerResult.Ok;
        }
    }

    public sealed class AddTagHandler : ISkillHandler
    {
        private readonly MobaActorLookupService _actors;
        private readonly IGameplayTagService _tags;

        public int HandlerType => (int)EHandlerType.AddTag;

        public AddTagHandler(MobaActorLookupService actors, IGameplayTagService tags = null)
        {
            _actors = actors;
            _tags = tags;
        }

        public HandlerResult Execute(in HandlerContext context)
        {
            var dto = context.CurrentDto as AddTagDTO;
            if (dto == null || dto.Tags == null || dto.Tags.Length == 0) return HandlerResult.Ok;

            var targetActorId = SkillHandlerRuntimeAccess.ResolveTargetActorId(context, dto.Target);
            if (!SkillHandlerRuntimeAccess.ActorExists(_actors, targetActorId)) return HandlerResult.Ok;
            if (_tags == null) return HandlerResult.Ok;

            var source = SkillHandlerRuntimeAccess.CreateTagSource(context);
            for (int i = 0; i < dto.Tags.Length; i++)
            {
                if (!SkillHandlerRuntimeAccess.TryParseTag(dto.Tags[i], out var tag)) continue;
                _tags.AddTag(targetActorId, tag, source);
            }

            return HandlerResult.Ok;
        }
    }

    public sealed class RemoveTagHandler : ISkillHandler
    {
        private readonly MobaActorLookupService _actors;
        private readonly IGameplayTagService _tags;

        public int HandlerType => (int)EHandlerType.RemoveTag;

        public RemoveTagHandler(MobaActorLookupService actors, IGameplayTagService tags = null)
        {
            _actors = actors;
            _tags = tags;
        }

        public HandlerResult Execute(in HandlerContext context)
        {
            var dto = context.CurrentDto as RemoveTagDTO;
            if (dto == null || dto.Tags == null || dto.Tags.Length == 0) return HandlerResult.Ok;

            var targetActorId = SkillHandlerRuntimeAccess.ResolveTargetActorId(context, dto.Target);
            if (!SkillHandlerRuntimeAccess.ActorExists(_actors, targetActorId)) return HandlerResult.Ok;
            if (_tags == null) return HandlerResult.Ok;

            var source = SkillHandlerRuntimeAccess.CreateTagSource(context);
            for (int i = 0; i < dto.Tags.Length; i++)
            {
                if (!SkillHandlerRuntimeAccess.TryParseTag(dto.Tags[i], out var tag)) continue;
                _tags.RemoveTag(targetActorId, tag, source);
            }

            return HandlerResult.Ok;
        }
    }

    internal static class SkillHandlerRuntimeAccess
    {
        public static long GetCurrentTimeMs(IFrameTime time)
        {
            return time != null ? (long)MathF.Round(time.Time * 1000f) : 0L;
        }

        public static bool TryGetActiveSkill(MobaActorLookupService actors, int actorId, int slot, int skillId, out ActiveSkillRuntime runtime)
        {
            runtime = null;
            if (!TryGetSkillLoadout(actors, actorId, out var skills)) return false;
            if (slot <= 0 || slot > skills.Length) return false;

            runtime = skills[slot - 1];
            return runtime != null && runtime.SkillId == skillId;
        }

        public static bool TrySetActiveSkillCooldown(MobaActorLookupService actors, int actorId, int slot, int skillId, long cooldownEndTimeMs)
        {
            if (!TryGetActiveSkill(actors, actorId, slot, skillId, out var runtime)) return false;
            runtime.CooldownEndTimeMs = cooldownEndTimeMs;
            return true;
        }

        public static bool TryGetResourceAmount(MobaActorLookupService actors, int actorId, ResourceType resourceType, out float amount)
        {
            amount = 0f;
            if (!TryGetResourceState(actors, actorId, resourceType, out var state)) return false;
            amount = state.Current;
            return true;
        }

        public static bool TryConsumeResource(MobaActorLookupService actors, int actorId, ResourceType resourceType, float amount, out float currentAmount)
        {
            currentAmount = 0f;
            if (amount <= 0f) return true;
            if (!TryGetResourceState(actors, actorId, resourceType, out var state)) return false;

            currentAmount = state.Current;
            if (state.Current < amount) return false;

            state.Current -= amount;
            currentAmount = state.Current;
            return true;
        }

        public static int ResolveTargetActorId(in HandlerContext context, int target)
        {
            return target == 0 ? context.CasterActorId : context.TargetActorId;
        }

        public static bool ActorExists(MobaActorLookupService actors, int actorId)
        {
            return actorId > 0 && actors != null && actors.TryGetActorEntity(actorId, out var entity) && entity != null;
        }

        public static GameplayTagSource CreateTagSource(in HandlerContext context)
        {
            var sourceId = context.PipelineContext != null && context.PipelineContext.SourceContextId != 0L
                ? context.PipelineContext.SourceContextId
                : context.CasterActorId;
            return sourceId != 0L ? new GameplayTagSource(sourceId) : GameplayTagSource.System;
        }

        public static bool TryParseTag(string raw, out GameplayTag tag)
        {
            tag = default;
            if (string.IsNullOrWhiteSpace(raw)) return false;
            if (!int.TryParse(raw, out var tagId)) return false;
            if (tagId <= 0) return false;

            tag = GameplayTag.FromId(tagId);
            return tag.IsValid;
        }

        private static bool TryGetSkillLoadout(MobaActorLookupService actors, int actorId, out ActiveSkillRuntime[] skills)
        {
            skills = null;
            if (actors == null) return false;
            if (!actors.TryGetActorEntity(actorId, out var entity) || entity == null) return false;
            if (!entity.hasSkillLoadout) return false;

            skills = entity.skillLoadout.ActiveSkills;
            return skills != null;
        }

        private static bool TryGetResourceState(MobaActorLookupService actors, int actorId, ResourceType resourceType, out ResourceState state)
        {
            state = null;
            if (actors == null) return false;
            if (resourceType == ResourceType.None) return false;
            if (!actors.TryGetActorEntity(actorId, out var entity) || entity == null) return false;
            if (!entity.hasResourceContainer || entity.resourceContainer.Value == null) return false;

            var container = entity.resourceContainer.Value;
            if (container.Map == null) return false;
            return container.Map.TryGetValue(resourceType, out state) && state != null;
        }
    }
}
