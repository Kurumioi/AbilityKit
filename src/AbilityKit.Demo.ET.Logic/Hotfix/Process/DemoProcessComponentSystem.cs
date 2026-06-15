using System;
using System.Collections.Generic;
using ET.AbilityKit.Demo.ET.Share;
using AbilityKit.Ability.Config;
using AbilityKit.Demo.Moba.Share;

namespace ET.Logic
{
    /// <summary>
    /// Coordinates the demo scene flow between login and the local MOBA battle scene.
    /// The process explicitly prepares room state, player loadouts, battle runtime, and demo/smoke automation.
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
        /// Switches to the login scene.
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

            // Create login scene.
            var loginScene = EntitySceneFactory.CreateScene(root,
                IdGenerater.Instance.GenerateId(),
                IdGenerater.Instance.GenerateInstanceId(),
                SceneType.DemoLogin,
                "DemoLogin");

            // Attach login component.
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
        /// Switches to the battle scene and prepares the local room state before starting runtime world initialization.
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

            // Create battle scene.
            var battleScene = EntitySceneFactory.CreateScene(root,
                IdGenerater.Instance.GenerateId(),
                IdGenerater.Instance.GenerateInstanceId(),
                SceneType.DemoBattle,
                "DemoBattle");

            // ========== Step 1: Create config loader ==========
            var textAssetLoader = new ETTextAssetLoader();

            // ========== Step 2: Create room component ==========
            var roomComponent = battleScene.AddComponent<ETMobaRoomComponent>();

            // ========== Step 3: Initialize room ==========
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

            // ========== Step 4: Prepare local demo players and loadouts ==========
            // Production multiplayer flows should provide these values through room synchronization.
            ETLocalMobaScenarioInitializer.SetupRoom(roomComponent, scenarioConfig);

            var roomPlayers = roomComponent.GetPlayers();
            var playerSpawnList = PlayerSpawnBuilder.BuildSpawnListFromRoomPlayers(roomPlayers, roomComponent.LocalPlayerId, scenarioConfig.SpawnLayout, scenarioConfig.LocalTeamId);

            // ========== Step 5: Create battle component ==========
            var battleComponent = battleScene.AddComponent<ETBattleComponent>();

            // Create battle start plan.
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

            // Initialize the formal battle runtime. Demo/smoke automation is installed explicitly below.
            battleComponent.InitializeBattle(plan, textAssetLoader, playerSpawnList);
            battleComponent.AutomationOptions = scenarioConfig.AutomationOptions ?? ETBattleAutomationOptions.CreateDisabled();
            ETBattleAutomationInstaller.Install(battleScene, battleComponent.AutomationOptions);

            // ========== Step 6: Create view event sink ==========
            // ETViewEventSink bridges AbilityKit events into the ET event system.
            var viewSink = new ETViewEventSink(battleScene);
            battleComponent.ViewSink = viewSink;

            Log.Info($"[DemoProcess] View event sink created");

            // ========== Step 7: Bind room-ready event ==========
            roomComponent.OnAllPlayersReady += () =>
            {
                Log.Info($"[DemoProcess] All players ready! Starting battle...");
                TriggerBattleStart(battleComponent, roomComponent, scenarioConfig);
            };

            // ========== Step 8: Start immediately when the local room is already ready ==========
            // Local scenario setup is completed before world initialization; the ready event may already have fired.
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
                launchOptions.ScenarioConfig = ETLocalMobaScenarioConfig.CreateLocalDemoDefaults();
            }

            return launchOptions.ScenarioConfig;
        }

        /// <summary>
        /// Starts the battle using the latest room player state.
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

            // Resolve ET battle host.
            var battleHost = battleComponent.BattleHost;
            if (battleHost == null)
            {
                Log.Error($"[DemoProcess] BattleHost is null!");
                return;
            }

            // Rebuild spawn data from the current room state.
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
