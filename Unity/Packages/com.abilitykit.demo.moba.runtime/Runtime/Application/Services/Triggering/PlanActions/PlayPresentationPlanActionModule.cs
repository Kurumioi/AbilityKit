using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Ability.Config;
using AbilityKit.Ability.World.DI;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan;
using AbilityKit.Ability.Triggering;
using AbilityKit.Ability.Triggering.Definitions;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Core.Common.Log;
using AbilityKit.Core.Common.Event;
using AbilityKit.Core.Math;
using AbilityKit.Triggering.Eventing;
using AbilityKit.Pipeline;
using PresentationEventArgs = AbilityKit.Demo.Moba.Triggering.PresentationEventArgs;
using AbilityKit.Demo.Moba.Systems;


namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    [PlanActionModule(order: 40)]
    public sealed class PlayPresentationPlanActionModule : MobaPlanActionModuleBase<PlayPresentationArgs, PlayPresentationPlanActionModule>
    {
        protected override IActionSchema<PlayPresentationArgs, IWorldResolver> Schema => PlayPresentationSchema.Instance;

        protected override void Execute(object triggerArgs, PlayPresentationArgs args, ExecCtx<IWorldResolver> ctx)
        {
            if (args.TemplateId <= 0)
            {
                Log.Warning("[Plan] play_presentation requires templateId > 0");
                return;
            }

            if (!ctx.Context.TryResolve<AbilityKit.Triggering.Eventing.IEventBus>(out var bus) || bus == null)
            {
                Log.Warning("[Plan] play_presentation cannot resolve IEventBus");
                return;
            }

            var input = MobaPlanActionInputResolver.Resolve(triggerArgs, ctx);
            var targetActorId = input.TargetActorId;
            var casterActorId = input.CasterActorId;
            var mode = (PresentationTargetMode)args.TargetMode;

            int[] targets = null;
            Vec3[] positions = null;
            long sourceContextId = input.TraceScope.EffectContextId;

            switch (mode)
            {
                case PresentationTargetMode.Self:
                    if (casterActorId > 0)
                        targets = new[] { casterActorId };
                    break;

                case PresentationTargetMode.Source:
                    if (casterActorId > 0)
                        targets = new[] { casterActorId };
                    break;

                case PresentationTargetMode.Target:
                    if (targetActorId > 0)
                        targets = new[] { targetActorId };
                    break;

                case PresentationTargetMode.Position:
                    positions = new[] { new Vec3(args.X, args.Y, args.Z) };
                    break;

                case PresentationTargetMode.PayloadTarget:
                    if (triggerArgs != null && triggerArgs is System.Collections.Generic.IDictionary<string, object> dict)
                    {
                        if (dict.TryGetValue("targetActorId", out var tidObj) && tidObj is int tid && tid > 0)
                            targets = new[] { tid };
                        if (dict.TryGetValue("sourceContextId", out var scidObj) && scidObj != null)
                        {
                            if (scidObj is long l) sourceContextId = l;
                            else if (scidObj is int i) sourceContextId = i;
                        }
                    }
                    break;
            }

            var eventName = args.Stop ? "presentation.stop" : "presentation.play";
            var eid = TriggeringIdUtil.GetEventEid(eventName);

            var payload = new PresentationEventArgs
            {
                EventId = eventName,
                TemplateId = args.TemplateId,
                RequestKey = args.RequestKey,
                DurationMsOverride = args.DurationMs,
                Targets = targets,
                Positions = positions,
                SourceContextId = sourceContextId,
                Scale = args.Scale != 1 ? args.Scale : (float?)null,
                Radius = args.Radius > 0 ? args.Radius : (float?)null,
            };

            bus.Publish(new EventKey<PresentationEventArgs>(eid), in payload);
            object boxedPayload = payload;
            bus.Publish(new EventKey<object>(eid), in boxedPayload);
        }
    }
}
