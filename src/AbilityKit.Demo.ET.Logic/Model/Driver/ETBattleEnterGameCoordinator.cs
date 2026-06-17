using System;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Extensions.Moba.Runtime;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.Services;

namespace ET.Logic
{
    /// <summary>
    /// Builds ET host startup data and submits it to the runtime game-start port.
    /// </summary>
    public static class ETBattleEnterGameCoordinator
    {
        public static bool Trigger(ETMobaBattleDriver driver)
        {
            try
            {
                var scene = driver.Scene();
                if (scene == null)
                {
                    Log.Error("[ETMobaBattleDriver] Scene is null, cannot spawn entities");
                    return false;
                }
 
                if (!driver.TryResolve<IMobaBattleRuntimePort>(out var runtime) || runtime == null)
                {
                    Log.Error("[ETMobaBattleDriver] IMobaBattleRuntimePort not found");
                    return false;
                }
 
                if (!runtime.Status.IsReadyForGameStart)
                {
                    Log.Error($"[ETMobaBattleDriver] Runtime is not ready for game start. {runtime.Status}");
                    return false;
                }
 
                var spec = ETBattleEnterGameSpecBuilder.Build(driver.Plan, driver.PlayerSpawnData);
                var result = runtime.TryStartGame(in spec);
                if (!result.Succeeded)
                {
                    if (result.FailureCode == MobaGameStartFailureCode.AlreadyStarted)
                    {
                        driver.RuntimeGameStarted = true;
                        Log.Info($"[ETBattleEnterGameCoordinator] Runtime game already started; treating as idempotent success. {result}");
                        LogEnterGameState(driver);
                        return true;
                    }

                    Log.Warning($"[ETBattleEnterGameCoordinator] Game start rejected. {result}");
                    return false;
                }
 
                driver.RuntimeGameStarted = true;
                LogEnterGameState(driver);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[ETMobaBattleDriver] TriggerEnterGameSnapshot failed: {ex}");
                return false;
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
