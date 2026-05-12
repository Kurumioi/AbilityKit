using AbilityKit.Ability.Triggering.Definitions;
using AbilityKit.Ability.Triggering.Runtime;

namespace AbilityKit.Demo.Moba.Triggering
{
    [TriggerActionType("add_buff", "娣诲姞Buff", "琛屼负/Buff", 0)]
    public sealed class AddBuffActionFactory : IActionFactory
    {
        public ITriggerAction Create(ActionDef def)
        {
            return AddBuffAction.FromDef(def);
        }
    }
}
