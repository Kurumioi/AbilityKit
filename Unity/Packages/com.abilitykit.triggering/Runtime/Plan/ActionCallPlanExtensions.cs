using System.Collections.Generic;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime.Config;
using AbilityKit.Triggering.Variables.Numeric;

namespace AbilityKit.Triggering.Runtime.Plan
{
    /// <summary>
    /// ActionCallPlan 鐨勬墿灞曟柟娉?
    /// </summary>
    public static class ActionCallPlanExtensions
    {
        /// <summary>
        /// 鍒涘缓鏃犲弬鏁扮殑鍔ㄤ綔璋冪敤锛堥粯璁?Immediate锛?
        /// </summary>
        public static ActionCallPlan Call(ActionId id)
        {
            return new ActionCallPlan(id);
        }

        /// <summary>
        /// 鍒涘缓甯︿竴涓弬鏁扮殑鍔ㄤ綔璋冪敤锛堥粯璁?Immediate锛?
        /// </summary>
        public static ActionCallPlan Call(this ActionId id, NumericValueRef arg0)
        {
            return new ActionCallPlan(id, arg0);
        }

        /// <summary>
        /// 鍒涘缓甯︿袱涓弬鏁扮殑鍔ㄤ綔璋冪敤锛堥粯璁?Immediate锛?
        /// </summary>
        public static ActionCallPlan Call(this ActionId id, NumericValueRef arg0, NumericValueRef arg1)
        {
            return new ActionCallPlan(id, arg0, arg1);
        }

        /// <summary>
        /// 鍒涘缓甯︽湁鍏峰悕鍙傛暟鐨勫姩浣滆皟鐢紙榛樿 Immediate锛?
        /// </summary>
        public static ActionCallPlan CallArgs(this ActionId id, Dictionary<string, ActionArgValue> args)
        {
            return ActionCallPlan.WithArgs(id, args);
        }

        /// <summary>
        /// 鍒涘缓甯︽湁涓や釜鍏峰悕鍙傛暟鐨勫姩浣滆皟鐢紙榛樿 Immediate锛?
        /// </summary>
        public static ActionCallPlan CallArgs(this ActionId id, string name0, double value0, string name1, double value1)
        {
            return ActionCallPlan.WithArgs(id, new Dictionary<string, ActionArgValue>
            {
                [name0] = ActionArgValue.OfConst(value0, name0),
                [name1] = ActionArgValue.OfConst(value1, name1)
            });
        }

        /// <summary>
        /// 鍒涘缓甯︽湁涓変釜鍏峰悕鍙傛暟鐨勫姩浣滆皟鐢紙榛樿 Immediate锛?
        /// </summary>
        public static ActionCallPlan CallArgs(this ActionId id, string name0, double value0, string name1, double value1, string name2, double value2)
        {
            return ActionCallPlan.WithArgs(id, new Dictionary<string, ActionArgValue>
            {
                [name0] = ActionArgValue.OfConst(value0, name0),
                [name1] = ActionArgValue.OfConst(value1, name1),
                [name2] = ActionArgValue.OfConst(value2, name2)
            });
        }

        /// <summary>
        /// 鍒涘缓甯﹀叿鍚嶅弬鏁扮殑鍔ㄤ綔璋冪敤锛堟墿灞曟柟娉曠増鏈級
        /// </summary>
        public static ActionCallPlan WithArgs(this ActionId id, Dictionary<string, ActionArgValue> args)
        {
            return ActionCallPlan.WithArgs(id, args);
        }

        /// <summary>
        /// 鍒涘缓甯︿袱涓叿鍚嶅弬鏁扮殑鍔ㄤ綔璋冪敤锛堟墿灞曟柟娉曠増鏈級
        /// </summary>
        public static ActionCallPlan WithArgs(this ActionId id, string name0, double value0, string name1, double value1)
        {
            return ActionCallPlan.WithArgs(id, new Dictionary<string, ActionArgValue>
            {
                [name0] = ActionArgValue.OfConst(value0, name0),
                [name1] = ActionArgValue.OfConst(value1, name1)
            });
        }

        /// <summary>
        /// 鍒涘缓甯︿笁涓叿鍚嶅弬鏁扮殑鍔ㄤ綔璋冪敤锛堟墿灞曟柟娉曠増鏈級
        /// </summary>
        public static ActionCallPlan WithArgs(this ActionId id, string name0, double value0, string name1, double value1, string name2, double value2)
        {
            return ActionCallPlan.WithArgs(id, new Dictionary<string, ActionArgValue>
            {
                [name0] = ActionArgValue.OfConst(value0, name0),
                [name1] = ActionArgValue.OfConst(value1, name1),
                [name2] = ActionArgValue.OfConst(value2, name2)
            });
        }

        // ========== 璋冨害妯″紡宸ュ巶鏂规硶 ==========

        /// <summary>
        /// 鍒涘缓绔嬪嵆鎵ц鐨勫姩浣?
        /// </summary>
        public static ActionCallPlan Immediate(this ActionId id)
        {
            var plan = new ActionCallPlan(id);
            return new ActionCallPlan(
                plan.Id, plan.Arity, plan.Arg0, plan.Arg1, plan.Args,
                Config.EActionScheduleMode.Immediate, 0, -1, true,
                Config.EActionExecutionPolicy.Immediate);
        }

        /// <summary>
        /// 鍒涘缓寤惰繜鎵ц鐨勫姩浣?
        /// </summary>
        /// <param name="delayMs">寤惰繜鏃堕棿锛堟绉掞級</param>
        /// <param name="maxExecutions">鏈€澶ф墽琛屾鏁帮紝-1=鏃犻檺</param>
        public static ActionCallPlan Delayed(this ActionId id, float delayMs, int maxExecutions = 1)
        {
            var plan = new ActionCallPlan(id);
            return new ActionCallPlan(
                plan.Id, plan.Arity, plan.Arg0, plan.Arg1, plan.Args,
                Config.EActionScheduleMode.Delayed, delayMs, maxExecutions, true,
                Config.EActionExecutionPolicy.Immediate);
        }

        /// <summary>
        /// 鍒涘缓鍛ㄦ湡鎵ц鐨勫姩浣?
        /// </summary>
        /// <param name="intervalMs">鍛ㄦ湡闂撮殧锛堟绉掞級</param>
        /// <param name="maxExecutions">鏈€澶ф墽琛屾鏁帮紝-1=鏃犻檺</param>
        /// <param name="canBeInterrupted">鏄惁鍙腑鏂?/param>
        public static ActionCallPlan Periodic(this ActionId id, float intervalMs, int maxExecutions = -1, bool canBeInterrupted = true)
        {
            var plan = new ActionCallPlan(id);
            return new ActionCallPlan(
                plan.Id, plan.Arity, plan.Arg0, plan.Arg1, plan.Args,
                Config.EActionScheduleMode.Periodic, intervalMs, maxExecutions, canBeInterrupted,
                Config.EActionExecutionPolicy.Immediate);
        }

        /// <summary>
        /// 鍒涘缓鎸佺画璋冨害鎵ц鐨勫姩浣滐紙鎸夐棿闅旀墽琛岋紝鐩村埌澶栭儴涓柇鎴栬揪鍒版墽琛屾鏁帮級
        /// </summary>
        /// <param name="canBeInterrupted">鏄惁鍙腑鏂?/param>
        public static ActionCallPlan Continuous(this ActionId id, bool canBeInterrupted = true)
        {
            var plan = new ActionCallPlan(id);
            return new ActionCallPlan(
                plan.Id, plan.Arity, plan.Arg0, plan.Arg1, plan.Args,
                Config.EActionScheduleMode.Continuous, 0, -1, canBeInterrupted,
                Config.EActionExecutionPolicy.Immediate);
        }

        /// <summary>
        /// 鍒涘缓甯︽墽琛岀瓥鐣ョ殑鍔ㄤ綔
        /// </summary>
        public static ActionCallPlan WithExecutionPolicy(this ActionCallPlan plan, Config.EActionExecutionPolicy policy)
        {
            return new ActionCallPlan(
                plan.Id, plan.Arity, plan.Arg0, plan.Arg1, plan.Args,
                plan.ScheduleMode, plan.ScheduleParam, plan.MaxExecutions, plan.CanBeInterrupted,
                policy, plan.RetryMaxRetries, plan.RetryDelayMs);
        }

        /// <summary>
        /// 鍒涘缓甯﹂噸璇曠瓥鐣ョ殑鍔ㄤ綔銆?
        /// </summary>
        public static ActionCallPlan WithRetry(this ActionCallPlan plan, int maxRetries = 3, float retryDelayMs = 0f)
        {
            return new ActionCallPlan(
                plan.Id, plan.Arity, plan.Arg0, plan.Arg1, plan.Args,
                plan.ScheduleMode, plan.ScheduleParam, plan.MaxExecutions, plan.CanBeInterrupted,
                Config.EActionExecutionPolicy.WithRetry, maxRetries, retryDelayMs);
        }

        /// <summary>
        /// 鍒涘缓甯﹁皟搴﹀弬鏁扮殑鍔ㄤ綔
        /// </summary>
        public static ActionCallPlan WithSchedule(this ActionCallPlan plan, Config.EActionScheduleMode mode, float param = 0, int maxExecutions = -1, bool canBeInterrupted = true)
        {
            return new ActionCallPlan(
                plan.Id, plan.Arity, plan.Arg0, plan.Arg1, plan.Args,
                mode, param, maxExecutions, canBeInterrupted,
                plan.ExecutionPolicy, plan.RetryMaxRetries, plan.RetryDelayMs);
        }
    }
}
