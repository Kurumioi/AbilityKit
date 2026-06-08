using System;
using System.Collections.Generic;
using System.Linq;

namespace AbilityKit.Orleans.Grains.Battle.Gameplay;

internal sealed class BattleRuntimeRegistry
{
    private readonly Dictionary<string, IBattleRuntimeAdapter> _adapters;
    private readonly IBattleRuntimeAdapter _defaultAdapter;

    public BattleRuntimeRegistry(IEnumerable<IBattleRuntimeAdapter> adapters, string defaultRoomType)
    {
        if (adapters is null)
        {
            throw new ArgumentNullException(nameof(adapters));
        }

        _adapters = adapters.ToDictionary(a => a.RoomType, StringComparer.OrdinalIgnoreCase);
        if (!_adapters.TryGetValue(defaultRoomType, out _defaultAdapter!))
        {
            throw new InvalidOperationException($"Default battle runtime adapter is not registered. RoomType={defaultRoomType}");
        }
    }

    public IBattleRuntimeAdapter Resolve(string? roomType)
    {
        if (!string.IsNullOrWhiteSpace(roomType) && _adapters.TryGetValue(roomType, out var adapter))
        {
            return adapter;
        }

        return _defaultAdapter;
    }
}
