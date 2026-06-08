using System;
using System.Collections.Generic;
using System.Linq;

namespace AbilityKit.Orleans.Grains.Rooms.Gameplay;

internal sealed class RoomGameplayRegistry
{
    private readonly Dictionary<string, IRoomGameplayAdapter> _adapters;
    private readonly IRoomGameplayAdapter _defaultAdapter;

    public RoomGameplayRegistry()
        : this(
            new IRoomGameplayAdapter[]
            {
                new MobaRoomGameplayAdapter(),
                new ShooterRoomGameplayAdapter()
            },
            defaultRoomType: MobaRoomGameplayAdapter.DefaultRoomType)
    {
    }

    public RoomGameplayRegistry(IEnumerable<IRoomGameplayAdapter> adapters, string defaultRoomType)
    {
        if (adapters is null)
        {
            throw new ArgumentNullException(nameof(adapters));
        }

        _adapters = adapters.ToDictionary(a => a.RoomType, StringComparer.OrdinalIgnoreCase);
        if (!_adapters.TryGetValue(defaultRoomType, out _defaultAdapter!))
        {
            throw new InvalidOperationException($"Default room gameplay adapter is not registered. RoomType={defaultRoomType}");
        }
    }

    public IRoomGameplayAdapter Resolve(string? roomType)
    {
        if (!string.IsNullOrWhiteSpace(roomType) && _adapters.TryGetValue(roomType, out var adapter))
        {
            return adapter;
        }

        return _defaultAdapter;
    }
}
