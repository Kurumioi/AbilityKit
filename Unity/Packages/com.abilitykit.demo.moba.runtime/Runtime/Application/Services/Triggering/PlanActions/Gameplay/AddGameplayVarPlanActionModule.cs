using AbilityKit.Ability.World.DI;
using AbilityKit.Core.Common.Log;
using AbilityKit.Demo.Moba.Gameplay;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan;

namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    [PlanActionModule(order: 0)]
    public sealed class AddGameplayVarPlanActionModule : MobaPlanActionModuleBase<AddGameplayVarArgs, AddGameplayVarPlanActionModule>
    {
        protected override IActionSchema<AddGameplayVarArgs, IWorldResolver> Schema => AddGameplayVarSchema.Instance;

        protected override void Execute(object triggerArgs, AddGameplayVarArgs args, ExecCtx<IWorldResolver> ctx)
        {
            if (ctx.Context == null || args.KeyId == 0)
            {
                return;
            }

            if (!ctx.Context.TryResolve<MobaGameplayVariableService>(out var variables) || variables == null)
            {
                Log.Warning("[Plan] add_gameplay_var skipped: gameplay variable service not found");
                return;
            }

            variables.Add(args.KeyId, args.Delta);
        }
    }
}
