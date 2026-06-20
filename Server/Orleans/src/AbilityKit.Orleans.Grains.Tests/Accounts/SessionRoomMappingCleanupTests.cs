using System.Threading.Tasks;
using AbilityKit.Orleans.Contracts.Accounts;
using AbilityKit.Orleans.Grains.Accounts;
using AbilityKit.Orleans.Grains.Persistence;
using Xunit;

namespace AbilityKit.Orleans.Grains.Tests.Accounts;

public sealed class SessionRoomMappingCleanupTests
{
    [Fact]
    public async Task CreateSessionForAccountAsync_ShouldKeepRoomMappingIndependentFromSessionLifecycle()
    {
        var sessionGrain = new SessionGrain(new InMemorySessionStateStore());
        var login = await sessionGrain.CreateSessionForAccountAsync(new CreateSessionForAccountRequest("account-a", 3600, false));

        var roomStore = new InMemoryRoomStateStore();
        await roomStore.BindAccountRoomAsync("account-a", "room-a");

        var validation = await sessionGrain.ValidateAsync(new ValidateSessionRequest(login.SessionToken));

        Assert.True(validation.IsValid);
        Assert.Equal("account-a", validation.AccountId);
        Assert.Equal("room-a", await roomStore.TryGetAccountRoomAsync("account-a"));
    }
}
