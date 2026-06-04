using System;
using System.Collections.Generic;
using System.ComponentModel;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Extensions.Moba.CreateWorld;
using AbilityKit.Demo.Moba.Share;
using AbilityKit.Game.Battle.Transport.Moba.Client;
using AbilityKit.Protocol.Moba;
using ShareSyncMode = AbilityKit.Demo.Moba.Share.SyncMode;
using ShareRunMode = AbilityKit.Demo.Moba.Share.RunMode;

namespace AbilityKit.Demo.Moba.Console.Battle.Config
{
    /// <summary>
    /// Console 战斗启动配置
    /// 对应 Unity 项目的 BattleStartConfig ScriptableObject
    /// </summary>
    public sealed class BattleStartConfig : IBattleStartConfigProvider, IBattleStartConfig
    {
        /// <summary>
        /// 配置名称
        /// </summary>
        public string Name { get; set; } = "Default";

        /// <summary>
        /// 世界 ID
        /// </summary>
        public string WorldId { get; set; } = "room_1";

        /// <summary>
        /// 世界类型
        /// </summary>
        public string WorldType { get; set; } = "MobaBattle";

        /// <summary>
        /// 客户端 ID
        /// </summary>
        public string ClientId { get; set; } = "console_client";

        /// <summary>
        /// 本地玩家 ID（字符串）
        /// </summary>
        public string PlayerId { get; set; } = "player_1";

        /// <summary>
        /// 地图 ID
        /// </summary>
        public int MapId { get; set; } = 1;

        /// <summary>
        /// 玩法配置 ID
        /// </summary>
        public int GameplayId { get; set; } = 1;

        /// <summary>
        /// 规则集 ID
        /// </summary>
        public int RuleSetId { get; set; } = 0;

        /// <summary>
        /// 配置版本
        /// </summary>
        public int ConfigVersion { get; set; } = 0;

        /// <summary>
        /// 协议版本
        /// </summary>
        public int ProtocolVersion { get; set; } = 0;

        /// <summary>
        /// 随机种子
        /// </summary>
        public int RandomSeed { get; set; } = 10001;

        /// <summary>
        /// Tick 率
        /// </summary>
        public int TickRate { get; set; } = 30;

        /// <summary>
        /// 输入延迟帧数
        /// </summary>
        public int InputDelayFrames { get; set; } = 2;

        /// <summary>
        /// 同步模式
        /// </summary>
        public ShareSyncMode SyncMode { get; set; } = ShareSyncMode.Lockstep;

        /// <summary>
        /// 运行模式
        /// </summary>
        public ShareRunMode RunMode { get; set; } = ShareRunMode.Normal;

        /// <summary>
        /// 启用调试
        /// </summary>
        public bool EnableDebug { get; set; } = true;

        /// <summary>
        /// 最大玩家数量
        /// </summary>
        public int MaxPlayerCount { get; set; } = 10;

        /// <summary>
        /// 启用输入录制
        /// </summary>
        public bool EnableInputRecording { get; set; } = false;

        /// <summary>
        /// 输入录制输出路径
        /// </summary>
        public string InputRecordOutputPath { get; set; } = "";

        /// <summary>
        /// 启用输入回放
        /// </summary>
        public bool EnableInputReplay { get; set; } = false;

        /// <summary>
        /// 输入回放路径
        /// </summary>
        public string InputReplayPath { get; set; } = "";

        /// <summary>
        /// 启用客户端预测
        /// </summary>
        public bool EnableClientPrediction { get; set; } = false;

        /// <summary>
        /// 网络连接配置
        /// </summary>
        [Description("网络连接配置（状态同步模式使用）")]
        public NetworkConfig? Network { get; set; }

        /// <summary>
        /// 玩家配置列表
        /// </summary>
        public List<PlayerConfig> Players { get; set; } = new();

        /// <summary>
        /// 获取配置（实现接口）
        /// </summary>
        BattleStartConfig IBattleStartConfigProvider.Config => this;

        int IBattleStartConfig.LocalPlayerId => DeterministicHash.StringToActorId(PlayerId);

        /// <summary>
        /// 构建战斗启动计划
        /// </summary>
        public BattleStartPlan BuildPlan()
        {
            return new BattleStartPlan(
                worldId: WorldId,
                worldType: WorldType,
                clientId: ClientId,
                playerId: PlayerId,
                tickRate: TickRate,
                inputDelayFrames: InputDelayFrames,
                syncMode: SyncMode,
                runMode: RunMode,
                enableDebug: EnableDebug,
                maxPlayerCount: MaxPlayerCount,
                enableInputRecording: EnableInputRecording,
                inputRecordOutputPath: InputRecordOutputPath,
                enableInputReplay: EnableInputReplay,
                inputReplayPath: InputReplayPath,
                enableClientPrediction: EnableClientPrediction,
                launchSpec: BuildLaunchSpec());
        }

        /// <summary>
        /// 构建框架层正式战斗启动规格
        /// </summary>
        public MobaBattleLaunchSpec BuildLaunchSpec()
        {
            return new MobaBattleLaunchSpec(
                battleId: WorldId,
                matchId: WorldId,
                worldId: WorldId,
                worldType: WorldType,
                clientId: ClientId,
                localPlayerId: new PlayerId(PlayerId),
                mapId: MapId,
                gameplayId: GameplayId,
                ruleSetId: RuleSetId,
                configVersion: ConfigVersion,
                protocolVersion: ProtocolVersion,
                randomSeed: RandomSeed,
                tickRate: TickRate,
                inputDelayFrames: InputDelayFrames,
                launchMode: MobaBattleLaunchMode.ConsoleSimulation,
                syncMode: ToLaunchSyncMode(SyncMode),
                authorityMode: EnableClientPrediction ? MobaBattleLaunchAuthorityMode.ClientPrediction : MobaBattleLaunchAuthorityMode.LocalAuthority,
                players: BuildPlayerLoadouts());
        }

        /// <summary>
        /// 创建默认配置
        /// </summary>
        public static BattleStartConfig CreateDefault()
        {
            // HeroId 需与 characters.json 中的 Id 对应
            var config = new BattleStartConfig
            {
                Name = "Default",
                Players = new List<PlayerConfig>
                {
                    new() { PlayerId = "player_1", TeamId = 1, HeroId = 1001, Name = "廉颇", PositionX = 0, PositionZ = 0 },
                    new() { PlayerId = "player_2", TeamId = 1, HeroId = 1002, Name = "小乔", PositionX = 10, PositionZ = 0 },
                    new() { PlayerId = "player_3", TeamId = 1, HeroId = 1003, Name = "赵云", PositionX = -10, PositionZ = 0 },
                    new() { PlayerId = "ai_1", TeamId = 2, HeroId = 1001, Name = "Enemy Warrior", PositionX = 0, PositionZ = 50 },
                    new() { PlayerId = "ai_2", TeamId = 2, HeroId = 1002, Name = "Enemy Archer", PositionX = 10, PositionZ = 50 },
                    new() { PlayerId = "ai_3", TeamId = 2, HeroId = 1003, Name = "Enemy Mage", PositionX = -10, PositionZ = 50 },
                }
            };
            return config;
        }

        private MobaPlayerLoadout[] BuildPlayerLoadouts()
        {
            if (Players == null || Players.Count == 0) return null;

            var loadouts = new MobaPlayerLoadout[Players.Count];
            for (int i = 0; i < Players.Count; i++)
            {
                var player = Players[i];
                loadouts[i] = new MobaPlayerLoadout(
                    playerId: new PlayerId(player.PlayerId),
                    teamId: player.TeamId,
                    heroId: player.HeroId,
                    attributeTemplateId: 0,
                    level: player.Level,
                    basicAttackSkillId: 0,
                    skillIds: null,
                    spawnIndex: i,
                    unitSubType: 1,
                    mainType: 1,
                    hasSpawnPosition: 1,
                    spawnX: player.PositionX,
                    spawnY: player.PositionY,
                    spawnZ: player.PositionZ);
            }

            return loadouts;
        }

        private static MobaBattleLaunchSyncMode ToLaunchSyncMode(ShareSyncMode syncMode)
        {
            return syncMode switch
            {
                ShareSyncMode.Lockstep => MobaBattleLaunchSyncMode.FrameSync,
                ShareSyncMode.SnapshotAuthority => MobaBattleLaunchSyncMode.StateSync,
                ShareSyncMode.Hybrid => MobaBattleLaunchSyncMode.Hybrid,
                _ => MobaBattleLaunchSyncMode.Unspecified,
            };
        }

        /// <summary>
        /// 创建调试配置
        /// </summary>
        public static BattleStartConfig CreateDebug()
        {
            var config = CreateDefault();
            config.EnableDebug = true;
            config.TickRate = 30;
            return config;
        }
    }

    /// <summary>
    /// 玩家配置
    /// </summary>
    public sealed class PlayerConfig
    {
        public string PlayerId { get; set; } = "";
        public string Name { get; set; } = "";
        public int TeamId { get; set; } = 1;
        public int HeroId { get; set; } = 1;
        public int Level { get; set; } = 1;
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }
    }
}
