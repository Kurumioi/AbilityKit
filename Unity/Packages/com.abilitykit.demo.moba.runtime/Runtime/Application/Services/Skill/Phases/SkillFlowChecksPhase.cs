using System;
using System.Collections.Generic;
using System.Linq;
using AbilityKit.Core.Generic;
using AbilityKit.Demo.Moba.Share.Config;
using AbilityKit.GameplayTags;
using AbilityKit.Pipeline;

namespace AbilityKit.Demo.Moba.Services
{
    using AbilityKit.Ability;
    /// <summary>
    /// 基于技能条件系统的检查阶段
    /// 支持配置多个条件，全部通过才能继续
    /// </summary>
    public sealed class SkillFlowChecksPhase : AbilityInstantPhaseBase<SkillPipelineContext>
    {
        private readonly SkillChecksPhaseDTO _def;
        private readonly SkillConditionRegistry _conditionRegistry;
        private readonly IGameplayTagService _tags;
        private readonly List<ISkillCondition> _conditions;

        public SkillFlowChecksPhase(
            AbilityPipelinePhaseId phaseId,
            SkillChecksPhaseDTO def,
            SkillConditionRegistry conditionRegistry = null,
            IGameplayTagService tags = null)
            : base(phaseId)
        {
            _def = def;
            _conditionRegistry = conditionRegistry;
            _tags = tags;
            _conditions = new List<ISkillCondition>();
            BuildConditions();
        }

        /// <summary>
        /// 从DTO配置构建条件列表
        /// </summary>
        private void BuildConditions()
        {
            _conditions.Clear();
            if (_def == null) return;

            // 1. 冷却检查
            if (_def.CheckCooldown)
            {
                if (_conditionRegistry?.TryGet("cooldown", out var cdCondition) == true)
                {
                    _conditions.Add(cdCondition);
                }
            }

            // 2. 施法状态检查
            if (_def.CheckCastingState)
            {
                if (_conditionRegistry?.TryGet("casting_state", out var castCondition) == true)
                {
                    _conditions.Add(castCondition);
                }
            }

            // RequiredTags and BlockedTags are checked directly in this phase because
            // the configured values are tag ids, not condition ids.
        }

        protected override void OnInstantExecute(SkillPipelineContext context)
        {
            if (context == null) return;

            if (!CheckRequiredTags(context)) return;
            if (!CheckBlockedTags(context)) return;

            // 空条件列表表示只进行配置型标签检查，或不进行任何检查
            if (_conditions.Count == 0) return;

            // 执行所有条件检查
            foreach (var condition in _conditions)
            {
                if (condition == null) continue;

                var result = condition.Check(context);
                if (!result.Passed)
                {
                    context.FailReason = result.FailureReason ?? $"条件不满足: {condition.DisplayName}";
                    context.IsAborted = true;
                    return;
                }
            }
        }

        private bool CheckRequiredTags(SkillPipelineContext context)
        {
            var required = _def?.RequiredTags;
            if (required == null || required.Length == 0) return true;
            if (_tags == null) return true;

            var actorId = context.CasterActorId;
            for (int i = 0; i < required.Length; i++)
            {
                var tagId = required[i];
                if (tagId <= 0) continue;

                if (!_tags.HasTag(actorId, GameplayTag.FromId(tagId)))
                {
                    context.FailReason = $"缺少必需标签: {tagId}";
                    context.IsAborted = true;
                    return false;
                }
            }

            return true;
        }

        private bool CheckBlockedTags(SkillPipelineContext context)
        {
            var blocked = _def?.BlockedTags;
            if (blocked == null || blocked.Length == 0) return true;
            if (_tags == null) return true;

            var actorId = context.CasterActorId;
            for (int i = 0; i < blocked.Length; i++)
            {
                var tagId = blocked[i];
                if (tagId <= 0) continue;

                if (_tags.HasTag(actorId, GameplayTag.FromId(tagId)))
                {
                    context.FailReason = $"存在阻塞标签: {tagId}";
                    context.IsAborted = true;
                    return false;
                }
            }

            return true;
        }
    }
}
