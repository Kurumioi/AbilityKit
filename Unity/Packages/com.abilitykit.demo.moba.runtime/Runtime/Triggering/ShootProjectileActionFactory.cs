using AbilityKit.Ability.Triggering.Definitions;
using AbilityKit.Ability.Triggering.Runtime;
using AbilityKit.Triggering.CodeGen;

namespace AbilityKit.Demo.Moba.Triggering
{
    [TriggerActionType("shoot_projectile", "发射子弹", "行为/Projectile", 0)]
    [TriggerAction("shoot_projectile")]
    [TriggerParam(0, "launcherId")]
    [TriggerParam(1, "projectileId")]
    public sealed class ShootProjectileActionFactory : IActionFactory
    {
        public ITriggerAction Create(ActionDef def)
        {
            return ShootProjectileAction.FromDef(def);
        }
    }
}
