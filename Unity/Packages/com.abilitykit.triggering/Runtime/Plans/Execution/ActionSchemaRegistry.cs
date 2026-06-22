using System;
using System.Collections.Generic;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime.Config;
using AbilityKit.Triggering.Runtime.Executable;
using AbilityKit.Triggering.Variables.Numeric;

namespace AbilityKit.Triggering.Runtime.Plan
{
    public partial class ActionSchemaRegistry
    {
        private static readonly Dictionary<ActionId, IActionSchema> Schemas = new Dictionary<ActionId, IActionSchema>();

        private sealed class GenericSchemaAdapter<TActionArgs, TCtx> : IActionSchema
        {
            private readonly IActionSchema<TActionArgs, TCtx> _inner;

            public GenericSchemaAdapter(IActionSchema<TActionArgs, TCtx> inner)
            {
                _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            }

            public ActionId ActionId => _inner.ActionId;
            public Type ArgsType => typeof(TActionArgs);
            public bool IsDeterministic => true;

            public object ParseArgs(Dictionary<string, ActionArgValue> namedArgs, object ctx)
            {
                return namedArgs;
            }

            public bool TryValidateArgs(ReadOnlySpan<KeyValuePair<string, ActionArgValue>> args, out string error)
            {
                return _inner.TryValidateArgs(args, out error);
            }
        }

        public static bool TryGet(ActionId actionId, out IActionSchema schema)
        {
            return Schemas.TryGetValue(actionId, out schema);
        }

        public static void Register<TActionArgs, TCtx>(IActionSchema<TActionArgs, TCtx> schema)
        {
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));

            Schemas[schema.ActionId] = new GenericSchemaAdapter<TActionArgs, TCtx>(schema);
        }

        public static void Register(ActionId actionId, IActionSchema schema)
        {
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));

            Schemas[actionId] = schema;
        }

        public static object ParseArgs(Dictionary<string, ActionArgValue> namedArgs, object ctx)
        {
            return namedArgs;
        }

        public static object ParseArgs(ActionId actionId, Dictionary<string, ActionArgValue> namedArgs, object ctx)
        {
            return ParseArgs(namedArgs, ctx);
        }

        public static object ParseArgs<TActionArgs, TCtx>(Dictionary<string, ActionArgValue> namedArgs, ExecCtx<TCtx> ctx)
        {
            return namedArgs;
        }

        public static object ParseArgs<TActionArgs, TCtx>(ActionId actionId, Dictionary<string, ActionArgValue> namedArgs, ExecCtx<TCtx> ctx)
        {
            return ParseArgs<TActionArgs, TCtx>(namedArgs, ctx);
        }

        public static object GetParsedArgs(ActionId actionId, Dictionary<string, ActionArgValue> namedArgs, object ctx)
        {
            return namedArgs;
        }

        public static object GetParsedArgs<TActionArgs, TCtx>(ActionId actionId, Dictionary<string, ActionArgValue> namedArgs, ExecCtx<TCtx> ctx)
        {
            return namedArgs;
        }

        public static bool TryResolveNumericRef<TArgs, TCtx>(in NumericValueRef valueRef, in TArgs args, in ExecCtx<TCtx> ctx, out double value)
        {
            value = default;
            return false;
        }

        public static bool TryResolveNumericRef(in NumericValueRef valueRef, object args, object ctx, out double value)
        {
            value = default;
            return false;
        }
    }
}
