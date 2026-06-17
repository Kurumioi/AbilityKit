using AbilityKit.Orleans.Contracts.Battle;
using AbilityKit.Orleans.Contracts.Rooms;

namespace AbilityKit.Orleans.Grains.Rooms;

internal static class RoomBattleSyncOptionsMapper
{
    public static BattleSyncStartOptions Resolve(RoomSummary summary, StartRoomBattleRequest request)
    {
        var requested = request.SyncOptions;
        return new BattleSyncStartOptions(
            FirstNonEmpty(requested?.SyncTemplateId, ReadTag(summary, "syncTemplateId")),
            requested?.SyncModel ?? ReadIntTag(summary, "syncModel", 0),
            FirstNonEmpty(requested?.NetworkEnvironmentId, ReadTag(summary, "networkEnvironmentId")),
            FirstNonEmpty(requested?.CarrierName, ReadTag(summary, "carrierName")),
            requested?.EnableAuthoritativeWorld ?? ReadBoolTag(summary, "enableAuthoritativeWorld", true),
            requested?.InterpolationEnabled ?? ReadBoolTag(summary, "interpolationEnabled", false),
            requested?.InputDelayFrames ?? ReadIntTag(summary, "inputDelayFrames", 0));
    }

    private static string? FirstNonEmpty(string? first, string? second)
    {
        return string.IsNullOrWhiteSpace(first) ? second : first;
    }

    private static string? ReadTag(RoomSummary summary, string key)
    {
        return summary.Tags != null && summary.Tags.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
    }

    private static int ReadIntTag(RoomSummary summary, string key, int fallback)
    {
        return summary.Tags != null && summary.Tags.TryGetValue(key, out var value) && int.TryParse(value, out var parsed)
            ? parsed
            : fallback;
    }

    private static bool ReadBoolTag(RoomSummary summary, string key, bool fallback)
    {
        return summary.Tags != null && summary.Tags.TryGetValue(key, out var value) && bool.TryParse(value, out var parsed)
            ? parsed
            : fallback;
    }
}
