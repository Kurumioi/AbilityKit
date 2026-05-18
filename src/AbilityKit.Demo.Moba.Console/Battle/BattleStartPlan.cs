using System;
using AbilityKit.Demo.Moba.Share;

namespace AbilityKit.Demo.Moba.Console.Battle
{
    /// <summary>
    /// Console 层战斗启动计划
    /// 与 Share 层的 BattleStartPlan 不同：
    /// - 使用字符串类型的 ID（WorldId, ClientId, PlayerId）
    /// - 添加了额外的配置字段（EnableInputRecording, InputRecordOutputPath 等）
    ///
    /// 注意：此类型仅用于 Console 内部，Share 层交互使用 Share.BattleStartPlan
    /// </summary>
    public readonly struct BattleStartPlan
    {
        public string WorldId { get; init; }
        public string WorldType { get; init; }
        public string ClientId { get; init; }
        public string PlayerId { get; init; }

        public int TickRate { get; init; }
        public int InputDelayFrames { get; init; }

        /// <summary>
        /// 同步模式（使用 Share 层类型）
        /// </summary>
        public SyncMode SyncMode { get; init; }

        /// <summary>
        /// 运行模式 Normal / Record / Replay
        /// </summary>
        public RunMode RunMode { get; init; }

        /// <summary>
        /// 是否启用调试
        /// </summary>
        public bool EnableDebug { get; init; }

        /// <summary>
        /// 最大玩家数
        /// </summary>
        public int MaxPlayerCount { get; init; }

        /// <summary>
        /// 是否启用输入录制
        /// </summary>
        public bool EnableInputRecording { get; init; }

        /// <summary>
        /// 输入录制输出路径
        /// </summary>
        public string InputRecordOutputPath { get; init; }

        /// <summary>
        /// 是否启用输入回放
        /// </summary>
        public bool EnableInputReplay { get; init; }

        /// <summary>
        /// 输入回放路径
        /// </summary>
        public string InputReplayPath { get; init; }

        /// <summary>
        /// 是否启用客户端预测
        /// </summary>
        public bool EnableClientPrediction { get; init; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public BattleStartPlan(
            string worldId = "room_1",
            string worldType = "MobaBattle",
            string clientId = "console_client",
            string playerId = "player_1",
            int tickRate = 30,
            int inputDelayFrames = 2,
            SyncMode syncMode = SyncMode.Lockstep,
            RunMode runMode = RunMode.Normal,
            bool enableDebug = true,
            int maxPlayerCount = 10,
            bool enableInputRecording = false,
            string inputRecordOutputPath = "",
            bool enableInputReplay = false,
            string inputReplayPath = "",
            bool enableClientPrediction = false)
        {
            WorldId = worldId;
            WorldType = worldType;
            ClientId = clientId;
            PlayerId = playerId;
            TickRate = tickRate;
            InputDelayFrames = inputDelayFrames;
            SyncMode = syncMode;
            RunMode = runMode;
            EnableDebug = enableDebug;
            MaxPlayerCount = maxPlayerCount;
            EnableInputRecording = enableInputRecording;
            InputRecordOutputPath = inputRecordOutputPath;
            EnableInputReplay = enableInputReplay;
            InputReplayPath = inputReplayPath;
            EnableClientPrediction = enableClientPrediction;
        }

        /// <summary>
        /// 创建默认计划
        /// </summary>
        public static BattleStartPlan CreateDefault()
        {
            return new BattleStartPlan();
        }

        /// <summary>
        /// 创建调试计划
        /// </summary>
        public static BattleStartPlan CreateDebug()
        {
            return new BattleStartPlan(enableDebug: true);
        }

        /// <summary>
        /// 转换为 Share 层的 BattleStartPlan
        /// </summary>
        public Share.BattleStartPlan ToSharePlan()
        {
            // Share.BattleStartPlan 是一个 class，通过构造函数创建
            return new Share.BattleStartPlan(
                mapId: 0,
                worldId: 0, // Console 使用字符串 ID，无法直接转换
                playerId: 0,
                clientId: 0,
                syncMode: SyncMode,
                hostMode: Share.HostMode.Local,
                tickRate: TickRate,
                useGatewayTransport: false,
                enableConfirmedAuthorityWorld: false,
                enableReplayRecording: EnableInputRecording,
                enableReplayPlayback: EnableInputReplay,
                playerIds: Array.Empty<int>());
        }
    }
}
