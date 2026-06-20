namespace AbilityKit.Orleans.Hosting;

public sealed record AbilityKitDeploymentModeOptions
{
    public string Mode { get; init; } = "Shared";

    public bool PreferSharedDuringDevelopment { get; init; } = true;

    public bool ForceDedicatedRolesInProduction { get; init; } = false;

    public string[] EnabledRoles { get; init; } = ["Shared"];
}
