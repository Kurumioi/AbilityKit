using AbilityKit.Ability.Triggering.Definitions;
using AbilityKit.Ability.Triggering.Runtime;

namespace AbilityKit.Demo.Moba.Triggering
{
    [TriggerActionType("apply_ongoing_effect", "添加持续效果", "行为/持续效果", 0)]
    public sealed class ApplyOngoingEffectActionFactory : IActionFactory
    {
        public ITriggerAction Create(ActionDef def)
        {
            return ApplyOngoingEffectAction.FromDef(def);
        }
    }
}
