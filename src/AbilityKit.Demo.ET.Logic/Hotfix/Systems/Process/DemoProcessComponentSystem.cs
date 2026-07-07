using System;
using System.Collections.Generic;
using ET.AbilityKit.Demo.ET.Share;
using AbilityKit.Ability.Config;
using AbilityKit.Demo.Moba.Share;

namespace ET.Logic
{
    /// <summary>
    /// 协调登录场景与本地 MOBA 战斗场景之间的 Demo 流程。
    /// 该流程会显式准备房间状态、玩家配置、战斗运行时以及 Demo/冒烟自动化。
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
        /// 切换到登录场景。
        /// </summary>
        public static async ETTask ChangeToLoginScene(this DemoProcessComponent self)
        {
            var root = self.Root();
            if (root == null)
            {
                Log.Error($"[DemoProcess] Root scene is null!");
                return;
            }

            DisposeActiveChildScenes(root);

            // 创建登录场景。
            var loginScene = EntitySceneFactory.CreateScene(root,
                IdGenerater.Instance.GenerateId(),
                IdGenerater.Instance.GenerateInstanceId(),
                SceneType.DemoLogin,
                "DemoLogin");

            // 挂载登录组件。
            self.LoginComponent = loginScene.AddComponent<DemoLoginComponent>();
            Log.Info($"[DemoProcess] Created DemoLoginComponent: {self.LoginComponent.Id}");

            var launchOptions = self.GetLaunchOptions();
            if (launchOptions.AutoLogin)
            {
                Log.Info($"[DemoProcess] Auto login enabled for {launchOptions.PlayerName}");
                self.LoginComponent.StartLogin(launchOptions.PlayerName);
            }
            else
            {
                Log.Info("[DemoProcess] Login scene ready; waiting for explicit login request");
            }

            Log.Info($"[DemoProcess] Login scene initialized");
        }

        /// <summary>
        /// 切换到战斗场景，并在启动运行时世界初始化前准备本地房间状态。
        /// </summary>
        public static async ETTask ChangeToBattleScene(this DemoProcessComponent self, long playerId, string playerName)
        {
            var root = self.Root();
            if (root == null)
            {
                Log.Error($"[DemoProcess] Root scene is null!");
                return;
            }

            DisposeActiveChildScenes(root);

            // 创建战斗场景。
            var battleScene = EntitySceneFactory.CreateScene(root,
                IdGenerater.Instance.GenerateId(),
                IdGenerater.Instance.GenerateInstanceId(),
                SceneType.DemoBattle,
                "DemoBattle");

            // ========== 步骤 1：创建配置加载器 ==========
            var textAssetLoader = new ETTextAssetLoader();

            // ========== 步骤 2：创建房间组件 ==========
            var roomComponent = battleScene.AddComponent<ETMobaRoomComponent>();

            // ========== 步骤 3：初始化房间 ==========
            var scenarioConfig = self.GetScenarioConfig();
            string matchId = scenarioConfig.CreateMatchId(playerId);

            roomComponent.InitializeRoom(
                matchId,
                scenarioConfig.MapId,
                scenarioConfig.MaxPlayers,
                scenarioConfig.TickRate,
                (int)playerId,
                scenarioConfig.RandomSeed,
                scenarioConfig.InputDelayFrames,
                scenarioConfig.MinPlayers);
            Log.Info($"[DemoProcess] Room initialized: MatchId={matchId}, MaxPlayers={scenarioConfig.MaxPlayers}");

            // ========== 步骤 4：准备本地 Demo 玩家和配置 ==========
            // 生产环境的多人流程应通过房间同步提供这些值。
            ETLocalMobaScenarioInitializer.SetupRoom(roomComponent, scenarioConfig);

            var roomPlayers = roomComponent.GetPlayers();
            var playerSpawnList = PlayerSpawnBuilder.BuildSpawnListFromRoomPlayers(roomPlayers, roomComponent.LocalPlayerId, scenarioConfig.SpawnLayout, scenarioConfig.LocalTeamId);

            // ========== 步骤 5：创建战斗组件 ==========
            var battleComponent = battleScene.AddComponent<ETBattleComponent>();

            // 创建战斗启动计划。
            var plan = new BattleStartPlan(
                mapId: scenarioConfig.MapId,
                worldId: scenarioConfig.WorldId,
                playerId: (int)playerId,
                clientId: (int)playerId,
                syncMode: SyncMode.SnapshotAuthority,
                hostMode: HostMode.Local,
                tickRate: scenarioConfig.TickRate,
                useGatewayTransport: false,
                enableConfirmedAuthorityWorld: false,
                enableReplayRecording: false,
                enableReplayPlayback: false,
                playerIds: new int[] { (int)playerId },
                inputDelayFrames: scenarioConfig.InputDelayFrames,
                gameplayId: scenarioConfig.GameplayId);

            // 初始化正式战斗运行时。Demo/冒烟自动化会在下方显式安装。
            battleComponent.AutomationOptions = scenarioConfig.AutomationOptions ?? ETBattleAutomationOptions.CreateDisabled();
            battleComponent.InitializeBattle(plan, textAssetLoader, playerSpawnList);
            ETBattleAutomationInstaller.Install(battleScene, battleComponent.AutomationOptions);

            // ========== 步骤 6：创建视图事件接收器 ==========
            // ETViewEventSink 将 AbilityKit 事件桥接到 ET 事件系统。
            var viewSink = new ETViewEventSink(battleScene);
            battleComponent.ViewSink = viewSink;

            Log.Info($"[DemoProcess] View event sink created");

            // ========== 步骤 7：绑定房间就绪事件 ==========
            roomComponent.OnAllPlayersReady += () =>
            {
                Log.Info($"[DemoProcess] All players ready! Starting battle...");
                TriggerBattleStart(battleComponent, roomComponent, scenarioConfig);
            };

            // ========== 步骤 8：本地房间已就绪时立即启动 ==========
            // 本地场景设置会在世界初始化前完成，就绪事件可能已经触发。
            if (roomComponent.CanStartBattle)
            {
                TriggerBattleStart(battleComponent, roomComponent, scenarioConfig);
            }

            self.CurrentScene = battleScene;
            self.LoginComponent = null;

            Log.Info($"[DemoProcess] Changed to Battle scene");
        }

        private static void DisposeActiveChildScenes(Scene root)
        {
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
        }

        private static ETDemoProcessLaunchOptions GetLaunchOptions(this DemoProcessComponent self)
        {
            if (self.LaunchOptions == null)
            {
                self.LaunchOptions = new ETDemoProcessLaunchOptions();
            }

            return self.LaunchOptions;
        }

        private static ETLocalMobaScenarioConfig GetScenarioConfig(this DemoProcessComponent self)
        {
            var launchOptions = self.GetLaunchOptions();
            if (launchOptions.ScenarioConfig == null)
            {
                launchOptions.ScenarioConfig = ETLocalMobaScenarioConfig.CreateLocalScenarioDefaults();
            }

            return launchOptions.ScenarioConfig;
        }

        /// <summary>
        /// 使用最新房间玩家状态启动战斗。
        /// </summary>
        private static void TriggerBattleStart(ETBattleComponent battleComponent, ETMobaRoomComponent roomComponent, ETLocalMobaScenarioConfig scenarioConfig)
        {
            if (battleComponent == null || roomComponent == null || scenarioConfig == null)
                return;

            var players = roomComponent.GetPlayers();
            if (players == null || players.Length == 0)
            {
                Log.Error($"[DemoProcess] No players in room!");
                return;
            }

            Log.Info($"[DemoProcess] ========== TriggerBattleStart ==========");
            Log.Info($"[DemoProcess] Players count: {players.Length}");

            // 解析 ET 战斗宿主。
            var battleHost = battleComponent.BattleHost;
            if (battleHost == null)
            {
                Log.Error($"[DemoProcess] BattleHost is null!");
                return;
            }

            // 根据当前房间状态重建出生数据。
            var playerSpawnList = PlayerSpawnBuilder.BuildSpawnListFromRoomPlayers(players, roomComponent.LocalPlayerId, scenarioConfig.SpawnLayout, scenarioConfig.LocalTeamId);

            Log.Info($"[DemoProcess] Calling battleHost.OnAllPlayersReady with {playerSpawnList.Count} players");
            if (!battleHost.OnAllPlayersReady(playerSpawnList))
            {
                Log.Error($"[DemoProcess] Runtime game start failed; battle state remains Ready");
                return;
            }
 
            Log.Info($"[DemoProcess] Calling battleComponent.StartBattle()");
            battleComponent.StartBattle();
 
            Log.Info($"[DemoProcess] ========== Battle started! ==========");
        }

    }
}
