namespace AbilityKit.Orleans.Gateway.HttpApi;

public sealed record AbilityKitGatewayDeploymentDiagnostics(
    string Role,
    bool IsGateway,
    bool IsExclusive,
    IReadOnlyList<string> LogicalGroups,
    IReadOnlyList<string> PreferredAffinity);
