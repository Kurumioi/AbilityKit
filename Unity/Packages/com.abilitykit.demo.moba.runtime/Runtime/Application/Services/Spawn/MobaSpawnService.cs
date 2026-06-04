using System;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Coordinator;
using AbilityKit.Core.Common.Log;
using AbilityKit.Core.Generic;
using AbilityKit.Ability.Host;
using AbilityKit.Protocol.Moba;

namespace AbilityKit.Demo.Moba.Services
{
    public interface ILogicWorldSpawnService : IService
    {
        bool CreateLogicWorldSpawns(LogicWorldSpawnData[] spawns);
    }

    /// <summary>
    /// Legacy fallback that converts spawn data into a game-start request.
    /// The formal MOBA startup path provides WorldInitData before bootstrap and uses StartGameStage.
    /// </summary>
    [WorldService(typeof(MobaSpawnService))]
    [WorldService(typeof(ILogicWorldSpawnService))]
    [WorldService(typeof(ISpawnService))]
    public sealed class MobaSpawnService : ILogicWorldSpawnService, ISpawnService
    {
        [WorldInject] private IMobaGameStartPort _gameStart;

        public bool CreateSpawns(PlayerSpawnData[] spawns)
        {
            return CreateLogicWorldSpawns(ToLogicWorldSpawns(spawns));
        }

        public bool CreateLogicWorldSpawns(LogicWorldSpawnData[] spawns)
        {
            if (spawns == null || spawns.Length == 0)
            {
                Log.Warning("[MobaSpawnService] No spawns to create");
                return false;
            }

            if (_gameStart == null)
            {
                Log.Error("[MobaSpawnService] IMobaGameStartPort not found, cannot create spawns");
                return false;
            }

            Log.Warning("[MobaSpawnService] Legacy spawn fallback is starting battle directly; prefer create-world init payload");

            try
            {
                var spec = SpawnDataConverter.ConvertToGameStartSpec(
                    spawns,
                    new PlayerId(spawns[0].PlayerId.ToString()),
                    "session_spawn",
                    mapId: 1,
                    tickRate: 30,
                    inputDelayFrames: 0,
                    randomSeed: Environment.TickCount
                );

                var result = _gameStart.TryStartGame(in spec);

                if (result.Succeeded)
                {
                    Log.Info($"[MobaSpawnService] Legacy spawn fallback started battle with {spawns.Length} spawns");
                }
                else
                {
                    Log.Warning($"[MobaSpawnService] Legacy spawn fallback did not start battle. {result}");
                }

                return result.Succeeded;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "[MobaSpawnService] CreateLogicWorldSpawns failed");
                return false;
            }
        }

        private static LogicWorldSpawnData[] ToLogicWorldSpawns(PlayerSpawnData[] spawns)
        {
            if (spawns == null || spawns.Length == 0)
            {
                return Array.Empty<LogicWorldSpawnData>();
            }

            var logicWorldSpawns = new LogicWorldSpawnData[spawns.Length];
            for (int i = 0; i < spawns.Length; i++)
            {
                var spawn = spawns[i];
                logicWorldSpawns[i] = new LogicWorldSpawnData(
                    spawn.PlayerId,
                    spawn.CharacterId,
                    spawn.TeamId,
                    spawn.X,
                    spawn.Y,
                    spawn.Z,
                    spawn.Name);
            }

            return logicWorldSpawns;
        }

        public void Dispose()
        {
        }
    }
}
