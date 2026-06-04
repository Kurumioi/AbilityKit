using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Coordinator;

namespace AbilityKit.Demo.Moba.Services
{
    [WorldService(typeof(ILogicWorldDriveGate), WorldLifetime.Scoped)]
    [WorldService(typeof(MobaLogicWorldDriveGate), WorldLifetime.Scoped)]
    public sealed class MobaLogicWorldDriveGate : ILogicWorldDriveGate
    {
        [WorldInject(required: false)] private MobaGamePhaseService _phase;

        public bool CanDriveLogicWorld(float deltaTime)
        {
            return _phase != null && _phase.InGame;
        }
    }
}
