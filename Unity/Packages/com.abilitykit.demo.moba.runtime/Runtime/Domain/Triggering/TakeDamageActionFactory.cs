using AbilityKit.Ability.Triggering.Definitions;
using AbilityKit.Ability.Triggering.Runtime;

namespace AbilityKit.Demo.Moba.Triggering
{
    [TriggerActionType("take_damage", "受到伤害(生成伤害)", "行为/Combat", 0)]
    public sealed class TakeDamageActionFactory : IActionFactory
    {
        public ITriggerAction Create(ActionDef def)
        {
            return TakeDamageAction.FromDef(def);
        }
    }
}
