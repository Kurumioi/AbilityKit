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
    /// ���� Scene ֮����л��߼�
    ///
    /// �������̣�
    /// 1. ����ս������
    /// 2. ����������� (ETMobaRoomComponent)���������׼������
    /// 3. ��Ҽ��롢ѡӢ�ۡ�׼��
    /// 4. �������׼����ɺ�ʹ�� RoomState �е������Ϣ��ʼ��ս��
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
        /// �л�����¼����
        /// </summary>
        public static async ETTask ChangeToLoginScene(this DemoProcessComponent self)
        {
            var root = self.Root();
            if (root == null)
            {
                Log.Error($"[DemoProcess] Root scene is null!");
                return;
            }

            // �Ƴ�֮ǰ���ӳ���
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

            // ������¼����
            var loginScene = EntitySceneFactory.CreateScene(root,
                IdGenerater.Instance.GenerateId(),
                IdGenerater.Instance.GenerateInstanceId(),
                SceneType.DemoLogin,
                "DemoLogin");

            // ���ӵ�¼���
            self.LoginComponent = loginScene.AddComponent<DemoLoginComponent>();
            Log.Info($"[DemoProcess] Created DemoLoginComponent: {self.LoginComponent.Id}");

            // �ֶ����õ�¼�߼�
            self.LoginComponent.Awake();
            Log.Info($"[DemoProcess] Called DemoLoginComponent.Awake()");

            // ֱ�Ӵ�����¼
            Log.Info($"[DemoProcess] Triggering login for TestPlayer...");
            self.LoginComponent.State = LoginState.Connecting;
            self.LoginComponent.PlayerId = IdGenerater.Instance.GenerateId();
            self.LoginComponent.PlayerName = "TestPlayer";
            self.LoginComponent.State = LoginState.LoginSuccess;

            Log.Info($"[DemoProcess] Login success! PlayerId: {self.LoginComponent.PlayerId}");

            // ֱ���л���ս������
            Log.Info($"[DemoProcess] Auto-entering battle...");
            await self.ChangeToBattleScene(self.LoginComponent.PlayerId, self.LoginComponent.PlayerName);

            Log.Info($"[DemoProcess] Login scene completed; current scene is managed by battle transition");
        }

        /// <summary>
        /// �л���ս������
        /// ʹ�÷���ϵͳ�������׼������
        /// </summary>
        public static async ETTask ChangeToBattleScene(this DemoProcessComponent self, long playerId, string playerName)
        {
            var root = self.Root();
            if (root == null)
            {
                Log.Error($"[DemoProcess] Root scene is null!");
                return;
            }

            // �Ƴ�֮ǰ���ӳ���
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

            // ����ս������
            var battleScene = EntitySceneFactory.CreateScene(root,
                IdGenerater.Instance.GenerateId(),
                IdGenerater.Instance.GenerateInstanceId(),
                SceneType.DemoBattle,
                "DemoBattle");

            // ========== ����1: �������ü����� ==========
            var textAssetLoader = new ETTextAssetLoader();

            // ========== ����2: ����������� ==========
            var roomComponent = battleScene.AddComponent<ETMobaRoomComponent>();

            // ========== ����3: ��ʼ������ ==========
            string matchId = $"match_{Environment.TickCount}";
            int maxPlayers = 6;
            int tickRate = 30;

            roomComponent.InitializeRoom(matchId, mapId: 1, maxPlayers, tickRate, (int)playerId);
            Log.Info($"[DemoProcess] Room initialized: MatchId={matchId}, MaxPlayers={maxPlayers}");

            // ========== ����4: ����ս����� ==========
            var battleComponent = battleScene.AddComponent<ETBattleComponent>();

            // ���������ƻ�
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

            // ��ʼ��ս����������� textAssetLoader��
            battleComponent.InitializeBattle(plan, textAssetLoader);

            // ========== ����5: ������ͼ�¼��Ž� ==========
            // ETViewEventSink ���� AbilityKit �¼��Žӵ� ET �¼�ϵͳ
            var viewSink = new ETViewEventSink(battleScene);
            battleComponent.ViewSink = viewSink;

            Log.Info($"[DemoProcess] View event sink created");

            // ========== ����6: ��������׼�����¼� ==========
            // ע�⣺������ AutoSetupForLocalTest ֮ǰע�ᣬ�Ա�ص�����ȷ����
            roomComponent.OnAllPlayersReady += () =>
            {
                Log.Info($"[DemoProcess] All players ready! Starting battle...");
                TriggerBattleStart(battleComponent, roomComponent);
            };

            // ========== ����7: ģ����Ҽ����׼�������ڱ��ز��ԣ�==========
            // ��ʵ�ʶ�����Ϸ�У������ͨ������ͬ���ȴ��������
            roomComponent.AutoSetupForLocalTest(heroId: 1001, attributeTemplateId: 1001);

            // ========== ����8: ����Ѿ�׼�����ˣ�����ս����ʼ ==========
            // ��������Ϊ�˴��������ڱ��ز��Ե����
            if (roomComponent.CanStartBattle && !roomComponent.HasTriggeredBattleStart)
            {
                roomComponent.CheckAndTriggerBattleStart();
            }

            self.CurrentScene = battleScene;
            self.LoginComponent = null;

            Log.Info($"[DemoProcess] Changed to Battle scene");
        }

        /// <summary>
        /// ����ս����ʼ
        /// ʹ�� RoomState �е������Ϣ��ʼ��ս��
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

            // ��ȡ BattleDriver
            var battleDriver = battleComponent.BattleDriver as ETMobaBattleDriver;
            if (battleDriver == null)
            {
                Log.Error($"[DemoProcess] BattleDriver is not ETMobaBattleDriver!");
                return;
            }

            // �� RoomState ��������б�
            var playerSpawnList = ConvertPlayersToSpawnList(players, roomComponent.LocalPlayerId);

            Log.Info($"[DemoProcess] Calling battleDriver.OnAllPlayersReady with {playerSpawnList.Count} players");
            if (!battleDriver.OnAllPlayersReady(playerSpawnList))
            {
                Log.Error($"[DemoProcess] Runtime game start failed; battle state remains Ready");
                return;
            }
 
            Log.Info($"[DemoProcess] Calling battleComponent.StartBattle()");
            battleComponent.StartBattle();
 
            Log.Info($"[DemoProcess] ========== Battle started! ==========");
        }

        /// <summary>
        /// ���������ת��Ϊ�����б�
        /// </summary>
        private static List<ETPlayerSpawnData> ConvertPlayersToSpawnList(MobaRoomPlayerSnapshot[] players, PlayerId localPlayerId)
        {
            var spawnList = new List<ETPlayerSpawnData>();

            int team1Count = 0;
            int team2Count = 0;

            foreach (var player in players)
            {
                // ����λ��
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
                    playerId: player.PlayerId.Value,
                    characterId: player.HeroId,
                    attributeTemplateId: player.AttributeTemplateId,
                    basicAttackSkillId: player.BasicAttackSkillId,
                    skillIds: player.SkillIds,
                    characterName: $"Hero_{player.HeroId}",
                    teamId: player.TeamId,
                    x, 0f, z,
                    rotY: 0f,
                    scale: 1f,
                    hp: 0f,
                    maxHp: 0f);

                spawnList.Add(spawnData);
                Log.Info($"[DemoProcess] Converted player: {player.PlayerId.Value}, HeroId={player.HeroId}, AttrTemplateId={player.AttributeTemplateId}, BasicAttackSkillId={player.BasicAttackSkillId}, Team={player.TeamId}");
            }

            return spawnList;
        }
    }
}
