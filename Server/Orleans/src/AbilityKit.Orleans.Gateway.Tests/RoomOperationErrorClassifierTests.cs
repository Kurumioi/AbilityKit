using AbilityKit.Orleans.Gateway;
using Xunit;

namespace AbilityKit.Orleans.Gateway.Tests;

public sealed class RoomOperationErrorClassifierTests : GatewayTestBase
{
    [Theory]
    [InlineData("Room is full", RoomGatewayErrorCodes.RoomFull, 409, GatewayStatusCode.Conflict)]
    [InlineData("Room is closed", RoomGatewayErrorCodes.RoomClosed, 409, GatewayStatusCode.Conflict)]
    [InlineData("Account is not in room", RoomGatewayErrorCodes.AccountNotInRoom, 403, GatewayStatusCode.Forbidden)]
    [InlineData("Only owner can start battle", RoomGatewayErrorCodes.OwnerRequired, 403, GatewayStatusCode.Forbidden)]
    [InlineData("Unsupported MOBA room gameplay command", RoomGatewayErrorCodes.InvalidGameplayCommand, 400, GatewayStatusCode.BadRequest)]
    public void ToError_WhenInvalidOperationMatchesKnownRoomFailure_MapsHttpAndGatewayStatus(
        string message,
        string expectedCode,
        int expectedHttpStatusCode,
        int expectedGatewayStatusCode)
    {
        var error = RoomOperationErrorClassifier.ToError(new InvalidOperationException(message));

        AssertErrorMapping(error, expectedCode, message, expectedHttpStatusCode, expectedGatewayStatusCode);
    }

    [Fact]
    public void ToError_WhenArgumentException_ReturnsBadRequestForBothTransports()
    {
        var error = RoomOperationErrorClassifier.ToError(new ArgumentException("payload is invalid"));

        AssertErrorMapping(
            error,
            RoomGatewayErrorCodes.BadRequest,
            "payload is invalid",
            400,
            GatewayStatusCode.BadRequest);
    }

    [Fact]
    public void ToError_WhenUnknownException_ReturnsInternalErrorForBothTransports()
    {
        var error = RoomOperationErrorClassifier.ToError(new Exception("boom"));

        AssertErrorMapping(
            error,
            RoomGatewayErrorCodes.InternalError,
            "Room operation failed.",
            500,
            GatewayStatusCode.InternalError);
    }
}
