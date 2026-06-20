namespace AbilityKit.Orleans.Hosting;

public static class AbilityKitDeploymentConfigurationSections
{
    public const string Deployment = AbilityKitServerConfigurationSections.Root + ":Deployment";
    public const string DeploymentRole = Deployment + ":Role";
    public const string DeploymentAffinity = Deployment + ":Affinity";
    public const string DeploymentGroups = Deployment + ":Groups";
}
