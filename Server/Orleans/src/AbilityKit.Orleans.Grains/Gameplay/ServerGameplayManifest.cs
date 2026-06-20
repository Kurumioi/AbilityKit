using AbilityKit.Orleans.Contracts.Rooms;

namespace AbilityKit.Orleans.Grains.Gameplay;

internal sealed record ServerGameplayManifestEntry(
    string RoomType,
    string DisplayName,
    int DefaultMaxPlayers,
    bool RequiresPlayerLoadout,
    string? DefaultWorldType,
    int DefaultTickRate,
    string? DefaultSyncTemplateId,
    IReadOnlyList<string> SupportedSyncTemplateIds,
    bool SupportsStateSyncPush,
    bool SupportsFrameSync);

internal sealed record ServerGameplayManifest(IReadOnlyList<ServerGameplayManifestEntry> Entries)
{
    public ServerGameplayManifestEntry Resolve(string? roomType)
    {
        if (!string.IsNullOrWhiteSpace(roomType))
        {
            for (var i = 0; i < Entries.Count; i++)
            {
                if (string.Equals(Entries[i].RoomType, roomType, StringComparison.OrdinalIgnoreCase))
                {
                    return Entries[i];
                }
            }
        }

        return Entries[0];
    }

    public static ServerGameplayManifest FromCatalog(ServerGameplayModuleCatalog catalog)
    {
        if (catalog is null)
        {
            throw new ArgumentNullException(nameof(catalog));
        }

        var entries = new ServerGameplayManifestEntry[catalog.Modules.Count];
        for (var i = 0; i < catalog.Modules.Count; i++)
        {
            var module = catalog.Modules[i];
            var descriptor = module.Descriptor;
            var syncProfile = module.SyncProfile;
            entries[i] = new ServerGameplayManifestEntry(
                descriptor.RoomType,
                descriptor.DisplayName,
                descriptor.DefaultMaxPlayers,
                descriptor.RequiresPlayerLoadout,
                descriptor.DefaultWorldType,
                descriptor.DefaultTickRate,
                descriptor.DefaultSyncTemplateId,
                syncProfile.SupportedTemplateIds,
                syncProfile.SupportsStateSyncPush,
                syncProfile.SupportsFrameSync);
        }

        return new ServerGameplayManifest(entries);
    }
}
