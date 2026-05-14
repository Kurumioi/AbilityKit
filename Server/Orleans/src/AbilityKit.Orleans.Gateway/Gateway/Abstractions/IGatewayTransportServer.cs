namespace AbilityKit.Orleans.Gateway.Abstractions;

/// <summary>
/// Gateway 传输层服务器接口
/// </summary>
public interface IGatewayTransportServer
{
    /// <summary>
    /// 服务器名称（TCP、KCP 等）
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 是否启用
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// 启动服务器
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止服务器
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 传输层配置基类
/// </summary>
public abstract class GatewayTransportOptions
{
    public bool Enabled { get; set; } = true;
    public string Host { get; set; } = "0.0.0.0";
    public int Port { get; set; }
}
