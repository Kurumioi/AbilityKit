using AbilityKit.Ability.Triggering.Definitions;
using AbilityKit.Ability.Triggering.Runtime;

namespace AbilityKit.Demo.Moba.Triggering
{
    [TriggerActionType("knock", "击退", "行为/Combat", 0)]
    public sealed class KnockActionFactory : IActionFactory
    {
        public ITriggerAction Create(ActionDef def)
        {
            return KnockAction.FromDef(def);
        }
    }
}
