using System.Threading.Tasks;
using AbilityKit.Orleans.Contracts.Accounts;
using AbilityKit.Orleans.Gateway.Gateway.Abstractions;
using AbilityKit.Orleans.Gateway.Gateway.Handlers;
using AbilityKit.Orleans.Grains.Accounts;
using AbilityKit.Orleans.Grains.Persistence;
using Xunit;

namespace AbilityKit.Orleans.Gateway.Tests;

public sealed class RestoreRoomHandlerTests
{
    [Fact]
    public async Task RestoreRoom_should_bind_account_to_room_when_session_is_valid()
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
