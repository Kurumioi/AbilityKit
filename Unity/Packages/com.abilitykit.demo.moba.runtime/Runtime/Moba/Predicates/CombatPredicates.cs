using System.Collections.Generic;
using AbilityKit.Ability.World.DI;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan;
using AbilityKit.Triggering.Runtime.Behavior;
using AbilityKit.Triggering.Runtime.Behavior.Predicates;
using AbilityKit.Core.Common.Log;

namespace AbilityKit.Demo.Moba.Predicates
{
    /// <summary>
    /// 检查目标是否有指定 BUFF 的条件
    /// </summary>
    public sealed class HasBuffPredicate : AutoPredicate
    {
        /// <summary>
        /// BUFF ID
        /// </summary>
        public int BuffId { get; private set; }

        /// <summary>
        /// 是否检查层数大于 0
        /// </summary>
        public bool CheckStack { get; private set; }

        protected override string PredicateType => "has_buff";
        protected override int Order => 10;

        public override void ParseFrom(Dictionary<string, ActionArgValue> namedArgs, ExecCtx<IWorldResolver> ctx)
        {
            BuffId = AutoPredicateExtensions.ResolveInt(this, namedArgs, "buff_id", 0);
            CheckStack = AutoPredicateExtensions.ResolveInt(this, namedArgs, "check_stack", 0) > 0;
        }

        public override bool Evaluate(IBehaviorContext context)
        {
            // TODO: 根据实际上下文结构获取 targetActorId
            int targetActorId = 0;

            Log.Info($"[HasBuffPredicate] Checking buff {BuffId} on actor {targetActorId}");

            // TODO: 调用实际的 BuffService 进行检查
            // 示例：return buffService.HasBuff(targetActorId, BuffId);
            return true;
        }
    }

    /// <summary>
    /// 检查目标生命值百分比的条件
    /// </summary>
    public sealed class HealthPercentPredicate : AutoPredicate
    {
        /// <summary>
        /// 生命值百分比阈值
        /// </summary>
        public float Threshold { get; private set; }

        /// <summary>
        /// 比较类型: 0=小于, 1=大于
        /// </summary>
        public int CompareType { get; private set; }

        protected override string PredicateType => "health_percent";
        protected override int Order => 10;

        public override void ParseFrom(Dictionary<string, ActionArgValue> namedArgs, ExecCtx<IWorldResolver> ctx)
        {
            Threshold = AutoPredicateExtensions.ResolveFloat(this, namedArgs, "threshold", 50f);
            CompareType = AutoPredicateExtensions.ResolveInt(this, namedArgs, "compare_type", 0);
        }

        public override bool Evaluate(IBehaviorContext context)
        {
            // TODO: 根据实际上下文结构获取生命值
            var currentHp = 500f;
            var maxHp = 1000f;

            var percent = (currentHp / maxHp) * 100f;

            // CompareType: 0=小于, 1=大于
            return CompareType == 0 ? percent < Threshold : percent > Threshold;
        }
    }
}
