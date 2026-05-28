using AbilityKit.Ability.Triggering.Definitions;
using AbilityKit.Ability.Triggering.Runtime;

namespace AbilityKit.Demo.Moba.Triggering
{
    [TriggerActionType("give_damage", "造成伤害", "行为/Combat", 0)]
    public sealed class GiveDamageActionFactory : IActionFactory
    {
        public ITriggerAction Create(ActionDef def)
        {
            return GiveDamageAction.FromDef(def);
        }
    }
}
