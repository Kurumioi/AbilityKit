namespace AbilityKit.Orleans.Gateway.Core;

/// <summary>
/// Gateway 配置选项
/// </summary>
public sealed class GatewayOptions
{
    public int RequestTimeoutMs { get; set; } = 30000;
    public int MaxFrameLength { get; set; } = 1024 * 1024;
}
