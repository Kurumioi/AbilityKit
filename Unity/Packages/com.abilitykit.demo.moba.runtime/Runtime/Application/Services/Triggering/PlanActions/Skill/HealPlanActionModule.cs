using AbilityKit.Ability.World.DI;
using AbilityKit.Demo.Moba.Systems;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan;

namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    [PlanActionModule(order: MobaPlanActionModuleOrders.Heal)]
    public sealed class HealPlanActionModule : MobaPlanActionModuleBase<HealArgs, HealPlanActionModule>
    {
        protected override IActionSchema<HealArgs, IWorldResolver> Schema => HealSchema.Instance;

        protected override void Execute(object triggerArgs, HealArgs args, ExecCtx<IWorldResolver> ctx)
        {
            if (args.Amount <= 0f) return;
            if (!ctx.Context.TryResolve<MobaDamageService>(out var damage) || damage == null)
            {
                LogRejected(ctx, "cannot resolve MobaDamageService.");
                return;
            }

            var coreInput = MobaPlanActionInputResolver.Resolve(triggerArgs, ctx);
            var effectInput = new MobaEffectActionInput(in coreInput);
            if (!effectInput.HasCasterActor)
            {
                LogRejected(ctx, "missing healer actor.");
                return;
            }

            var targets = PooledMobaPlanActionLists.GetIntList();
            try
            {
                if (!MobaActionTargetResolver.TryResolveTargets(in args.TargetRequest, in coreInput, in effectInput, ctx, TriggeringConstants.Actions.Heal, targets))
                {
                    return;
                }

                for (int i = 0; i < targets.Count; i++)
                {
                    var healed = damage.ApplyHeal(effectInput.CasterActorId, targets[i], (int)args.HealType, args.Amount, args.ReasonKind, args.ReasonParam);
                    if (healed > 0f)
                    {
                        MobaPlanActionDiagnostics.Applied(ctx.Context, TriggeringConstants.Actions.Heal, $"healer={effectInput.CasterActorId}, target={targets[i]}, amount={healed:0.###}, reasonParam={args.ReasonParam}");
                    }
                }
            }
            finally
            {
                PooledMobaPlanActionLists.Release(targets);
            }
        }
    }
}
