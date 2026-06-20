using System.Threading.Tasks;
using AbilityKit.Orleans.Contracts.Accounts;
using AbilityKit.Orleans.Grains.Accounts;
using AbilityKit.Orleans.Grains.Persistence;
using Xunit;

namespace AbilityKit.Orleans.Grains.Tests.Accounts;

public sealed class SessionGrainTests
{
    [Fact]
    public async Task CreateGuestAsync_ShouldCreateValidSession()
    {
        var grain = CreateGrain();

        var response = await grain.CreateGuestAsync();

        Assert.False(string.IsNullOrWhiteSpace(response.AccountId));
        Assert.False(string.IsNullOrWhiteSpace(response.SessionToken));
        Assert.True(response.ExpireAtUnixMs > 0);

        var validation = await grain.ValidateAsync(new ValidateSessionRequest(response.SessionToken));
        Assert.True(validation.IsValid);
        Assert.Equal(response.AccountId, validation.AccountId);
    }

    [Fact]
    public async Task RenewAsync_ShouldRotateTokenWhenRequested()
    {
        var grain = CreateGrain();
        var login = await grain.CreateGuestAsync();

        var renewed = await grain.RenewAsync(new RenewSessionRequest(login.SessionToken, 3600, true));

        Assert.True(renewed.IsValid);
        Assert.NotEqual(login.SessionToken, renewed.SessionToken);
        Assert.True(renewed.ExpireAtUnixMs is not null and > 0);

        var oldValidation = await grain.ValidateAsync(new ValidateSessionRequest(login.SessionToken));
        Assert.False(oldValidation.IsValid);

        var newValidation = await grain.ValidateAsync(new ValidateSessionRequest(renewed.SessionToken!));
        Assert.True(newValidation.IsValid);
        Assert.Equal(login.AccountId, newValidation.AccountId);
    }

    [Fact]
    public async Task CreateSessionForAccountAsync_ShouldKickExistingSessionWhenRequested()
    {
        var grain = CreateGrain();
        var first = await grain.CreateSessionForAccountAsync(new CreateSessionForAccountRequest("account-a", 3600, false));

        var second = await grain.CreateSessionForAccountAsync(new CreateSessionForAccountRequest("account-a", 3600, true));

        Assert.NotEqual(first.SessionToken, second.SessionToken);
        Assert.Equal(first.SessionToken, second.KickedSessionToken);
        Assert.False((await grain.ValidateAsync(new ValidateSessionRequest(first.SessionToken))).IsValid);
        Assert.True((await grain.ValidateAsync(new ValidateSessionRequest(second.SessionToken))).IsValid);
    }

    [Fact]
    public async Task LogoutAsync_ShouldInvalidateSession()
    {
        var grain = CreateGrain();
        var login = await grain.CreateGuestAsync();

        var logout = await grain.LogoutAsync(new LogoutRequest(login.SessionToken));

        Assert.True(logout.Success);
        Assert.False((await grain.ValidateAsync(new ValidateSessionRequest(login.SessionToken))).IsValid);
    }

    private static SessionGrain CreateGrain()
    {
        return new SessionGrain(new InMemorySessionStateStore());
    }
}
