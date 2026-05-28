using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;

namespace AbilityKit.Demo.Moba.Services
{
    [WorldService(typeof(MobaGamePhaseService))]
    public sealed class MobaGamePhaseService : IService
    {
        public bool InGame { get; private set; }

        public void SetInGame()
        {
            InGame = true;
        }

        public void Reset()
        {
            InGame = false;
        }

        public void Dispose()
        {
        }
    }
}
