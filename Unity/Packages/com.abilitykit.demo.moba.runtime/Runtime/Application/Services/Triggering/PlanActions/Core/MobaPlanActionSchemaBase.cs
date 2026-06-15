using System;
using System.Collections.Generic;
using AbilityKit.Ability.World.DI;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan;
using AbilityKit.Triggering.Variables.Numeric;

namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    /// <summary>
    /// Demo MOBA strongly typed action schema base.
    /// New schemas only need to provide their config action name and argument parsing rules.
    /// </summary>
    public abstract class MobaPlanActionSchemaBase<TActionArgs> : ITriggerArgsAwareActionSchema<TActionArgs, IWorldResolver>
    {
        [ThreadStatic]
        private static object _currentTriggerArgs;

        protected abstract string ActionName { get; }

        public string ConfigActionName => ActionName;

        public ActionId ActionId => PlanActionRegisterUtil.GetActionId(ActionName);

        public Type ArgsType => typeof(TActionArgs);

        public abstract TActionArgs ParseArgs(Dictionary<string, ActionArgValue> namedArgs, ExecCtx<IWorldResolver> ctx);

        public TActionArgs ParseArgs(Dictionary<string, ActionArgValue> namedArgs, ExecCtx<IWorldResolver> ctx, object triggerArgs)
        {
            var previous = _currentTriggerArgs;
            _currentTriggerArgs = triggerArgs;
            try
            {
                return ParseArgs(namedArgs, ctx);
            }
            finally
            {
                _currentTriggerArgs = previous;
            }
        }

        public abstract bool TryValidateArgs(ReadOnlySpan<KeyValuePair<string, ActionArgValue>> args, out string error);

        protected static float ReadFloat(Dictionary<string, ActionArgValue> namedArgs, ExecCtx<IWorldResolver> ctx, float defaultValue, params string[] aliases)
        {
            return TryReadNumber(namedArgs, ctx, out var value, aliases) ? (float)value : defaultValue;
        }

        protected static int ReadInt(Dictionary<string, ActionArgValue> namedArgs, ExecCtx<IWorldResolver> ctx, int defaultValue, params string[] aliases)
        {
            return TryReadNumber(namedArgs, ctx, out var value, aliases) ? (int)Math.Round(value) : defaultValue;
        }

        protected static bool ReadBool(Dictionary<string, ActionArgValue> namedArgs, ExecCtx<IWorldResolver> ctx, bool defaultValue, params string[] aliases)
        {
            return TryReadNumber(namedArgs, ctx, out var value, aliases) ? value >= 0.5 : defaultValue;
        }

        protected static bool ReadBoolNonZero(Dictionary<string, ActionArgValue> namedArgs, ExecCtx<IWorldResolver> ctx, bool defaultValue, params string[] aliases)
        {
            return TryReadNumber(namedArgs, ctx, out var value, aliases) ? value != 0 : defaultValue;
        }

        protected static TEnum ReadEnum<TEnum>(Dictionary<string, ActionArgValue> namedArgs, ExecCtx<IWorldResolver> ctx, TEnum defaultValue, params string[] aliases)
            where TEnum : struct, Enum
        {
            return TryReadNumber(namedArgs, ctx, out var value, aliases) ? (TEnum)Enum.ToObject(typeof(TEnum), (int)Math.Round(value)) : defaultValue;
        }

        protected static bool TryReadNumber(Dictionary<string, ActionArgValue> namedArgs, ExecCtx<IWorldResolver> ctx, out double value, params string[] aliases)
        {
            value = default;
            if (namedArgs == null || namedArgs.Count == 0 || aliases == null || aliases.Length == 0)
                return false;

            foreach (var kv in namedArgs)
            {
                if (!IsAlias(kv.Key, aliases))
                    continue;

                value = ResolveNumber(kv.Value, ctx);
                return true;
            }

            return false;
        }

        protected static int[] ReadPositiveInts(Dictionary<string, ActionArgValue> namedArgs, ExecCtx<IWorldResolver> ctx, params string[] aliases)
        {
            if (namedArgs == null || namedArgs.Count == 0 || aliases == null || aliases.Length == 0)
                return null;

            List<int> values = null;
            foreach (var kv in namedArgs)
            {
                if (!IsAlias(kv.Key, aliases))
                    continue;

                var value = (int)Math.Round(ResolveNumber(kv.Value, ctx));
                if (value <= 0)
                    continue;

                if (values == null)
                    values = new List<int>();
                values.Add(value);
            }

            return values == null || values.Count == 0 ? null : values.ToArray();
        }

        protected static bool HasAny(ReadOnlySpan<KeyValuePair<string, ActionArgValue>> args, params string[] aliases)
        {
            if (aliases == null || aliases.Length == 0)
                return false;

            foreach (var kv in args)
            {
                if (IsAlias(kv.Key, aliases))
                    return true;
            }

            return false;
        }

        protected bool RequireAny(ReadOnlySpan<KeyValuePair<string, ActionArgValue>> args, string displayName, out string error, params string[] aliases)
        {
            if (HasAny(args, aliases))
            {
                error = null;
                return true;
            }

            error = $"{ActionName} is missing required parameter '{displayName}'";
            return false;
        }

        private static double ResolveNumber(ActionArgValue arg, ExecCtx<IWorldResolver> ctx)
        {
            if (arg.Ref.Kind == ENumericValueRefKind.Const)
            {
                return arg.Ref.ConstValue;
            }

            if (arg.Ref.Kind == ENumericValueRefKind.PayloadField
                && TryResolvePayloadField(arg.Ref.FieldId, ctx, out var payloadValue))
            {
                return payloadValue;
            }

            return ActionSchemaRegistry.ResolveNumericRef(arg.Ref, ctx);
        }

        protected static bool TryReadCurrentPayloadNumber(ExecCtx<IWorldResolver> ctx, int fieldId, out double value)
        {
            return TryResolvePayloadField(fieldId, ctx, out value);
        }

        private static bool TryResolvePayloadField(int fieldId, ExecCtx<IWorldResolver> ctx, out double value)
        {
            if (ctx.Payloads != null && _currentTriggerArgs != null)
            {
                if (ctx.Payloads.TryGetDouble(in _currentTriggerArgs, fieldId, out value))
                {
                    return true;
                }
            }

            value = default;
            return false;
        }

        private static bool IsAlias(string key, string[] aliases)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            for (int i = 0; i < aliases.Length; i++)
            {
                if (string.Equals(key, aliases[i], StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
