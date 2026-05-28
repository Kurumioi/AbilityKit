using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Ability.World.DI;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan;

namespace AbilityKit.Demo.Moba.Systems
{
    public sealed class AddBuffSchema : IActionSchema<AddBuffArgs, IWorldResolver>
    {
        public static readonly AddBuffSchema Instance = new AddBuffSchema();

        public ActionId ActionId => TriggeringConstants.AddBuffId;

        public Type ArgsType => typeof(AddBuffArgs);

        public AddBuffArgs ParseArgs(Dictionary<string, ActionArgValue> namedArgs, ExecCtx<IWorldResolver> ctx)
        {
            int[] buffIds = null;
            int targetActorId = 0;

            if (namedArgs == null || namedArgs.Count == 0)
                return new AddBuffArgs(buffIds, targetActorId);

            var buffIdList = new List<int>();
            foreach (var kv in namedArgs)
            {
                var rawValue = kv.Value.Ref.Kind == ENumericValueRefKind.Const
                    ? kv.Value.Ref.ConstValue
                    : ActionSchemaRegistry.ResolveNumericRef(kv.Value.Ref, ctx);

                switch (kv.Key.ToLowerInvariant())
                {
                    case "buffids":
                    case "buff_ids":
                    case "buffid":
                    case "buff_id":
                    case "id":
                    case "ids":
                        var id = (int)System.Math.Round(rawValue);
                        if (id > 0) buffIdList.Add(id);
                        break;

                    case "target_actor_id":
                    case "targetactorid":
                    case "target":
                        targetActorId = (int)System.Math.Round(rawValue);
                        break;
                }
            }

            if (buffIdList.Count > 0)
                buffIds = buffIdList.ToArray();

            return new AddBuffArgs(buffIds, targetActorId);
        }

        public bool TryValidateArgs(ReadOnlySpan<KeyValuePair<string, ActionArgValue>> args, out string error)
        {
            error = null;
            foreach (var kv in args)
            {
                switch (kv.Key.ToLowerInvariant())
                {
                    case "buffids":
                    case "buff_ids":
                    case "buffid":
                    case "buff_id":
                        error = null;
                        return true;
                }
            }
            error = "add_buff is missing required parameter 'buffIds'";
            return false;
        }
    }
}
