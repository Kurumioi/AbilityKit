using AbilityKit.Ability.Triggering.Definitions;
using AbilityKit.Ability.Triggering.Runtime;

namespace AbilityKit.Demo.Moba.Triggering
{
    [TriggerActionType("spawn_area", "生成范围", "行为/Area", 0)]
    public sealed class SpawnAreaActionFactory : IActionFactory
    {
        public ITriggerAction Create(ActionDef def)
        {
            return SpawnAreaAction.FromDef(def);
        }
    }
}
