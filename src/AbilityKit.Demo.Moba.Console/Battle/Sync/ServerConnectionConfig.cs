namespace AbilityKit.Demo.Moba.Console.Battle.Sync;

/// <summary>
/// 服务器连接配置
/// </summary>
public sealed class ServerConnectionConfig
{
    /// <summary>
    /// 服务器地址
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// 服务器端口
    /// </summary>
    public int Port { get; set; } = 4000;

    /// <summary>
    /// 房间 ID
    /// </summary>
    public string RoomId { get; set; } = string.Empty;

    /// <summary>
    /// 玩家 ID
    /// </summary>
    public string PlayerId { get; set; } = string.Empty;

    /// <summary>
    /// 重连间隔（毫秒）
    /// </summary>
    public int ReconnectIntervalMs { get; set; } = 3000;

    /// <summary>
    /// 最大重连次数
    /// </summary>
    public int MaxReconnectAttempts { get; set; } = 5;

    public static ServerConnectionConfig FromBattleConfig(BattleStartConfig config)
    {
        return new ServerConnectionConfig
        {
            Host = config.Network?.Host ?? "localhost",
            Port = config.Network?.Port ?? 4000,
            RoomId = config.WorldId ?? string.Empty,
            PlayerId = config.PlayerId ?? string.Empty,
            ReconnectIntervalMs = config.Network?.ReconnectIntervalMs ?? 3000,
            MaxReconnectAttempts = config.Network?.MaxReconnectAttempts ?? 5
        };
    }
}
