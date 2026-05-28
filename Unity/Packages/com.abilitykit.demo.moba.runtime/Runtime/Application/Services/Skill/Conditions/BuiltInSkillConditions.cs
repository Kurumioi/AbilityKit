using System;
using AbilityKit.Ability.Share.ECS; using AbilityKit.ECS; using AbilityKit.Ability.Share.ECS;
using AbilityKit.Ability.Share.ECS.Entitas;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Pipeline;

namespace AbilityKit.Demo.Moba.Services
{
    using AbilityKit.Ability;
    /// <summary>
    /// 冷却检查条件
    /// </summary>
    [SkillCondition("cooldown", "冷却中")]
    public sealed class CooldownSkillCondition : SkillConditionBase
    {
        private readonly MobaActorLookupService _actors;
        private readonly IFrameTime _time;

        public override bool SupportsContinuousCheck => true;

        public CooldownSkillCondition(MobaActorLookupService actors, IFrameTime time)
        {
            _actors = actors;
            _time = time;
        }

        public override SkillConditionResult Check(SkillPipelineContext context)
        {
            if (context.SkillId <= 0)
                return Pass;

            var casterActorId = context.CasterActorId;
            var slot = context.SkillSlot;

            if (_actors == null || !_actors.TryGetActorEntity(casterActorId, out var entity) || entity == null)
                return Pass;

            if (!entity.hasSkillLoadout)
                return Pass;

            var skills = entity.skillLoadout.ActiveSkills;
            if (skills == null || slot <= 0 || slot > skills.Length)
                return Pass;

            var runtime = skills[slot - 1];
            if (runtime == null)
                return Pass;

            if (runtime.SkillId != context.SkillId)
                return Pass;

            var currentTimeMs = 0L;
            try
            {
                currentTimeMs = _time != null ? (long)System.MathF.Round(_time.Time * 1000f) : 0L;
            }
            catch
            {
                currentTimeMs = 0L;
            }

            if (runtime.CooldownEndTimeMs > currentTimeMs)
            {
                var remainingMs = runtime.CooldownEndTimeMs - currentTimeMs;
                var remainingSec = remainingMs / 1000.0;
                return Fail($"冷却中 ({remainingSec:F1}s)", "cooldown", remainingSec);
            }

            return Pass;
        }
    }

    /// <summary>
    /// 目标存在条件
    /// </summary>
    [SkillCondition("target_exists", "需要目标")]
    public sealed class TargetExistsSkillCondition : SkillConditionBase
    {
        public override SkillConditionResult Check(SkillPipelineContext context)
        {
            var targetActorId = context.TargetActorId;
            if (targetActorId <= 0 || targetActorId == context.CasterActorId)
            {
                return Fail("需要目标", "target_required");
            }
            return Pass;
        }
    }

    /// <summary>
    /// 自身释放条件（不需要目标）
    /// </summary>
    [SkillCondition("self_only", "不能指定目标")]
    public sealed class SelfOnlySkillCondition : SkillConditionBase
    {
        public override SkillConditionResult Check(SkillPipelineContext context)
        {
            var targetActorId = context.TargetActorId;
            if (targetActorId > 0 && targetActorId != context.CasterActorId)
            {
                return Fail("该技能不能指定目标", "cannot_target");
            }
            return Pass;
        }
    }

    /// <summary>
    /// 施法状态检查条件
    /// </summary>
    [SkillCondition("casting_state", "正在施法中")]
    public sealed class CastingStateSkillCondition : SkillConditionBase
    {
        public override SkillConditionResult Check(SkillPipelineContext context)
        {
            // 注意：这个条件的实现存在问题
            //
            // 问题分析：
            // 1. 这个条件检查 context.PipelineState == EAbilityPipelineState.Executing
            // 2. 但在 AbilityPipeline.Run 构造函数中，PipelineState 已被设置为 Executing
            // 3. 所以当执行 Checks 阶段时（第一次 Tick），PipelineState 已经是 Executing
            // 4. 导致条件检查失败，所有技能释放都会被阻止
            //
            // 正确语义应该是："是否有其他技能正在施法"
            // 但当前实现检查的是"自身 Pipeline 是否正在执行"
            // 由于 context 是当前 Pipeline 的上下文，所以这个检查永远为 Executing
            //
            // 修复方案：暂时返回 Pass，跳过这个检查
            // 正确的实现应该通过其他机制（如 SkillExecutor 的运行状态）来判断
            //
            // TODO: 重新设计这个条件，正确检查"是否有其他技能正在施法"

            return Pass;
        }
    }

    /// <summary>
    /// 禁止状态检查条件
    /// </summary>
    [SkillCondition("not_silenced", "被禁用")]
    public sealed class NotSilencedSkillCondition : SkillConditionBase
    {
        private readonly MobaActorLookupService _actors;

        public NotSilencedSkillCondition(MobaActorLookupService actors)
        {
            _actors = actors;
        }

        public override SkillConditionResult Check(SkillPipelineContext context)
        {
            // TODO: 实现沉默/禁用状态检查
            // 需要通过 Tag 系统检查是否有 "silenced" 或 "disabled" 标签
            return Pass;
        }
    }
}