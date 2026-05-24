using System;
using System.Collections.Generic;
using AbilityKit.Ability.Config;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Demo.Moba.Share;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// ETBattlePlayerRegistryComponent System
    ///
    /// Business logic for managing player registration, ready state, and entity spawning
    ///
    /// 正确流程:
    /// 1. 配置规定最大玩家数量 (MaxPlayerCount)
    /// 2. 客户端发送准备信号
    /// 3. 等待全部玩家准备完成
    /// 4. 才派发实体创建
    /// </summary>
    [EntitySystemOf(typeof(ETBattlePlayerRegistryComponent))]
    [FriendOf(typeof(ETBattlePlayerRegistryComponent))]
    [FriendOf(typeof(ETBattleComponent))]
    [FriendOf(typeof(ETMobaBattleDriver))]
    public static partial class ETBattlePlayerRegistrySystem
    {
        [EntitySystem]
        private static void Awake(this ETBattlePlayerRegistryComponent self)
        {
            Log.Info("[ETBattlePlayerRegistry] System awake");
            self.RegisteredPlayers = new List<PlayerRegistration>();
            self.HasSpawnedEntities = false;
        }

        // ============== Properties ==============

        /// <summary>
        /// 获取已准备的玩家数量
        /// </summary>
        public static int GetReadyPlayerCount(ETBattlePlayerRegistryComponent self)
        {
            int count = 0;
            if (self.RegisteredPlayers == null)
                return count;

            foreach (var player in self.RegisteredPlayers)
            {
                if (player.IsReady)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 是否所有玩家都已准备
        /// </summary>
        public static bool AreAllPlayersReady(ETBattlePlayerRegistryComponent self)
        {
            return GetReadyPlayerCount(self) >= self.MaxPlayerCount;
        }

        // ============== Basic Operations ==============

        /// <summary>
        /// 设置最大玩家数量
        /// </summary>
        public static void SetMaxPlayerCount(this ETBattlePlayerRegistryComponent self, int count)
        {
            self.MaxPlayerCount = count;
            Log.Info($"[ETBattlePlayerRegistry] MaxPlayerCount set to {count}");
        }

        /// <summary>
        /// 注册玩家
        /// </summary>
        public static bool RegisterPlayer(this ETBattlePlayerRegistryComponent self, int playerId, string playerName, int characterId = 1001, int teamId = 1)
        {
            if (FindPlayer(self, playerId) != null)
            {
                Log.Warning($"[ETBattlePlayerRegistry] Player {playerId} already registered");
                return false;
            }

            if (self.RegisteredPlayers.Count >= self.MaxPlayerCount)
            {
                Log.Warning($"[ETBattlePlayerRegistry] Max player count ({self.MaxPlayerCount}) reached");
                return false;
            }

            var registration = new PlayerRegistration
            {
                PlayerId = playerId,
                PlayerName = playerName,
                CharacterId = characterId,
                TeamId = teamId,
                IsReady = false,
                RegisterTime = Environment.TickCount64
            };

            self.RegisteredPlayers.Add(registration);
            Log.Info($"[ETBattlePlayerRegistry] Player registered: {playerName} ({playerId}), Total: {self.RegisteredPlayers.Count}/{self.MaxPlayerCount}");

            return true;
        }

        /// <summary>
        /// 设置玩家准备状态
        /// </summary>
        public static bool SetPlayerReady(this ETBattlePlayerRegistryComponent self, int playerId, bool ready)
        {
            for (int i = 0; i < self.RegisteredPlayers.Count; i++)
            {
                if (self.RegisteredPlayers[i].PlayerId == playerId)
                {
                    var p = self.RegisteredPlayers[i];
                    p.IsReady = ready;
                    self.RegisteredPlayers[i] = p;

                    Log.Info($"[ETBattlePlayerRegistry] Player {playerId} {(ready ? "ready" : "unready")} ({GetReadyPlayerCount(self)}/{self.MaxPlayerCount})");
                    return true;
                }
            }
            Log.Warning($"[ETBattlePlayerRegistry] Cannot update unregistered player {playerId}");
            return false;
        }

        /// <summary>
        /// 查找玩家
        /// </summary>
        public static PlayerRegistration? FindPlayer(ETBattlePlayerRegistryComponent self, int playerId)
        {
            if (self.RegisteredPlayers == null)
                return null;

            foreach (var player in self.RegisteredPlayers)
            {
                if (player.PlayerId == playerId)
                {
                    return player;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取本地玩家
        /// </summary>
        public static PlayerRegistration? GetLocalPlayer(ETBattlePlayerRegistryComponent self)
        {
            return FindPlayer(self, self.LocalPlayerId);
        }

        /// <summary>
        /// 获取所有已准备的玩家
        /// </summary>
        public static List<PlayerRegistration> GetReadyPlayers(ETBattlePlayerRegistryComponent self)
        {
            var ready = new List<PlayerRegistration>();
            if (self.RegisteredPlayers == null)
                return ready;

            foreach (var player in self.RegisteredPlayers)
            {
                if (player.IsReady)
                {
                    ready.Add(player);
                }
            }
            return ready;
        }

        /// <summary>
        /// 清空所有玩家
        /// </summary>
        public static void Clear(ETBattlePlayerRegistryComponent self)
        {
            self.RegisteredPlayers?.Clear();
            self.HasSpawnedEntities = false;
        }

        // ============== Initialization ==============

        /// <summary>
        /// 初始化玩家注册组件
        /// </summary>
        public static void Initialize(this ETBattlePlayerRegistryComponent self, int maxPlayerCount, int localPlayerId)
        {
            self.SetMaxPlayerCount(maxPlayerCount);
            self.LocalPlayerId = localPlayerId;
            Log.Info($"[ETBattlePlayerRegistry] Initialized: MaxPlayers={maxPlayerCount}, LocalPlayer={localPlayerId}");
        }

        // ============== Player Registration ==============

        /// <summary>
        /// 处理玩家准备完成
        /// </summary>
        public static void OnPlayerReady(this ETBattlePlayerRegistryComponent self, int playerId)
        {
            if (self.HasSpawnedEntities)
            {
                Log.Warning($"[ETBattlePlayerRegistry] Entities already spawned, ignoring ready signal");
                return;
            }

            self.SetPlayerReady(playerId, true);

            if (AreAllPlayersReady(self))
            {
                Log.Info($"[ETBattlePlayerRegistry] All players ready! Triggering entity spawn...");
                SpawnEntitiesForAllPlayers(self);
            }
            else
            {
                Log.Info($"[ETBattlePlayerRegistry] Waiting for players: {GetReadyPlayerCount(self)}/{self.MaxPlayerCount}");
            }
        }

        /// <summary>
        /// 为所有已准备玩家创建实体
        /// </summary>
        private static void SpawnEntitiesForAllPlayers(ETBattlePlayerRegistryComponent self)
        {
            var battleComponent = self.GetParent<ETBattleComponent>();
            if (battleComponent == null)
            {
                Log.Error("[ETBattlePlayerRegistry] BattleComponent not found");
                return;
            }

            var battleDriver = battleComponent.BattleDriver;
            if (battleDriver == null)
            {
                Log.Error("[ETBattlePlayerRegistry] BattleDriver not found");
                return;
            }

            var readyPlayers = GetReadyPlayers(self);
            var playerSpawnList = PlayerSpawnBuilder.BuildSpawnList(readyPlayers);

            if (battleDriver is ETMobaBattleDriver mobaDriver)
            {
                mobaDriver.OnAllPlayersReady(playerSpawnList);
            }

            self.HasSpawnedEntities = true;
            Log.Info($"[ETBattlePlayerRegistry] Spawned entities for {readyPlayers.Count} players");
        }

        /// <summary>
        /// 直接触发实体创建（跳过准备阶段，用于本地测试）
        /// </summary>
        public static void ForceSpawnEntities(this ETBattlePlayerRegistryComponent self, ITextAssetLoader loader, int localPlayerId)
        {
            if (self.HasSpawnedEntities)
            {
                Log.Warning($"[ETBattlePlayerRegistry] Entities already spawned");
                return;
            }

            var battleComponent = self.GetParent<ETBattleComponent>();
            if (battleComponent == null)
            {
                Log.Error("[ETBattlePlayerRegistry] BattleComponent not found");
                return;
            }

            var playerSpawnList = PlayerSpawnBuilder.BuildSpawnListFromConfig(loader, localPlayerId);

            if (playerSpawnList.Count == 0)
            {
                Log.Warning("[ETBattlePlayerRegistry] No players from config, using defaults");
                BuildDefaultSpawnList(self, localPlayerId);
                playerSpawnList = PlayerSpawnBuilder.BuildSpawnListFromConfig(null, localPlayerId);
            }

            var battleDriver = battleComponent.BattleDriver;
            if (battleDriver is ETMobaBattleDriver mobaDriver)
            {
                mobaDriver.OnAllPlayersReady(playerSpawnList);
            }

            self.HasSpawnedEntities = true;
            Log.Info($"[ETBattlePlayerRegistry] Force spawned {playerSpawnList.Count} entities");
        }

        /// <summary>
        /// 构建默认生成列表（配置加载失败时）
        /// </summary>
        private static void BuildDefaultSpawnList(ETBattlePlayerRegistryComponent self, int localPlayerId)
        {
            int actorId = localPlayerId > 0 ? localPlayerId : 1;

            self.RegisterPlayer(actorId, $"Player_{actorId}", 1001, 1);
            self.SetPlayerReady(actorId, true);

            self.RegisterPlayer(actorId + 1, "AI_Archer", 1002, 1);
            self.SetPlayerReady(actorId + 1, true);

            self.RegisterPlayer(actorId + 2, "AI_Mage", 1003, 1);
            self.SetPlayerReady(actorId + 2, true);

            self.RegisterPlayer(2001, "Enemy_Warrior", 1001, 2);
            self.SetPlayerReady(2001, true);

            self.RegisterPlayer(2002, "Enemy_Archer", 1002, 2);
            self.SetPlayerReady(2002, true);

            self.RegisterPlayer(2003, "Enemy_Mage", 1003, 2);
            self.SetPlayerReady(2003, true);
        }
    }
}
