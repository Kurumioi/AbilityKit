using System;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Config;
using AbilityKit.Triggering.Variables.Numeric;

namespace AbilityKit.Triggering.Runtime.Plan
{
    /// <summary>
    /// 瑙﹀彂鍣ㄨ鍒掞紙涓嶅彲鍙樻暟鎹粨鏋勶級
    /// </summary>
    public readonly struct TriggerPlan<TArgs>
    {
        public readonly int Phase;
        public readonly int Priority;

        /// <summary>
        /// 瑙﹀彂鍣?ID
        /// </summary>
        public readonly int TriggerId;

        /// <summary>
        /// 浼樺厛绾ф墦鏂槇鍊笺€侲xecute 鎴愬姛鍚庤嚜鍔ㄨ皟鐢?StopBelowPriority銆?
        /// 0 = 涓嶈嚜鍔ㄦ墦鏂紱>0 = 浠ユ鍊间负闃堝€兼墦鏂洿浣庝紭鍏堢骇鐨勮Е鍙戝櫒銆?
        /// </summary>
        public readonly int InterruptPriority;

        public readonly EPredicateKind PredicateKind;
        public readonly bool HasPredicate;
        public readonly FunctionId PredicateId;

        public readonly byte PredicateArity;
        public readonly NumericValueRef PredicateArg0;
        public readonly NumericValueRef PredicateArg1;

        public readonly PredicateExprPlan PredicateExpr;

        public readonly ActionCallPlan[] Actions;

        /// <summary>
        /// 琛ㄧ幇灞?Cue锛圴FX / SFX / UI 鍙嶉锛?
        /// </summary>
        public readonly ITriggerCue Cue;

        /// <summary>
        /// 璋冨害閰嶇疆锛堟寔缁涓虹浉鍏筹級
        /// </summary>
        public readonly ScheduleModePlan Schedule;

        public readonly TriggerExecutionControlPlan ExecutionControl;

        // ========== 鏍稿績鏋勯€犲櫒锛堜繚鐣?3 涓級==========

        /// <summary>
        /// 鏃犳潯浠惰Е鍙戝櫒鏋勯€犲櫒
        /// </summary>
        public TriggerPlan(
            int phase,
            int priority,
            int triggerId = 0,
            ActionCallPlan[] actions = null,
            int interruptPriority = 0,
            ITriggerCue cue = null,
            in ScheduleModePlan schedule = default,
            in TriggerExecutionControlPlan executionControl = default)
        {
            Phase = phase;
            Priority = priority;
            TriggerId = triggerId;
            InterruptPriority = interruptPriority;
            PredicateKind = EPredicateKind.None;
            HasPredicate = false;
            PredicateId = default;
            PredicateArity = 0;
            PredicateArg0 = default;
            PredicateArg1 = default;
            PredicateExpr = default;
            Actions = actions ?? Array.Empty<ActionCallPlan>();
            Cue = cue ?? NullTriggerCue.Instance;
            Schedule = schedule;
            ExecutionControl = executionControl;
        }

        /// <summary>
        /// 鍑芥暟鏉′欢瑙﹀彂鍣ㄦ瀯閫犲櫒
        /// </summary>
        public TriggerPlan(
            int phase,
            int priority,
            int triggerId,
            FunctionId predicateId,
            NumericValueRef[] predicateArgs,
            ActionCallPlan[] actions = null,
            int interruptPriority = 0,
            ITriggerCue cue = null,
            in ScheduleModePlan schedule = default,
            in TriggerExecutionControlPlan executionControl = default)
        {
            Phase = phase;
            Priority = priority;
            TriggerId = triggerId;
            InterruptPriority = interruptPriority;
            PredicateKind = EPredicateKind.Function;
            HasPredicate = true;
            PredicateId = predicateId;
            PredicateArity = (byte)(predicateArgs?.Length ?? 0);
            PredicateArg0 = predicateArgs?.Length > 0 ? predicateArgs[0] : default;
            PredicateArg1 = predicateArgs?.Length > 1 ? predicateArgs[1] : default;
            PredicateExpr = default;
            Actions = actions ?? Array.Empty<ActionCallPlan>();
            Cue = cue ?? NullTriggerCue.Instance;
            Schedule = schedule;
            ExecutionControl = executionControl;
        }

        /// <summary>
        /// 琛ㄨ揪寮忔潯浠惰Е鍙戝櫒鏋勯€犲櫒
        /// </summary>
        public TriggerPlan(
            int phase,
            int priority,
            int triggerId,
            PredicateExprPlan predicateExpr,
            ActionCallPlan[] actions = null,
            int interruptPriority = 0,
            ITriggerCue cue = null,
            in ScheduleModePlan schedule = default,
            in TriggerExecutionControlPlan executionControl = default)
        {
            Phase = phase;
            Priority = priority;
            TriggerId = triggerId;
            InterruptPriority = interruptPriority;
            PredicateKind = EPredicateKind.Expr;
            HasPredicate = predicateExpr.Nodes != null && predicateExpr.Nodes.Length > 0;
            PredicateId = default;
            PredicateArity = 0;
            PredicateArg0 = default;
            PredicateArg1 = default;
            PredicateExpr = predicateExpr;
            Actions = actions ?? Array.Empty<ActionCallPlan>();
            Cue = cue ?? NullTriggerCue.Instance;
            Schedule = schedule;
            ExecutionControl = executionControl;
        }

        // ========== 渚挎嵎宸ュ巶鏂规硶==========

        /// <summary>
        /// 鍒涘缓鏃犳潯浠惰Е鍙戝櫒
        /// </summary>
        public static TriggerPlan<TArgs> Create(
            int phase = 0,
            int priority = 0,
            int interruptPriority = 0,
            params ActionCallPlan[] actions)
        {
            return new TriggerPlan<TArgs>(phase, priority, 0, actions, interruptPriority);
        }

        /// <summary>
        /// 鍒涘缓甯﹀嚱鏁版潯浠剁殑瑙﹀彂鍣紙鏃犲弬鏁帮級
        /// </summary>
        public static TriggerPlan<TArgs> When(
            int phase,
            int priority,
            FunctionId predicateId,
            int interruptPriority = 0,
            params ActionCallPlan[] actions)
        {
            return new TriggerPlan<TArgs>(phase, priority, 0, predicateId, null, actions, interruptPriority, null, default);
        }

        /// <summary>
        /// 鍒涘缓甯﹀嚱鏁版潯浠剁殑瑙﹀彂鍣紙甯﹀弬鏁帮級
        /// </summary>
        public static TriggerPlan<TArgs> When(
            int phase,
            int priority,
            FunctionId predicateId,
            NumericValueRef[] predicateArgs,
            int interruptPriority = 0,
            params ActionCallPlan[] actions)
        {
            return new TriggerPlan<TArgs>(phase, priority, 0, predicateId, predicateArgs, actions, interruptPriority, null, default);
        }

        /// <summary>
        /// 鍒涘缓甯﹁〃杈惧紡鏉′欢鐨勮Е鍙戝櫒
        /// </summary>
        public static TriggerPlan<TArgs> WhenExpr(
            int phase,
            int priority,
            PredicateExprPlan predicateExpr,
            int interruptPriority = 0,
            params ActionCallPlan[] actions)
        {
            return new TriggerPlan<TArgs>(phase, priority, 0, predicateExpr, actions, interruptPriority, null, default);
        }

        /// <summary>
        /// 娣诲姞鍔ㄤ綔锛岃繑鍥炴柊鐨?TriggerPlan
        /// </summary>
        public TriggerPlan<TArgs> AddActions(params ActionCallPlan[] actions)
        {
            var newActions = new ActionCallPlan[(Actions?.Length ?? 0) + actions.Length];
            if (Actions?.Length > 0)
                Array.Copy(Actions, newActions, Actions.Length);
            Array.Copy(actions, 0, newActions, Actions?.Length ?? 0, actions.Length);
            return new TriggerPlan<TArgs>(Phase, Priority, TriggerId, InterruptPriority, PredicateKind, HasPredicate, PredicateId,
                PredicateArity, PredicateArg0, PredicateArg1, PredicateExpr, newActions, Cue, in Schedule, in ExecutionControl);
        }

        public TriggerPlan<TNextArgs> AsArgs<TNextArgs>()
        {
            return new TriggerPlan<TNextArgs>(Phase, Priority, TriggerId, InterruptPriority, PredicateKind, HasPredicate, PredicateId,
                PredicateArity, PredicateArg0, PredicateArg1, PredicateExpr, Actions, Cue, in Schedule, in ExecutionControl);
        }

        internal TriggerPlan(
            int phase, int priority, int triggerId, int interruptPriority,
            EPredicateKind predicateKind, bool hasPredicate, FunctionId predicateId,
            byte predicateArity, NumericValueRef predicateArg0, NumericValueRef predicateArg1,
            PredicateExprPlan predicateExpr,
            ActionCallPlan[] actions, ITriggerCue cue, in ScheduleModePlan schedule, in TriggerExecutionControlPlan executionControl = default)
        {
            Phase = phase;
            Priority = priority;
            TriggerId = triggerId;
            InterruptPriority = interruptPriority;
            PredicateKind = predicateKind;
            HasPredicate = hasPredicate;
            PredicateId = predicateId;
            PredicateArity = predicateArity;
            PredicateArg0 = predicateArg0;
            PredicateArg1 = predicateArg1;
            PredicateExpr = predicateExpr;
            Actions = actions;
            Cue = cue;
            Schedule = schedule;
            ExecutionControl = executionControl;
        }

        private TriggerPlan(
            int phase, int priority, int interruptPriority,
            EPredicateKind predicateKind, bool hasPredicate, FunctionId predicateId,
            byte predicateArity, NumericValueRef predicateArg0, NumericValueRef predicateArg1,
            PredicateExprPlan predicateExpr,
            ActionCallPlan[] actions, ITriggerCue cue, in ScheduleModePlan schedule, in TriggerExecutionControlPlan executionControl = default)
        {
            Phase = phase;
            Priority = priority;
            TriggerId = 0;
            InterruptPriority = interruptPriority;
            PredicateKind = predicateKind;
            HasPredicate = hasPredicate;
            PredicateId = predicateId;
            PredicateArity = predicateArity;
            PredicateArg0 = predicateArg0;
            PredicateArg1 = predicateArg1;
            PredicateExpr = predicateExpr;
            Actions = actions;
            Cue = cue;
            Schedule = schedule;
            ExecutionControl = executionControl;
        }
    }
}
