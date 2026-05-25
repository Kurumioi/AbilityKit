using System;
using System.Collections.Generic;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Extensions.Moba.Room;
using ET.AbilityKit.Demo.ET.Share;
using AbilityKit.Ability.Config;
using AbilityKit.Demo.Moba.Share;

namespace ET.Logic
{
    /// <summary>
    /// DemoProcessComponent System
    /// 处理 Scene 之间的切换逻辑
    ///
    /// 完整流程：
    /// 1. 创建战斗场景
    /// 2. 创建房间组件 (ETMobaRoomComponent)，管理玩家准备流程
    /// 3. 玩家加入、选英雄、准备
    /// 4. 所有玩家准备完成后，使用 RoomState 中的玩家信息初始化战斗
    /// </summary>
    [EntitySystemOf(typeof(DemoProcessComponent))]
    [FriendOf(typeof(DemoProcessComponent))]
    public static partial class DemoProcessComponentSystem
    {
        [EntitySystem]
        private static void Awake(this DemoProcessComponent self)
        {
            Log.Info($"[DemoProcess] DemoProcessComponent awake");
        }

        [EntitySystem]
        private static void Update(this DemoProcessComponent self)
        {
        }

        /// <summary>
        /// 切换到登录场景
        /// </summary>
        public static async ETTask ChangeToLoginScene(this DemoProcessComponent self)
        {
            var root = self.Root();
            if (root == null)
            {
                Log.Error($"[DemoProcess] Root scene is null!");
                return;
            }

            // 移除之前的子场景
            List<long> keysToRemove = new List<long>();
            foreach (var child in root.Children.Values)
            {
                if (child is Scene scene && scene.SceneType != 0)
                {
                    keysToRemove.Add(child.Id);
                }
            }
            foreach (var key in keysToRemove)
            {
                if (root.Children.TryGetValue(key, out var child))
                {
                    child.Dispose();
                }
            }

            // 创建登录场景
            var loginScene = EntitySceneFactory.CreateScene(root,
                IdGenerater.Instance.GenerateId(),
                IdGenerater.Instance.GenerateInstanceId(),
                SceneType.DemoLogin,
                "DemoLogin");

            // 添加登录组件
            self.LoginComponent = loginScene.AddComponent<DemoLoginComponent>();
            Log.Info($"[DemoProcess] Created DemoLoginComponent: {self.LoginComponent.Id}");

            // 手动调用登录逻辑
            self.LoginComponent.Awake();
            Log.Info($"[DemoProcess] Called DemoLoginComponent.Awake()");

            // 直接触发登录
            Log.Info($"[DemoProcess] Triggering login for TestPlayer...");
            self.LoginComponent.State = LoginState.Connecting;
            self.LoginComponent.PlayerId = IdGenerater.Instance.GenerateId();
            self.LoginComponent.PlayerName = "TestPlayer";
            self.LoginComponent.State = LoginState.LoginSuccess;

            Log.Info($"[DemoProcess] Login success! PlayerId: {self.LoginComponent.PlayerId}");

            // 直接切换到战斗场景
            Log.Info($"[DemoProcess] Auto-entering battle...");
            await self.ChangeToBattleScene(self.LoginComponent.PlayerId, self.LoginComponent.PlayerName);

            self.CurrentScene = loginScene;

            Log.Info($"[DemoProcess] Changed to Login scene");
        }

        /// <summary>
        /// 切换到战斗场景
        /// 使用房间系统管理玩家准备流程
        /// </summary>
        public static async ETTask ChangeToBattleScene(this DemoProcessComponent self, long playerId, string playerName)
        {
            var root = self.Root();
            if (root == null)
            {
                Log.Error($"[DemoProcess] Root scene is null!");
                return;
            }

            // 移除之前的子场景
            List<long> battleKeysToRemove = new List<long>();
            foreach (var child in root.Children.Values)
            {
                if (child is Scene scene && scene.SceneType != 0)
                {
                    battleKeysToRemove.Add(child.Id);
                }
            }
            foreach (var key in battleKeysToRemove)
            {
                if (root.Children.TryGetValue(key, out var child))
                {
                    child.Dispose();
                }
            }

            // 创建战斗场景
            var battleScene = EntitySceneFactory.CreateScene(root,
                IdGenerater.Instance.GenerateId(),
                IdGenerater.Instance.GenerateInstanceId(),
                SceneType.DemoBattle,
                "DemoBattle");

            // ========== 步骤1: 创建配置加载器 ==========
            var textAssetLoader = new ETTextAssetLoader();

            // ========== 步骤2: 创建房间组件 ==========
            var roomComponent = battleScene.AddComponent<ETMobaRoomComponent>();

            // ========== 步骤3: 初始化房间 ==========
            string matchId = $"match_{Environment.TickCount}";
            int maxPlayers = 6;
            int tickRate = 30;

            roomComponent.InitializeRoom(matchId, mapId: 1, maxPlayers, tickRate, (int)playerId);
            Log.Info($"[DemoProcess] Room initialized: MatchId={matchId}, MaxPlayers={maxPlayers}");

            // ========== 步骤4: 添加战斗组件 ==========
            var battleComponent = battleScene.AddComponent<ETBattleComponent>();

            // 创建启动计划
            var plan = new BattleStartPlan(
                mapId: 1,
                worldId: 1,
                playerId: (int)playerId,
                clientId: (int)playerId,
                syncMode: SyncMode.SnapshotAuthority,
                hostMode: HostMode.Local,
                tickRate: tickRate,
                useGatewayTransport: false,
                enableConfirmedAuthorityWorld: false,
                enableReplayRecording: false,
                enableReplayPlayback: false,
                playerIds: new int[] { (int)playerId });

            // 初始化战斗组件（传递 textAssetLoader）
            battleComponent.InitializeBattle(plan, textAssetLoader);

            // ========== 步骤5: 创建视图事件桥接 ==========
            // ETViewEventSink 负责将 AbilityKit 事件桥接到 ET 事件系统
            var viewSink = new ETViewEventSink(battleScene);
            battleComponent.ViewSink = viewSink;

            Log.Info($"[DemoProcess] View event sink created");

            // ========== 步骤6: 监听房间准备好事件 ==========
            // 注意：必须在 AutoSetupForLocalTest 之前注册，以便回调能正确触发
            roomComponent.OnAllPlayersReady += () =>
            {
                Log.Info($"[DemoProcess] All players ready! Starting battle...");
                TriggerBattleStart(battleComponent, roomComponent);
            };

            // ========== 步骤7: 模拟玩家加入和准备（用于本地测试）==========
            // 在实际多人游戏中，这里会通过网络同步等待其他玩家
            roomComponent.AutoSetupForLocalTest(heroId: 1001, attributeTemplateId: 1);

            // ========== 步骤8: 如果已经准备好了，触发战斗开始 ==========
            // 这里检查是为了处理单人在本地测试的情况
            if (roomComponent.CanStartBattle && !roomComponent.HasTriggeredBattleStart)
            {
                roomComponent.CheckAndTriggerBattleStart();
            }

            self.CurrentScene = battleScene;
            self.LoginComponent = null;

            Log.Info($"[DemoProcess] Changed to Battle scene");
        }

        /// <summary>
        /// 触发战斗开始
        /// 使用 RoomState 中的玩家信息初始化战斗
        /// </summary>
        private static void TriggerBattleStart(ETBattleComponent battleComponent, ETMobaRoomComponent roomComponent)
        {
            if (battleComponent == null || roomComponent == null)
                return;

            var players = roomComponent.GetPlayers();
            if (players == null || players.Length == 0)
            {
                Log.Error($"[DemoProcess] No players in room!");
                return;
            }

            Log.Info($"[DemoProcess] ========== TriggerBattleStart ==========");
            Log.Info($"[DemoProcess] Players count: {players.Length}");

            // 获取 BattleDriver
            var battleDriver = battleComponent.BattleDriver as ETMobaBattleDriver;
            if (battleDriver == null)
            {
                Log.Error($"[DemoProcess] BattleDriver is not ETMobaBattleDriver!");
                return;
            }

            // 从 RoomState 构建玩家列表
            var playerSpawnList = ConvertPlayersToSpawnList(players, roomComponent.LocalPlayerId);

            // 先启动战斗（设置 _isRunning = true）
            Log.Info($"[DemoProcess] Calling battleComponent.StartBattle()");
            battleComponent.StartBattle();

            // 再调用 OnAllPlayersReady（此时 _isRunning 已经为 true）
            Log.Info($"[DemoProcess] Calling battleDriver.OnAllPlayersReady with {playerSpawnList.Count} players");
            battleDriver.OnAllPlayersReady(playerSpawnList);

            Log.Info($"[DemoProcess] ========== Battle started! ==========");
        }

        /// <summary>
        /// 将房间玩家转换为生成列表
        /// </summary>
        private static List<ETPlayerSpawnData> ConvertPlayersToSpawnList(MobaRoomPlayerSnapshot[] players, PlayerId localPlayerId)
        {
            var spawnList = new List<ETPlayerSpawnData>();

            int team1Count = 0;
            int team2Count = 0;

            foreach (var player in players)
            {
                // 计算位置
                float x, z;
                if (player.TeamId == 1)
                {
                    x = 0f;
                    z = 10f * team1Count;
                    team1Count++;
                }
                else
                {
                    x = 50f;
                    z = 10f * team2Count;
                    team2Count++;
                }

                var spawnData = new ETPlayerSpawnData(
                    actorId: int.Parse(player.PlayerId.Value),
                    characterId: player.HeroId,
                    characterName: $"Hero_{player.HeroId}",
                    teamId: player.TeamId,
                    x, 0f, z);

                spawnList.Add(spawnData);
                Log.Info($"[DemoProcess] Converted player: {player.PlayerId.Value}, HeroId={player.HeroId}, Team={player.TeamId}");
            }

            return spawnList;
        }
    }
}
