using Orleans;

namespace AbilityKit.Orleans.Contracts.Battle;

/// <summary>
/// Battle Logic Host Grain 接口
/// 在服务器端运行战斗逻辑，生成状态快照
/// </summary>
public interface IBattleLogicHostGrain : IGrainWithStringKey
{
    /// <summary>
    /// 初始化战斗世界
    /// </summary>
    Task InitializeBattleAsync(BattleInitParams initParams);

    /// <summary>
    /// 提交玩家输入
    /// </summary>
    Task SubmitInputAsync(ulong worldId, int frame, BattleInputItem input);

    /// <summary>
    /// 获取当前帧
    /// </summary>
    Task<int> GetCurrentFrameAsync();

    /// <summary>
    /// 获取快照（用于调试）
    /// </summary>
    Task<BattleSnapshot?> GetSnapshotAsync();

    /// <summary>
    /// 订阅状态同步观察者
    /// </summary>
    Task SubscribeAsync(IStateSyncObserver observer);

    /// <summary>
    /// 取消订阅状态同步观察者
    /// </summary>
    Task UnsubscribeAsync(IStateSyncObserver observer);

    /// <summary>
    /// 销毁战斗世界
    /// </summary>
    Task DestroyAsync();
}

/// <summary>
/// 战斗初始化参数
/// </summary>
[GenerateSerializer]
public class BattleInitParams
{
    [Id(0)] public ulong WorldId { get; set; }
    [Id(1)] public int TickRate { get; set; }
    [Id(2)] public List<PlayerInitInfo>? Players { get; set; }
    [Id(3)] public int MapId { get; set; } = 1;
    [Id(4)] public int GameplayId { get; set; }
    [Id(5)] public int RuleSetId { get; set; }
    [Id(6)] public int ConfigVersion { get; set; }
    [Id(7)] public int ProtocolVersion { get; set; }
    [Id(8)] public int RandomSeed { get; set; }
    [Id(9)] public int InputDelayFrames { get; set; }
    [Id(10)] public string? WorldType { get; set; }
    [Id(11)] public string? ClientId { get; set; }
    [Id(12)] public string? RoomType { get; set; }
}

/// <summary>
/// 玩家初始化信息
/// </summary>
[GenerateSerializer]
public class PlayerInitInfo
{
    [Id(0)] public uint PlayerId { get; set; }
    [Id(1)] public int ActorId { get; set; }
    [Id(2)] public int HeroId { get; set; }
    [Id(3)] public float PosX { get; set; }
    [Id(4)] public float PosY { get; set; }
    [Id(5)] public float PosZ { get; set; }
    [Id(6)] public int TeamId { get; set; }
}

/// <summary>
/// 战斗输入项
/// </summary>
[GenerateSerializer]
public class BattleInputItem
{
    [Id(0)] public uint PlayerId { get; set; }
    [Id(1)] public int OpCode { get; set; }
    [Id(2)] public byte[]? Payload { get; set; }
}

/// <summary>
/// 战斗快照
/// </summary>
[GenerateSerializer]
public class BattleSnapshot
{
    [Id(0)] public int Frame { get; set; }
    [Id(1)] public List<ActorSnapshot> Actors { get; set; } = new();
}

/// <summary>
/// Actor 快照
/// </summary>
[GenerateSerializer]
public class ActorSnapshot
{
    [Id(0)] public int ActorId { get; set; }
    [Id(1)] public float X { get; set; }
    [Id(2)] public float Y { get; set; }
    [Id(3)] public float Z { get; set; }
    [Id(4)] public float Rotation { get; set; }
    [Id(5)] public float VelocityX { get; set; }
    [Id(6)] public float VelocityZ { get; set; }
    [Id(7)] public float Hp { get; set; }
    [Id(8)] public float HpMax { get; set; }
    [Id(9)] public int TeamId { get; set; }
}

/// <summary>
/// 状态同步观察者接口
/// 用于推送服务器快照到客户端
/// </summary>
public interface IStateSyncObserver
{
    /// <summary>
    /// 推送快照
    /// </summary>
    void OnSnapshotPushed(StateSyncPush push);
}

/// <summary>
/// 状态同步推送
/// </summary>
[GenerateSerializer]
public class StateSyncPush
{
    [Id(0)] public ulong WorldId { get; set; }
    [Id(1)] public int Frame { get; set; }
    [Id(2)] public double Timestamp { get; set; }
    [Id(3)] public List<ActorSnapshot> Actors { get; set; } = new();

    /// <summary>
    /// 是否为全量快照（vs 增量快照）
    /// </summary>
    [Id(4)] public bool IsFullSnapshot { get; set; } = true;
}
