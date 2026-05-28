using System;
using System.Collections.Generic;
using AbilityKit.Trace;

namespace AbilityKit.Demo.Moba.Services
{
    /// <summary>
    /// MOBA 战斗溯源代码元数据
    /// 记录每次技能/效果执行的来源信息
    /// </summary>
    public sealed class MobaTraceMetadata : TraceMetadata
    {
        /// <summary>
        /// 技能配置ID（如果是技能触发的效果）
        /// </summary>
        public int SkillConfigId { get; set; }

        /// <summary>
        /// 技能实例ID
        /// </summary>
        public long SkillInstanceId { get; set; }

        /// <summary>
        /// 效果配置ID
        /// </summary>
        public int EffectConfigId { get; set; }

        /// <summary>
        /// 触发器计划ID
        /// </summary>
        public int TriggerPlanId { get; set; }

        /// <summary>
        /// 动作ID（ActionCallPlan 中的 ActionId）
        /// </summary>
        public int ActionId { get; set; }

        /// <summary>
        /// 来源角色ID（施法者）
        /// </summary>
        public int SourceActorId { get; set; }

        /// <summary>
        /// 目标角色ID
        /// </summary>
        public int TargetActorId { get; set; }

        /// <summary>
        /// 原始来源标识（技能ID、BuffID、弹道ID等）
        /// </summary>
        public string OriginSource { get; set; }

        /// <summary>
        /// 原始目标标识
        /// </summary>
        public string OriginTarget { get; set; }

        /// <summary>
        /// 效果上下文类型
        /// </summary>
        public EffectContextKind ContextKind { get; set; }

        /// <summary>
        /// 额外的描述信息
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 溯源创建时间戳（毫秒）
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// 创建便捷的格式化字符串
        /// </summary>
        public string ToDisplayString()
        {
            var parts = new List<string>();

            if (SkillConfigId > 0)
                parts.Add($"Skill={SkillConfigId}");
            if (EffectConfigId > 0)
                parts.Add($"Effect={EffectConfigId}");
            if (TriggerPlanId > 0)
                parts.Add($"Plan={TriggerPlanId}");
            if (ActionId > 0)
                parts.Add($"Action={ActionId}");
            if (SourceActorId > 0)
                parts.Add($"From={SourceActorId}");
            if (TargetActorId > 0)
                parts.Add($"To={TargetActorId}");
            if (!string.IsNullOrEmpty(Description))
                parts.Add(Description);

            return string.Join(", ", parts);
        }
    }
}
