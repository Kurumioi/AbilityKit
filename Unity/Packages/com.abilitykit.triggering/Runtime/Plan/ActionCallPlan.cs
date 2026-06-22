using System;
using System.Collections.Generic;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime.Config;
using AbilityKit.Triggering.Variables.Numeric;

namespace AbilityKit.Triggering.Runtime.Plan
{
    public readonly struct ActionArgumentsPlan
    {
        public readonly byte Arity;
        public readonly NumericValueRef Arg0;
        public readonly NumericValueRef Arg1;
        public readonly Dictionary<string, ActionArgValue> NamedArgs;

        public bool HasNamedArgs => NamedArgs != null && NamedArgs.Count > 0;

        public ActionArgumentsPlan(byte arity, NumericValueRef arg0, NumericValueRef arg1, Dictionary<string, ActionArgValue> namedArgs)
        {
            Arity = arity;
            Arg0 = arg0;
            Arg1 = arg1;
            NamedArgs = namedArgs;
        }
    }

    public readonly struct ActionSchedulePlan
    {
        public readonly Config.EActionScheduleMode Mode;
        public readonly float Param;
        public readonly int MaxExecutions;
        public readonly bool CanBeInterrupted;

        public ActionSchedulePlan(Config.EActionScheduleMode mode, float param, int maxExecutions, bool canBeInterrupted)
        {
            Mode = mode;
            Param = param;
            MaxExecutions = maxExecutions;
            CanBeInterrupted = canBeInterrupted;
        }
    }

    public readonly struct ActionExecutionPlan
    {
        public readonly Config.EActionExecutionPolicy Policy;
        public readonly int RetryMaxRetries;
        public readonly float RetryDelayMs;

        public ActionExecutionPlan(Config.EActionExecutionPolicy policy, int retryMaxRetries = 3, float retryDelayMs = 0f)
        {
            Policy = policy;
            RetryMaxRetries = retryMaxRetries;
            RetryDelayMs = retryDelayMs;
        }
    }

    /// <summary>
    /// Action 璋冪敤璁″垝锛堝弬鏁板寲鍔ㄤ綔鎻忚堪锛?
    /// </summary>
    public readonly struct ActionCallPlan
    {
        public readonly ActionId Id;
        public readonly byte Arity;
        public readonly NumericValueRef Arg0;
        public readonly NumericValueRef Arg1;

        /// <summary>
        /// 鍏峰悕鍙傛暟瀛楀吀锛坘ey=鍙傛暟鍚嶏紝value=鍙傛暟鍊煎紩鐢級
        /// 涓?null 鏃惰〃绀哄悜鍚庡吋瀹圭殑浣嶇疆鍙傛暟妯″紡锛堜娇鐢?Arg0/Arg1锛?
        /// </summary>
        public readonly Dictionary<string, ActionArgValue> Args;

        /// <summary>
        /// 璋冨害妯″紡锛圓ction 鑷韩濡備綍杩愯锛?
        /// Immediate: 绔嬪嵆鎵ц涓€娆?
        /// Delayed: 寤惰繜鎵ц锛堢瓑寰?ScheduleParam 姣锛?
        /// Periodic: 鍛ㄦ湡鎵ц锛堟瘡 ScheduleParam 姣锛?
        /// Continuous: 鎸佺画璋冨害鎵ц锛堟寜 ScheduleParam 闂撮殧锛岀洿鍒板閮ㄤ腑鏂垨杈惧埌鎵ц娆℃暟锛?
        /// Timeline: 鏃堕棿绾挎墽琛岋紙鎸夋椂闂磋酱搴忓垪锛?
        /// </summary>
        public readonly Config.EActionScheduleMode ScheduleMode;

        /// <summary>
        /// 璋冨害鍙傛暟
        /// Delayed: 寤惰繜鏃堕棿锛堟绉掞級
        /// Periodic: 鍛ㄦ湡闂撮殧锛堟绉掞級
        /// Timeline: 鏃堕棿绾挎€绘椂闀匡紙姣锛?
        /// </summary>
        public readonly float ScheduleParam;

        /// <summary>
        /// 鏈€澶ф墽琛屾鏁帮紙-1=鏃犻檺锛屼粎瀵?Periodic/Delayed 鏈夋晥锛?
        /// </summary>
        public readonly int MaxExecutions;

        /// <summary>
        /// 鏄惁鍙涓柇锛堟寔缁涓烘湁鏁堬級
        /// </summary>
        public readonly bool CanBeInterrupted;

        /// <summary>
        /// 鎵ц绛栫暐锛堝崟娆℃墽琛岀殑绾︽潫锛?
        /// </summary>
        public readonly Config.EActionExecutionPolicy ExecutionPolicy;

        /// <summary>
        /// WithRetry 绛栫暐鐨勬渶澶ч噸璇曟鏁般€?
        /// </summary>
        public readonly int RetryMaxRetries;

        /// <summary>
        /// WithRetry 绛栫暐鐨勫崟娆￠噸璇曞欢杩燂紙姣锛夈€? 琛ㄧず鍚屽抚绔嬪嵆閲嶈瘯銆?
        /// </summary>
        public readonly float RetryDelayMs;

        /// <summary>
        /// 鍒涘缓鏃犲弬鏁扮殑鍔ㄤ綔璋冪敤锛堥粯璁?Immediate锛?
        /// </summary>
        public ActionCallPlan(ActionId id)
        {
            Id = id;
            Arity = 0;
            Arg0 = default;
            Arg1 = default;
            Args = null;
            ScheduleMode = Config.EActionScheduleMode.Immediate;
            ScheduleParam = 0;
            MaxExecutions = -1;
            CanBeInterrupted = true;
            ExecutionPolicy = Config.EActionExecutionPolicy.Immediate;
            RetryMaxRetries = 3;
            RetryDelayMs = 0f;
        }

        /// <summary>
        /// 鍒涘缓甯﹀弬鏁扮殑鍔ㄤ綔璋冪敤锛堥粯璁?Immediate锛?
        /// </summary>
        public ActionCallPlan(ActionId id, NumericValueRef arg0, NumericValueRef arg1 = default, NumericValueRef arg2 = default)
        {
            Id = id;
            if (arg2.Kind != ENumericValueRefKind.Const || arg2.ConstValue != 0 || !string.IsNullOrEmpty(arg2.Key) || !string.IsNullOrEmpty(arg2.ExprText))
            {
                Arity = 3;
                Arg0 = arg0;
                Arg1 = arg1;
                Args = new Dictionary<string, ActionArgValue> { ["_2"] = ActionArgValue.Of(arg2, "_2") };
            }
            else if (arg1.Kind != ENumericValueRefKind.Const || arg1.ConstValue != 0 || !string.IsNullOrEmpty(arg1.Key) || !string.IsNullOrEmpty(arg1.ExprText))
            {
                Arity = 2;
                Arg0 = arg0;
                Arg1 = arg1;
                Args = null;
            }
            else if (arg0.Kind != ENumericValueRefKind.Const || arg0.ConstValue != 0 || !string.IsNullOrEmpty(arg0.Key) || !string.IsNullOrEmpty(arg0.ExprText))
            {
                Arity = 1;
                Arg0 = arg0;
                Arg1 = default;
                Args = null;
            }
            else
            {
                Arity = 0;
                Arg0 = default;
                Arg1 = default;
                Args = null;
            }
            ScheduleMode = Config.EActionScheduleMode.Immediate;
            ScheduleParam = 0;
            MaxExecutions = -1;
            CanBeInterrupted = true;
            ExecutionPolicy = Config.EActionExecutionPolicy.Immediate;
            RetryMaxRetries = 3;
            RetryDelayMs = 0f;
        }

        /// <summary>
        /// 鍒涘缓甯﹀父閲忓弬鏁扮殑鍔ㄤ綔璋冪敤锛堥粯璁?Immediate锛?
        /// </summary>
        public ActionCallPlan(ActionId id, params double[] constArgs)
        {
            Id = id;
            switch (constArgs.Length)
            {
                case 0:
                    Arity = 0;
                    Arg0 = default;
                    Arg1 = default;
                    Args = null;
                    break;
                case 1:
                    Arity = 1;
                    Arg0 = NumericValueRef.Const(constArgs[0]);
                    Arg1 = default;
                    Args = null;
                    break;
                case 2:
                    Arity = 2;
                    Arg0 = NumericValueRef.Const(constArgs[0]);
                    Arg1 = NumericValueRef.Const(constArgs[1]);
                    Args = null;
                    break;
                default:
                    Arity = (byte)constArgs.Length;
                    Arg0 = NumericValueRef.Const(constArgs[0]);
                    Arg1 = NumericValueRef.Const(constArgs[1]);
                    Args = new Dictionary<string, ActionArgValue>();
                    for (int i = 2; i < constArgs.Length; i++)
                        Args[$"__{i}"] = ActionArgValue.OfConst(constArgs[i], $"__{i}");
                    break;
            }
            ScheduleMode = Config.EActionScheduleMode.Immediate;
            ScheduleParam = 0;
            MaxExecutions = -1;
            CanBeInterrupted = true;
            ExecutionPolicy = Config.EActionExecutionPolicy.Immediate;
            RetryMaxRetries = 3;
            RetryDelayMs = 0f;
        }

        /// <summary>
        /// 鍒涘缓甯︽湁鍏峰悕鍙傛暟鐨?ActionCallPlan锛堥粯璁?Immediate锛?
        /// </summary>
        public static ActionCallPlan WithArgs(ActionId id, Dictionary<string, ActionArgValue> args)
        {
            return new ActionCallPlan(id, args);
        }

        private ActionCallPlan(ActionId id, Dictionary<string, ActionArgValue> args)
        {
            Id = id;
            Arity = (byte)(args != null ? args.Count : 0);
            Arg0 = default;
            Arg1 = default;
            Args = args;
            ScheduleMode = Config.EActionScheduleMode.Immediate;
            ScheduleParam = 0;
            MaxExecutions = -1;
            CanBeInterrupted = true;
            ExecutionPolicy = Config.EActionExecutionPolicy.Immediate;
            RetryMaxRetries = 3;
            RetryDelayMs = 0f;
        }

        /// <summary>
        /// 鏄惁浣跨敤浜嗗叿鍚嶅弬鏁版ā寮?
        /// </summary>
        public bool HasNamedArgs => Arguments.HasNamedArgs;

        public ActionArgumentsPlan Arguments => new ActionArgumentsPlan(Arity, Arg0, Arg1, Args);

        public ActionSchedulePlan Schedule => new ActionSchedulePlan(ScheduleMode, ScheduleParam, MaxExecutions, CanBeInterrupted);

        public ActionExecutionPlan Execution => new ActionExecutionPlan(ExecutionPolicy, RetryMaxRetries, RetryDelayMs);

        /// <summary>
        /// 瀹屾暣鏋勯€犲嚱鏁帮紙鐢ㄤ簬鎵╁睍鏂规硶鍒涘缓淇敼鍚庣殑鍓湰锛?
        /// </summary>
        public ActionCallPlan(
            ActionId id,
            byte arity,
            NumericValueRef arg0,
            NumericValueRef arg1,
            Dictionary<string, ActionArgValue> args,
            Config.EActionScheduleMode scheduleMode,
            float scheduleParam,
            int maxExecutions,
            bool canBeInterrupted,
            Config.EActionExecutionPolicy executionPolicy,
            int retryMaxRetries = 3,
            float retryDelayMs = 0f)
        {
            if (retryMaxRetries < 0) throw new ArgumentOutOfRangeException(nameof(retryMaxRetries));
            if (retryDelayMs < 0f) throw new ArgumentOutOfRangeException(nameof(retryDelayMs));

            Id = id;
            Arity = arity;
            Arg0 = arg0;
            Arg1 = arg1;
            Args = args;
            ScheduleMode = scheduleMode;
            ScheduleParam = scheduleParam;
            MaxExecutions = maxExecutions;
            CanBeInterrupted = canBeInterrupted;
            ExecutionPolicy = executionPolicy;
            RetryMaxRetries = retryMaxRetries;
            RetryDelayMs = retryDelayMs;
        }
    }
}
