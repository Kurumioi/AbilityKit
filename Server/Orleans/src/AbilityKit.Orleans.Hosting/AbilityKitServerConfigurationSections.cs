namespace AbilityKit.Orleans.Hosting;

public static class AbilityKitServerConfigurationSections
{
    public const string Root = "AbilityKit";
    public const string Orleans = Root + ":Orleans";
    public const string Logging = Root + ":Logging";
    public const string Storage = Root + ":Storage";
    public const string Gateway = Root + ":Gateway";
    public const string LegacyOrleans = "Orleans";
    public const string LegacyTcpGateway = "TcpGateway";
}
