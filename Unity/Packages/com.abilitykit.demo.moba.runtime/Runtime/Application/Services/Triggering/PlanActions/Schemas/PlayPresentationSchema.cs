using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Ability.World.DI;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan;
using AbilityKit.Demo.Moba.Systems;


namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    public sealed class PlayPresentationSchema : MobaPlanActionSchemaBase<PlayPresentationArgs>
    {
        public static readonly PlayPresentationSchema Instance = new PlayPresentationSchema();

        protected override string ActionName => TriggeringConstants.Actions.PlayPresentation;

        public override PlayPresentationArgs ParseArgs(Dictionary<string, ActionArgValue> namedArgs, ExecCtx<IWorldResolver> ctx)
        {
            int templateId = 0;
            int targetMode = 0;
            string requestKey = null;
            int durationMs = 0;
            bool stop = false;
            float x = 0, y = 0, z = 0;
            float scale = 1;
            float radius = 0;

            if (namedArgs == null || namedArgs.Count == 0)
                return new PlayPresentationArgs(templateId);

            foreach (var kv in namedArgs)
            {
                var rawValue = kv.Value.Ref.Kind == ENumericValueRefKind.Const
                    ? kv.Value.Ref.ConstValue
                    : ActionSchemaRegistry.ResolveNumericRef(kv.Value.Ref, ctx);

                switch (kv.Key.ToLowerInvariant())
                {
                    case "template_id":
                    case "templateid":
                    case "id":
                        templateId = (int)System.Math.Round(rawValue);
                        break;
                    case "target_mode":
                    case "targetmode":
                    case "target":
                        targetMode = (int)System.Math.Round(rawValue);
                        break;
                    case "request_key":
                    case "requestkey":
                    case "key":
                        break;
                    case "duration_ms":
                    case "durationms":
                        durationMs = (int)System.Math.Round(rawValue);
                        break;
                    case "stop":
                        stop = rawValue != 0;
                        break;
                    case "x":
                        x = (float)rawValue;
                        break;
                    case "y":
                        y = (float)rawValue;
                        break;
                    case "z":
                        z = (float)rawValue;
                        break;
                    case "scale":
                        scale = (float)rawValue;
                        break;
                    case "radius":
                        radius = (float)rawValue;
                        break;
                }
            }

            return new PlayPresentationArgs(templateId)
            {
                TargetMode = targetMode,
                RequestKey = requestKey,
                DurationMs = durationMs,
                Stop = stop,
                X = x,
                Y = y,
                Z = z,
                Scale = scale,
                Radius = radius
            };
        }

        public override bool TryValidateArgs(ReadOnlySpan<KeyValuePair<string, ActionArgValue>> args, out string error)
        {
            error = null;
            foreach (var kv in args)
            {
                switch (kv.Key.ToLowerInvariant())
                {
                    case "template_id":
                    case "templateid":
                    case "id":
                        return true;
                }
            }
            error = "play_presentation is missing required parameter 'templateId'";
            return false;
        }
    }
}
