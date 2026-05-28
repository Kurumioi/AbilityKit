using AbilityKit.Ability.Triggering.Definitions;
using AbilityKit.Ability.Triggering.Runtime;

namespace AbilityKit.Demo.Moba.Triggering
{
    [TriggerActionType("aoe_burst", "范围爆发(重置目标)", "行为/Area", 0)]
    public sealed class AoeBurstActionFactory : IActionFactory
    {
        public ITriggerAction Create(ActionDef def)
        {
            return AoeBurstAction.FromDef(def);
        }
    }
}
