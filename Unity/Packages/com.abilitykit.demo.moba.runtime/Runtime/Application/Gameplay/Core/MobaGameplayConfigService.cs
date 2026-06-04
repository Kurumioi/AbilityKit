using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Core.Common.Log;
using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Demo.Moba.Config.Core;

namespace AbilityKit.Demo.Moba.Gameplay
{
    [WorldService(typeof(MobaGameplayConfigService), WorldLifetime.Scoped)]
    public sealed class MobaGameplayConfigService : IService
    {
        private const int BuiltInDefaultGameplayId = 1;

        [WorldInject(required: false)] private MobaConfigDatabase _configs;

        public int ResolveDefaultGameplayId()
        {
            return BuiltInDefaultGameplayId;
        }

        public GameplayMO GetDefaultGameplay()
        {
            return GetGameplay(ResolveDefaultGameplayId());
        }

        public GameplayMO GetGameplay(int gameplayId)
        {
            if (TryGetGameplay(gameplayId, out var gameplay))
            {
                return gameplay;
            }

            Log.Warning($"[MobaGameplayConfigService] gameplay config not found. gameplayId={gameplayId}");
            return null;
        }

        public bool TryGetGameplay(int gameplayId, out GameplayMO gameplay)
        {
            gameplay = null;
            if (gameplayId <= 0 || _configs == null)
            {
                return false;
            }

            return _configs.TryGetGameplay(gameplayId, out gameplay);
        }

        public void Dispose()
        {
        }
    }
}
