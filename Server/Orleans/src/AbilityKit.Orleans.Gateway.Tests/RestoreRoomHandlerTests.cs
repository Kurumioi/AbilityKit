using AbilityKit.Orleans.Contracts.Accounts;
using AbilityKit.Orleans.Contracts.Rooms;
using Xunit;

namespace AbilityKit.Orleans.Gateway.Tests;

public sealed class RestoreRoomHandlerTests
{
    [Fact]
    public void RestoreRoom_flow_contract_should_preserve_valid_session_room_binding()
    {
        var accountId = "account-a";
        var login = new CreateSessionForAccountResponse("session-a", 3600, null);
        var validation = new ValidateSessionResponse(true, accountId, login.ExpireAtUnixMs);
        var response = RestoreRoomResponse.Active(
            new RoomSnapshot(
                new RoomSummary("cn", "server-a", "room-a", "moba", "Room A", true, 5, 1, accountId, 1, null),
                new List<string> { accountId },
                new List<RoomPlayerSnapshot>
                {
                    new(accountId, 1, true, 1001, 1, 1, 1, 1, null, 1u)
                },
                true,
                null,
                null,
                1,
                new Dictionary<string, RoomMemberState>
                {
                    [accountId] = new(true, 1, 0)
                }),
            RoomJoinKind.Reconnect,
            false,
            1);

        Assert.True(validation.IsValid);
        Assert.Equal(accountId, validation.AccountId);
        Assert.Equal("room-a", response.Snapshot.Summary.RoomId);
        Assert.Equal(RoomJoinKind.Reconnect, response.JoinKind);
        Assert.Equal(RoomRestoreStatus.Restored, response.Status);
        Assert.Equal(RoomRestoreErrorCode.None, response.ErrorCode);
    }
}
