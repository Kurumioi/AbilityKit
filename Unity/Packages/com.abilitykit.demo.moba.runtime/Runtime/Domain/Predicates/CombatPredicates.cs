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

        private static bool _notImplementedLogged;

        public override void ParseFrom(Dictionary<string, ActionArgValue> namedArgs, ExecCtx<IWorldResolver> ctx)
        {
            BuffId = AutoPredicateExtensions.ResolveInt(this, namedArgs, "buff_id", 0);
            CheckStack = AutoPredicateExtensions.ResolveInt(this, namedArgs, "check_stack", 0) > 0;
        }

        public override bool Evaluate(IBehaviorContext context)
        {
            if (!_notImplementedLogged)
            {
                _notImplementedLogged = true;
                Log.Warning($"[HasBuffPredicate] Predicate is not wired to BuffService yet; returning false. buffId={BuffId}, checkStack={CheckStack}");
            }

            return false;
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

        private static bool _notImplementedLogged;

        public override void ParseFrom(Dictionary<string, ActionArgValue> namedArgs, ExecCtx<IWorldResolver> ctx)
        {
            Threshold = AutoPredicateExtensions.ResolveFloat(this, namedArgs, "threshold", 50f);
            CompareType = AutoPredicateExtensions.ResolveInt(this, namedArgs, "compare_type", 0);
        }

        public override bool Evaluate(IBehaviorContext context)
        {
            if (!_notImplementedLogged)
            {
                _notImplementedLogged = true;
                Log.Warning($"[HealthPercentPredicate] Predicate is not wired to actor attributes yet; returning false. threshold={Threshold}, compareType={CompareType}");
            }

            return false;
        }
    }
}
