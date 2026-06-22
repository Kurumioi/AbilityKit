namespace AbilityKit.Orleans.Gateway.HttpApi;

internal static class GatewayGameplayCatalog
{
    public static readonly GameplayHttpDescriptor[] All =
    {
        new(
            "moba",
            1,
            "MOBA Battle",
            10,
            true,
            "moba",
            30,
            "frame-sync-authority",
            new[] { "frame-sync-authority", "state-sync-authority" },
            SupportsStateSyncPush: true,
            SupportsFrameSync: true),
        new(
            "shooter",
            2,
            "Shooter State Sync",
            4,
            false,
            "shooter_battle",
            30,
            "pure-state-authority",
            new[] { "pure-state-authority", "batch-state-sync", "mass-battle-lod", "hybrid-hero-prediction" },
            SupportsStateSyncPush: true,
            SupportsFrameSync: false)
    };

    public static GameplayHttpDescriptor Resolve(string? roomType)
    {
        if (!string.IsNullOrWhiteSpace(roomType))
        {
            foreach (var gameplay in All)
            {
                if (string.Equals(gameplay.RoomType, roomType, StringComparison.OrdinalIgnoreCase))
                {
                    return gameplay;
                }
            }
        }

        return All[0];
    }
}
