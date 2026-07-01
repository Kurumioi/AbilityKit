namespace AbilityKit.Orleans.Hosting;

public sealed record AbilityKitOrleansClusterOptions
{
    public const string DefaultClusterId = "abilitykit-dev";
    public const string DefaultServiceId = "abilitykit-orleans";

    public string ClusterId { get; init; } = DefaultClusterId;

    public string ServiceId { get; init; } = DefaultServiceId;

    public int? SiloPort { get; init; }

    public int? GatewayPort { get; init; }

    public int? PrimarySiloPort { get; init; }

    public static AbilityKitOrleansClusterOptions Development { get; } = new();
}
