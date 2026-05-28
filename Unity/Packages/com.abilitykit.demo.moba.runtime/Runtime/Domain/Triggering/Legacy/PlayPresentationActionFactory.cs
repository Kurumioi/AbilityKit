using AbilityKit.Ability.Triggering.Definitions;
using AbilityKit.Ability.Triggering.Runtime;

namespace AbilityKit.Demo.Moba.Triggering
{
    [TriggerActionType("play_presentation", "表现", "行为/Presentation", 0)]
    public sealed class PlayPresentationActionFactory : IActionFactory
    {
        public ITriggerAction Create(ActionDef def)
        {
            return PlayPresentationAction.FromDef(def);
        }
    }
}
