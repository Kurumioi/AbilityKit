using AbilityKit.Ability.Triggering.Definitions;
using AbilityKit.Ability.Triggering.Runtime;

namespace AbilityKit.Demo.Moba.Triggering
{
    [TriggerActionType("emit", "发射器", "行为/Emitter", 0)]
    public sealed class EmitActionFactory : IActionFactory
    {
        public ITriggerAction Create(ActionDef def)
        {
            return EmitAction.FromDef(def);
        }
    }
}
