using Microsoft.Extensions.DependencyInjection;

namespace AbilityKit.Orleans.Grains.Persistence;

public static class AbilityKitGrainStateStorageServiceCollectionExtensions
{
    public static IServiceCollection AddAbilityKitGrainStateStorage(
        this IServiceCollection services,
        string sessionStateProvider,
        string roomStateProvider,
        bool allowInMemoryFallbackForUnsupportedProviders = true)
    {
        services.AddAbilityKitSessionStateStorage(sessionStateProvider, allowInMemoryFallbackForUnsupportedProviders);
        services.AddAbilityKitRoomStateStorage(roomStateProvider, allowInMemoryFallbackForUnsupportedProviders);
        return services;
    }

    public static IServiceCollection AddAbilityKitSessionStateStorage(
        this IServiceCollection services,
        string sessionStateProvider,
        bool allowInMemoryFallbackForUnsupportedProviders = true)
    {
        var plan = AbilityKitStateStorageProviderPlan.Create(sessionStateProvider, "Session");
        if (plan.IsInMemory || allowInMemoryFallbackForUnsupportedProviders)
        {
            services.AddSingleton<ISessionStateStore, InMemorySessionStateStore>();
            return services;
        }

        throw new NotSupportedException($"{plan.RegistrationMessage} Register an ISessionStateStore implementation before enabling provider '{sessionStateProvider}'.");
    }

    public static IServiceCollection AddAbilityKitRoomStateStorage(
        this IServiceCollection services,
        string roomStateProvider,
        bool allowInMemoryFallbackForUnsupportedProviders = true)
    {
        var plan = AbilityKitStateStorageProviderPlan.Create(roomStateProvider, "Room");
        if (plan.IsInMemory || allowInMemoryFallbackForUnsupportedProviders)
        {
            services.AddSingleton<IRoomStateStore, InMemoryRoomStateStore>();
            return services;
        }

        throw new NotSupportedException($"{plan.RegistrationMessage} Register an IRoomStateStore implementation before enabling provider '{roomStateProvider}'.");
    }

    public static IServiceCollection AddAbilityKitInMemoryGrainStateStorage(this IServiceCollection services)
    {
        services.AddSingleton<ISessionStateStore, InMemorySessionStateStore>();
        services.AddSingleton<IRoomStateStore, InMemoryRoomStateStore>();
        return services;
    }

}
