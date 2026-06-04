using AbilityKit.Ability.World.DI;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime.Plan;

namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    /// <summary>
    /// Demo MOBA strongly typed action module base.
    /// The schema is the single source for ActionId so modules do not duplicate action constants.
    /// </summary>
    public abstract class MobaPlanActionModuleBase<TActionArgs, TModule> : NamedArgsPlanActionModuleBase<TActionArgs, IWorldResolver, TModule>
        where TModule : MobaPlanActionModuleBase<TActionArgs, TModule>
    {
        protected sealed override ActionId ActionId => Schema.ActionId;
    }
}
