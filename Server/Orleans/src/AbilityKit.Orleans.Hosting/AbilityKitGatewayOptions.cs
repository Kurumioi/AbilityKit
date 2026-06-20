namespace AbilityKit.Orleans.Hosting;

public sealed record AbilityKitGatewayOptions
{
    public AbilityKitGatewayHttpOptions Http { get; init; } = new();
}

public sealed record AbilityKitGatewayHttpOptions
{
    public string Scheme { get; init; } = "http";

    public string Host { get; init; } = "localhost";

    public int Port { get; init; } = 5001;

    public string PathBase { get; init; } = string.Empty;

    public string Url => $"{Scheme}://{Host}:{Port}";

    public string NormalizedPathBase => string.IsNullOrWhiteSpace(PathBase)
        ? string.Empty
        : "/" + PathBase.Trim('/');
}
