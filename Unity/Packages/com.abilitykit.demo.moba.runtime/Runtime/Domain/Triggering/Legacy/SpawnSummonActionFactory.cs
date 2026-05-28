using AbilityKit.Ability.Triggering.Definitions;
using AbilityKit.Ability.Triggering.Runtime;

namespace AbilityKit.Demo.Moba.Triggering
{
    [TriggerActionType("spawn_summon", "生成召唤物", "行为/Combat", 0)]
    public sealed class SpawnSummonActionFactory : IActionFactory
    {
        public ITriggerAction Create(ActionDef def)
        {
            return SpawnSummonAction.FromDef(def);
        }
    }
}
