namespace AbilityKit.Orleans.Hosting;

public static class AbilityKitDeploymentConfigurationSections
{
    public const string Deployment = AbilityKitServerConfigurationSections.Root + ":Deployment";
    public const string DeploymentRole = Deployment + ":SiloRole";
    public const string DeploymentAffinity = Deployment + ":RuntimeProfile";
    public const string DeploymentGroups = Deployment + ":Groups";
}
