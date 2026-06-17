using System;
using System.Collections.Generic;
using AbilityKit.Orleans.Contracts.Rooms;
using Orleans;

namespace AbilityKit.Orleans.Grains.Rooms;

public sealed class RoomIdMappingGrain : Grain, IRoomIdMappingGrain
{
    private const string StateKey = "global";

    private readonly Dictionary<string, ulong> _roomToNum = new(StringComparer.Ordinal);
    private readonly Dictionary<ulong, string> _numToRoom = new();
    private readonly Dictionary<string, string> _accountToRoom = new(StringComparer.Ordinal);

    private ulong _next = 1;

    public Task<ulong> GetOrCreateNumericIdAsync(string roomId)
    {
        if (string.IsNullOrWhiteSpace(roomId)) throw new ArgumentException("roomId is required", nameof(roomId));

        if (_roomToNum.TryGetValue(roomId, out var existing))
        {
            return Task.FromResult(existing);
        }

        var id = _next++;
        _roomToNum[roomId] = id;
        _numToRoom[id] = roomId;
        return Task.FromResult(id);
    }

    public Task<string?> TryGetRoomIdAsync(ulong numericRoomId)
    {
        if (numericRoomId == 0) return Task.FromResult<string?>(null);
        return Task.FromResult(_numToRoom.TryGetValue(numericRoomId, out var roomId) ? roomId : null);
    }

    public Task BindAccountRoomAsync(string accountId, string roomId)
    {
        if (string.IsNullOrWhiteSpace(accountId)) throw new ArgumentException("accountId is required", nameof(accountId));
        if (string.IsNullOrWhiteSpace(roomId)) throw new ArgumentException("roomId is required", nameof(roomId));

        _accountToRoom[accountId] = roomId;
        return Task.CompletedTask;
    }

    public Task<string?> TryGetAccountRoomAsync(string accountId)
    {
        if (string.IsNullOrWhiteSpace(accountId)) return Task.FromResult<string?>(null);
        return Task.FromResult(_accountToRoom.TryGetValue(accountId, out var roomId) ? roomId : null);
    }

    public Task ClearAccountRoomAsync(string accountId, string roomId)
    {
        if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(roomId))
        {
            return Task.CompletedTask;
        }

        if (_accountToRoom.TryGetValue(accountId, out var currentRoomId) && string.Equals(currentRoomId, roomId, StringComparison.Ordinal))
        {
            _accountToRoom.Remove(accountId);
        }

        return Task.CompletedTask;
    }
}
