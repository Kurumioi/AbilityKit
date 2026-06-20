using System.Collections.Concurrent;
using AbilityKit.Orleans.Contracts.Rooms;

namespace AbilityKit.Orleans.Grains.Persistence;

public sealed class InMemoryRoomStateStore : IRoomStateStore
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, RoomSummary>> _roomsByDirectory = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ulong> _roomToNumericId = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<ulong, string> _numericIdToRoom = new();
    private readonly ConcurrentDictionary<string, string> _accountToRoom = new(StringComparer.Ordinal);
    private ulong _nextNumericRoomId;

    public Task UpsertRoomAsync(string directoryKey, RoomSummary summary, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(directoryKey))
        {
            throw new ArgumentException("Directory key is required.", nameof(directoryKey));
        }

        if (summary is null)
        {
            throw new ArgumentNullException(nameof(summary));
        }

        var rooms = _roomsByDirectory.GetOrAdd(directoryKey, _ => new ConcurrentDictionary<string, RoomSummary>(StringComparer.Ordinal));
        rooms[summary.RoomId] = CloneSummary(summary);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<RoomSummary>> ListRoomsAsync(string directoryKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(directoryKey) || !_roomsByDirectory.TryGetValue(directoryKey, out var rooms))
        {
            return Task.FromResult<IReadOnlyCollection<RoomSummary>>(Array.Empty<RoomSummary>());
        }

        IReadOnlyCollection<RoomSummary> result = rooms.Values.Select(CloneSummary).ToArray();
        return Task.FromResult(result);
    }

    public Task UpdateRoomPlayerCountAsync(string directoryKey, string roomId, int playerCount, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(directoryKey) || string.IsNullOrWhiteSpace(roomId))
        {
            return Task.CompletedTask;
        }

        if (_roomsByDirectory.TryGetValue(directoryKey, out var rooms) && rooms.TryGetValue(roomId, out var summary))
        {
            rooms[roomId] = summary with { PlayerCount = Math.Max(0, playerCount) };
        }

        return Task.CompletedTask;
    }

    public Task RemoveRoomAsync(string directoryKey, string roomId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(directoryKey) || string.IsNullOrWhiteSpace(roomId))
        {
            return Task.CompletedTask;
        }

        if (_roomsByDirectory.TryGetValue(directoryKey, out var rooms))
        {
            rooms.TryRemove(roomId, out _);
        }

        return Task.CompletedTask;
    }

    public Task<ulong> GetOrCreateNumericRoomIdAsync(string roomId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roomId))
        {
            throw new ArgumentException("Room id is required.", nameof(roomId));
        }

        var numericId = _roomToNumericId.GetOrAdd(roomId, key =>
        {
            var createdId = (ulong)Interlocked.Increment(ref _nextNumericRoomId);
            _numericIdToRoom[createdId] = key;
            return createdId;
        });

        return Task.FromResult(numericId);
    }

    public Task<string?> TryGetRoomIdAsync(ulong numericRoomId, CancellationToken cancellationToken = default)
    {
        if (numericRoomId == 0)
        {
            return Task.FromResult<string?>(null);
        }

        _numericIdToRoom.TryGetValue(numericRoomId, out var roomId);
        return Task.FromResult(roomId);
    }

    public Task BindAccountRoomAsync(string accountId, string roomId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            throw new ArgumentException("Account id is required.", nameof(accountId));
        }

        if (string.IsNullOrWhiteSpace(roomId))
        {
            throw new ArgumentException("Room id is required.", nameof(roomId));
        }

        _accountToRoom[accountId] = roomId;
        return Task.CompletedTask;
    }

    public Task<string?> TryGetAccountRoomAsync(string accountId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return Task.FromResult<string?>(null);
        }

        _accountToRoom.TryGetValue(accountId, out var roomId);
        return Task.FromResult(roomId);
    }

    public Task ClearAccountRoomAsync(string accountId, string roomId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(roomId))
        {
            return Task.CompletedTask;
        }

        if (_accountToRoom.TryGetValue(accountId, out var currentRoomId) && string.Equals(currentRoomId, roomId, StringComparison.Ordinal))
        {
            _accountToRoom.TryRemove(accountId, out _);
        }

        return Task.CompletedTask;
    }

    private static RoomSummary CloneSummary(RoomSummary summary)
    {
        return summary with
        {
            Tags = summary.Tags is null ? null : new Dictionary<string, string>(summary.Tags, StringComparer.Ordinal)
        };
    }
}
