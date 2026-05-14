using System;
using System.Collections.Generic;

using AbilityKit.Game.Battle.Transport.Moba.Client;

namespace AbilityKit.Demo.Moba.Console.Battle
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
        public BattleSyncMode SyncMode { get; set; } = BattleSyncMode.Lockstep;

        /// <summary>
        /// 运行模式
        /// </summary>
        public BattleRunMode RunMode { get; set; } = BattleRunMode.Normal;

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
        /// 玩家配置列表
        /// </summary>
        public List<PlayerConfig> Players { get; set; } = new();

        /// <summary>
        /// 获取配置（实现接口）
        /// </summary>
        BattleStartConfig IBattleStartConfigProvider.Config => this;

        int IBattleStartConfig.LocalPlayerId => HashPlayerId(PlayerId);

        private static int HashPlayerId(string playerId)
        {
            return playerId.GetHashCode() & 0xFFFF;
        }

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
                enableClientPrediction: EnableClientPrediction);
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
