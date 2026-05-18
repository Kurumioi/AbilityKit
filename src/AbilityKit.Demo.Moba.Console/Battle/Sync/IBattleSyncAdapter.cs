using System;
using AbilityKit.Demo.Moba.Console.Core.Battle.Context;
using AbilityKit.Demo.Moba.Share;
using ShareSyncMode = AbilityKit.Demo.Moba.Share.SyncMode;

namespace AbilityKit.Demo.Moba.Console.Battle.Sync;

/// <summary>
/// 战斗同步模式（已废弃，使用 Share.SyncMode）
/// </summary>
[Obsolete("使用 AbilityKit.Demo.Moba.Share.SyncMode 替代")]
public enum SyncMode
{
    /// <summary>
    /// 帧同步 - 本地运行逻辑，客户端之间同步输入
    /// </summary>
    Lockstep = 0,

    /// <summary>
    /// 状态同步 - 服务器运行逻辑，客户端接收状态快照
    /// </summary>
    StateSync = 1,

    /// <summary>
    /// 混合模式 - 帧同步 + 客户端预测 + 回滚
    /// </summary>
    Hybrid = 2
}

/// <summary>
/// 战斗同步适配器接口
/// 抽象帧同步和状态同步两种模式，统一战斗同步行为
/// </summary>
public interface IBattleSyncAdapter : IDisposable
{
    /// <summary>
    /// 当前同步模式
    /// </summary>
    SyncMode Mode { get; }

    /// <summary>
    /// 是否已连接
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 当前逻辑帧
    /// </summary>
    int CurrentFrame { get; }

    /// <summary>
    /// 逻辑时间（秒）
    /// </summary>
    double LogicTimeSeconds { get; }

    /// <summary>
    /// 渲染时间（秒）- 用于视图插值
    /// </summary>
    double RenderTimeSeconds { get; }

    /// <summary>
    /// 本地玩家 ActorId
    /// </summary>
    int LocalActorId { get; }

    /// <summary>
    /// 连接状态变更事件
    /// </summary>
    event Action<bool> OnConnectionChanged;

    /// <summary>
    /// 帧同步事件（每帧触发）
    /// </summary>
    event Action<int, double> OnFrameSync;

    /// <summary>
    /// 实体状态同步事件（当接收服务器快照时触发）
    /// </summary>
    event Action<ActorStateSnapshot[]> OnActorStateSnapshot;

    /// <summary>
    /// 初始化同步适配器
    /// </summary>
    /// <param name="context">战斗上下文</param>
    /// <param name="config">启动配置</param>
    void Initialize(ConsoleBattleContext context, BattleStartConfig config);

    /// <summary>
    /// 连接到远程服务器（状态同步模式）
    /// </summary>
    /// <param name="host">服务器地址</param>
    /// <param name="port">端口</param>
    /// <param name="roomId">房间ID</param>
    /// <param name="playerId">玩家ID</param>
    void Connect(string host, int port, string roomId, string playerId);

    /// <summary>
    /// 断开连接
    /// </summary>
    void Disconnect();

    /// <summary>
    /// 提交本地输入
    /// </summary>
    /// <param name="input">输入数据</param>
    void SubmitInput(PlayerInput input);

    /// <summary>
    /// 每帧更新
    /// </summary>
    /// <param name="deltaTime">帧间隔（秒）</param>
    void Tick(float deltaTime);

    /// <summary>
    /// 获取所有实体的当前状态（用于视图渲染）
    /// </summary>
    ActorStateSnapshot[] GetAllActorStates();
}

/// <summary>
/// 玩家输入数据
/// </summary>
public sealed class PlayerInput
{
    public int OpCode { get; set; }
    public byte[] Payload { get; set; } = Array.Empty<byte>();
}

/// <summary>
/// Actor 状态快照
/// </summary>
public sealed class ActorStateSnapshot
{
    public int ActorId { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float Rotation { get; set; }
    public float VelocityX { get; set; }
    public float VelocityZ { get; set; }
    public float Hp { get; set; }
    public float HpMax { get; set; }
    public int TeamId { get; set; }
}
