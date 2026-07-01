namespace AbilityKit.Orleans.Hosting;

public sealed record AbilityKitSiloRuntimeProfileOptions
{
    public string Role { get; init; } = "Shared";

    public string[] LogicalGroups { get; init; } = [];

    public string[] PreferredAffinity { get; init; } = [];

    public bool IsExclusive { get; init; }

    public bool IsGateway { get; init; }

    public int MaxRooms { get; init; } = 0;

    public int MaxBattles { get; init; } = 0;

    public int MaxSessions { get; init; } = 0;

    public string? Notes { get; init; }
}
