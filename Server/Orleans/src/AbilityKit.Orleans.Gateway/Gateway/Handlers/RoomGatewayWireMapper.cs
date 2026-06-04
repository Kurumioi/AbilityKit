using AbilityKit.Orleans.Contracts.Accounts;
using AbilityKit.Orleans.Contracts.Rooms;
using AbilityKit.Protocol.Moba.Room;
using Orleans;

namespace AbilityKit.Orleans.Gateway.Handlers;

internal static class RoomGatewayWireMapper
{
    public static async Task<string?> ValidateAccountAsync(IClusterClient client, string sessionToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return null;
        }

        var session = client.GetGrain<ISessionGrain>("global");
        var result = await session.ValidateAsync(new ValidateSessionRequest(sessionToken));
        return result.IsValid && !string.IsNullOrWhiteSpace(result.AccountId) ? result.AccountId : null;
    }

    public static WireJoinRoomRes ToJoinRoomRes(RoomSnapshot snapshot, string message = "")
    {
        var roomId = snapshot.Summary?.RoomId ?? string.Empty;
        return new WireJoinRoomRes
        {
            Success = true,
            RoomId = roomId,
            NumericRoomId = RoomGatewayIds.CreateNumericRoomId(roomId),
            Snapshot = ToWireSnapshot(snapshot),
            WorldStartAnchor = default,
            Message = message ?? string.Empty
        };
    }

    public static WireRoomSnapshotRes ToSnapshotRes(RoomSnapshot snapshot, string message = "")
    {
        var roomId = snapshot.Summary?.RoomId ?? string.Empty;
        return new WireRoomSnapshotRes
        {
            Success = true,
            RoomId = roomId,
            NumericRoomId = RoomGatewayIds.CreateNumericRoomId(roomId),
            Snapshot = ToWireSnapshot(snapshot),
            Message = message ?? string.Empty
        };
    }

    public static WireRoomSnapshot ToWireSnapshot(RoomSnapshot snapshot)
    {
        return new WireRoomSnapshot
        {
            Summary = ToWireSummary(snapshot.Summary),
            Members = snapshot.Members == null ? null : new List<string>(snapshot.Members),
            Players = ToWirePlayers(snapshot.Players),
            CanStart = snapshot.CanStart,
            BattleId = snapshot.BattleId ?? string.Empty
        };
    }

    private static WireRoomSummary ToWireSummary(RoomSummary? summary)
    {
        if (summary == null)
        {
            return default;
        }

        return new WireRoomSummary
        {
            Region = summary.Region ?? string.Empty,
            ServerId = summary.ServerId ?? string.Empty,
            RoomId = summary.RoomId ?? string.Empty,
            RoomType = summary.RoomType ?? string.Empty,
            Title = summary.Title ?? string.Empty,
            IsPublic = summary.IsPublic,
            MaxPlayers = summary.MaxPlayers,
            PlayerCount = summary.PlayerCount,
            OwnerAccountId = summary.OwnerAccountId ?? string.Empty,
            CreatedAtUnixMs = summary.CreatedAtUnixMs,
            Tags = summary.Tags == null ? null : new Dictionary<string, string>(summary.Tags)
        };
    }

    private static List<WireRoomPlayerSnapshot>? ToWirePlayers(List<RoomPlayerSnapshot>? players)
    {
        if (players == null || players.Count == 0)
        {
            return players == null ? null : new List<WireRoomPlayerSnapshot>();
        }

        var result = new List<WireRoomPlayerSnapshot>(players.Count);
        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            result.Add(new WireRoomPlayerSnapshot
            {
                AccountId = player.AccountId ?? string.Empty,
                TeamId = player.TeamId,
                Ready = player.Ready,
                HeroId = player.HeroId,
                SpawnPointId = player.SpawnPointId,
                Level = player.Level,
                AttributeTemplateId = player.AttributeTemplateId,
                BasicAttackSkillId = player.BasicAttackSkillId,
                SkillIds = player.SkillIds == null ? null : new List<int>(player.SkillIds)
            });
        }

        return result;
    }
}
