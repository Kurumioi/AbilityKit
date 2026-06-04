using AbilityKit.Ability.World.DI;
using AbilityKit.Core.Common.Log;
using AbilityKit.Demo.Moba.Gameplay;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan;

namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    [PlanActionModule(order: 0)]
    public sealed class SetGameplayVarPlanActionModule : MobaPlanActionModuleBase<SetGameplayVarArgs, SetGameplayVarPlanActionModule>
    {
        protected override IActionSchema<SetGameplayVarArgs, IWorldResolver> Schema => SetGameplayVarSchema.Instance;

        protected override void Execute(object triggerArgs, SetGameplayVarArgs args, ExecCtx<IWorldResolver> ctx)
        {
            if (ctx.Context == null || args.KeyId == 0)
            {
                return;
            }

            if (!ctx.Context.TryResolve<MobaGameplayVariableService>(out var variables) || variables == null)
            {
                Log.Warning("[Plan] set_gameplay_var skipped: gameplay variable service not found");
                return;
            }

            variables.Set(args.KeyId, args.Value);
        }
    }
}
