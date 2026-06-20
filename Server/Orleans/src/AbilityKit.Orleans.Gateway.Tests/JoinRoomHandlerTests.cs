using System.Threading.Tasks;
using AbilityKit.Orleans.Contracts.Accounts;
using AbilityKit.Orleans.Gateway.Gateway.Handlers;
using AbilityKit.Orleans.Grains.Accounts;
using AbilityKit.Orleans.Grains.Persistence;
using Xunit;

namespace AbilityKit.Orleans.Gateway.Tests;

public sealed class JoinRoomHandlerTests
{
    [Fact]
    public async Task JoinRoom_flow_should_preserve_account_session_and_room_binding()
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
