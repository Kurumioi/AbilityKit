using System;
using AbilityKit.Ability.Host;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Protocol.Moba;

namespace ET.Logic
{
    /// <summary>
    /// Coordinates moba.core game start, ET presentation unit creation, and enter-game snapshot dispatch.
    /// </summary>
    public static class ETBattleEnterGameCoordinator
    {
        public static void Trigger(ETMobaBattleDriver driver)
        {
            try
            {
                var scene = driver.Scene();
                if (scene == null)
                {
                    Log.Error("[ETMobaBattleDriver] Scene is null, cannot spawn entities");
                    return;
                }

                if (!driver.TryResolve<MobaEnterGameFlowService>(out var enterGameService) || enterGameService == null)
                {
                    Log.Error("[ETMobaBattleDriver] MobaEnterGameFlowService not found");
                    return;
                }

                if (!driver.TryResolve<global::Entitas.IContexts>(out var contexts) || contexts == null)
                {
                    Log.Error("[ETMobaBattleDriver] Entitas IContexts not found");
                    return;
                }

                var actorContext = ((global::Contexts)contexts).actor;
                var spec = ETBattleEnterGameSpecBuilder.Build(driver.PlayerSpawnData);

                enterGameService.ApplyGameStartSpec(actorContext, in spec);
                SetGamePhaseInGame(driver);
                LogEnterGameState(driver);
            }
            catch (Exception ex)
            {
                Log.Error($"[ETMobaBattleDriver] TriggerEnterGameSnapshot failed: {ex}");
            }
        }

        private static void SetGamePhaseInGame(ETMobaBattleDriver driver)
        {
            if (driver.TryResolve<MobaGamePhaseService>(out var phaseService) && phaseService != null)
            {
                phaseService.SetInGame();
            }
        }

        private static void LogEnterGameState(ETMobaBattleDriver driver)
        {
            var playerId = driver.PlayerSpawnData != null && driver.PlayerSpawnData.Count > 0
                ? driver.PlayerSpawnData[0].PlayerId
                : string.Empty;

            driver.TryResolve(out MobaActorRegistry registry);
            driver.TryResolve(out MobaPlayerActorMapService playerActorMap);
            driver.TryResolve(out MobaConfigDatabase config);

            var hasActor = false;
            var actorId = 0;
            if (!string.IsNullOrEmpty(playerId) && playerActorMap != null)
            {
                hasActor = playerActorMap.TryGetActorId(new PlayerId(playerId), out actorId);
            }

            var resolvedActorId = hasActor ? actorId : 0;
            var actorCount = -1;
            if (registry != null)
            {
                actorCount = 0;
                foreach (var _ in registry.Entries)
                {
                    actorCount++;
                }
            }

            var hasPlayerMap = playerActorMap != null;
            var hasConfig = config != null;

            Log.Info($"[ETBattleEnterGameCoordinator] EnterGame state: Player={playerId}, HasPlayerMap={hasPlayerMap}, HasActor={hasActor}, ActorId={resolvedActorId}, ActorCount={actorCount}, HasConfig={hasConfig}");
        }

    }
}
