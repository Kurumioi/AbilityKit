using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Ability.World.DI;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan;

namespace AbilityKit.Demo.Moba.Systems
{
    public sealed class SpawnSummonSchema : IActionSchema<SpawnSummonArgs, IWorldResolver>
    {
        public static readonly SpawnSummonSchema Instance = new SpawnSummonSchema();

        public ActionId ActionId => TriggeringConstants.SpawnSummonId;

        public Type ArgsType => typeof(SpawnSummonArgs);

        public SpawnSummonArgs ParseArgs(Dictionary<string, ActionArgValue> namedArgs, ExecCtx<IWorldResolver> ctx)
        {
            int summonId = 0;
            int positionMode = 0;
            int rotationMode = 0;
            float intervalMs = 0;
            float durationMs = 0;
            int totalCount = 0;
            int queryTemplateId = 0;
            int targetMode = 0;

            if (namedArgs == null || namedArgs.Count == 0)
                return new SpawnSummonArgs(summonId);

            foreach (var kv in namedArgs)
            {
                var rawValue = kv.Value.Ref.Kind == ENumericValueRefKind.Const
                    ? kv.Value.Ref.ConstValue
                    : ActionSchemaRegistry.ResolveNumericRef(kv.Value.Ref, ctx);

                switch (kv.Key.ToLowerInvariant())
                {
                    case "summon_id":
                    case "summonid":
                    case "id":
                        summonId = (int)System.Math.Round(rawValue);
                        break;
                    case "position_mode":
                    case "positionmode":
                    case "position":
                        positionMode = (int)System.Math.Round(rawValue);
                        break;
                    case "rotation_mode":
                    case "rotationmode":
                    case "rotation":
                        rotationMode = (int)System.Math.Round(rawValue);
                        break;
                    case "interval_ms":
                    case "intervalms":
                        intervalMs = (float)rawValue;
                        break;
                    case "duration_ms":
                    case "durationms":
                        durationMs = (float)rawValue;
                        break;
                    case "total_count":
                    case "totalcount":
                    case "count":
                        totalCount = (int)System.Math.Round(rawValue);
                        break;
                    case "query_template_id":
                    case "querytemplateid":
                    case "query_id":
                        queryTemplateId = (int)System.Math.Round(rawValue);
                        break;
                    case "target_mode":
                    case "targetmode":
                    case "target":
                        targetMode = (int)System.Math.Round(rawValue);
                        break;
                }
            }

            var args = new SpawnSummonArgs(summonId)
            {
                PositionMode = positionMode,
                RotationMode = rotationMode,
                IntervalMs = intervalMs,
                DurationMs = durationMs,
                TotalCount = totalCount,
                QueryTemplateId = queryTemplateId,
                TargetMode = targetMode
            };

            return args;
        }

        public bool TryValidateArgs(ReadOnlySpan<KeyValuePair<string, ActionArgValue>> args, out string error)
        {
            error = null;
            foreach (var kv in args)
            {
                switch (kv.Key.ToLowerInvariant())
                {
                    case "summon_id":
                    case "summonid":
                    case "id":
                        return true;
                }
            }
            error = "spawn_summon is missing required parameter 'summon_id'";
            return false;
        }
    }
}
