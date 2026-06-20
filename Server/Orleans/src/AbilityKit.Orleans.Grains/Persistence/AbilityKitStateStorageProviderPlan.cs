namespace AbilityKit.Orleans.Grains.Persistence;

public enum AbilityKitStateStorageProviderKind
{
    InMemory = 0,
    External = 1
}

public sealed record AbilityKitStateStorageProviderPlan(
    string ProviderName,
    AbilityKitStateStorageProviderKind Kind,
    bool RequiresConnectionString,
    string RegistrationMessage)
{
    public bool IsInMemory => Kind == AbilityKitStateStorageProviderKind.InMemory;

    public bool IsExternal => Kind == AbilityKitStateStorageProviderKind.External;

    public static AbilityKitStateStorageProviderPlan Create(string providerName, string stateName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            throw new ArgumentException($"{stateName} state provider is required.", nameof(providerName));
        }

        if (IsInMemoryProvider(providerName))
        {
            return new AbilityKitStateStorageProviderPlan(
                providerName,
                AbilityKitStateStorageProviderKind.InMemory,
                RequiresConnectionString: false,
                RegistrationMessage: $"{stateName} state uses in-memory storage.");
        }

        if (IsExternalProvider(providerName))
        {
            return new AbilityKitStateStorageProviderPlan(
                providerName,
                AbilityKitStateStorageProviderKind.External,
                RequiresConnectionString: true,
                RegistrationMessage: $"{stateName} state requires an external storage implementation.");
        }

        throw new NotSupportedException($"{stateName} state provider '{providerName}' is not supported yet.");
    }

    private static bool IsInMemoryProvider(string provider)
    {
        return string.Equals(provider, "InMemory", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(provider, "None", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsExternalProvider(string provider)
    {
        return string.Equals(provider, "External", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(provider, "Redis", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(provider, "PostgreSql", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase);
    }
}
