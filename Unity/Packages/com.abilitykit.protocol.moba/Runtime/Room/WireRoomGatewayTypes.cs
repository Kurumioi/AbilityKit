using System.Collections.Generic;
using MemoryPack;

namespace AbilityKit.Protocol.Moba.Room
{
    [MemoryPackable]
    public readonly partial struct WireRoomGuestLoginReq
    {
        [MemoryPackOrder(0)] public string GuestId { get; init; }
    }

    [MemoryPackable]
    public readonly partial struct WireRoomGuestLoginRes
    {
        [MemoryPackOrder(0)] public bool Success { get; init; }
        [MemoryPackOrder(1)] public string SessionToken { get; init; }
        [MemoryPackOrder(2)] public string AccountId { get; init; }
        [MemoryPackOrder(3)] public string Message { get; init; }
    }

    [MemoryPackable]
    public readonly partial struct WireCreateRoomReq
    {
        [MemoryPackOrder(0)] public string SessionToken { get; init; }
        [MemoryPackOrder(1)] public string Region { get; init; }
        [MemoryPackOrder(2)] public string ServerId { get; init; }
        [MemoryPackOrder(3)] public string RoomType { get; init; }
        [MemoryPackOrder(4)] public string Title { get; init; }
        [MemoryPackOrder(5)] public bool IsPublic { get; init; }
        [MemoryPackOrder(6)] public int MaxPlayers { get; init; }
        [MemoryPackOrder(7)] public Dictionary<string, string>? Tags { get; init; }
    }

    [MemoryPackable]
    public readonly partial struct WireCreateRoomRes
    {
        [MemoryPackOrder(0)] public bool Success { get; init; }
        [MemoryPackOrder(1)] public string RoomId { get; init; }
        [MemoryPackOrder(2)] public ulong NumericRoomId { get; init; }
        [MemoryPackOrder(3)] public string Message { get; init; }
    }

    [MemoryPackable]
    public readonly partial struct WireJoinRoomReq
    {
        [MemoryPackOrder(0)] public string SessionToken { get; init; }
        [MemoryPackOrder(1)] public string Region { get; init; }
        [MemoryPackOrder(2)] public string ServerId { get; init; }
        [MemoryPackOrder(3)] public string RoomId { get; init; }
    }

    [MemoryPackable]
    public readonly partial struct WireJoinRoomRes
    {
        [MemoryPackOrder(0)] public bool Success { get; init; }
        [MemoryPackOrder(1)] public string RoomId { get; init; }
        [MemoryPackOrder(2)] public ulong NumericRoomId { get; init; }
        [MemoryPackOrder(3)] public WireRoomSnapshot Snapshot { get; init; }
        [MemoryPackOrder(4)] public WireWorldStartAnchor WorldStartAnchor { get; init; }
        [MemoryPackOrder(5)] public string Message { get; init; }
    }

    [MemoryPackable]
    public readonly partial struct WireRoomReadyReq
    {
        [MemoryPackOrder(0)] public string SessionToken { get; init; }
        [MemoryPackOrder(1)] public string RoomId { get; init; }
        [MemoryPackOrder(2)] public bool Ready { get; init; }
    }

    [MemoryPackable]
    public readonly partial struct WireRoomSnapshotRes
    {
        [MemoryPackOrder(0)] public bool Success { get; init; }
        [MemoryPackOrder(1)] public string RoomId { get; init; }
        [MemoryPackOrder(2)] public ulong NumericRoomId { get; init; }
        [MemoryPackOrder(3)] public WireRoomSnapshot Snapshot { get; init; }
        [MemoryPackOrder(4)] public string Message { get; init; }
    }

    [MemoryPackable]
    public readonly partial struct WireRoomPickHeroReq
    {
        [MemoryPackOrder(0)] public string SessionToken { get; init; }
        [MemoryPackOrder(1)] public string RoomId { get; init; }
        [MemoryPackOrder(2)] public int HeroId { get; init; }
        [MemoryPackOrder(3)] public int TeamId { get; init; }
        [MemoryPackOrder(4)] public int SpawnPointId { get; init; }
        [MemoryPackOrder(5)] public int Level { get; init; }
        [MemoryPackOrder(6)] public int AttributeTemplateId { get; init; }
        [MemoryPackOrder(7)] public int BasicAttackSkillId { get; init; }
        [MemoryPackOrder(8)] public List<int>? SkillIds { get; init; }
    }

    [MemoryPackable]
    public readonly partial struct WireStartRoomBattleReq
    {
        [MemoryPackOrder(0)] public string SessionToken { get; init; }
        [MemoryPackOrder(1)] public string RoomId { get; init; }
        [MemoryPackOrder(2)] public int GameplayId { get; init; }
        [MemoryPackOrder(3)] public int RuleSetId { get; init; }
        [MemoryPackOrder(4)] public int ConfigVersion { get; init; }
        [MemoryPackOrder(5)] public int ProtocolVersion { get; init; }
        [MemoryPackOrder(6)] public string WorldType { get; init; }
        [MemoryPackOrder(7)] public string ClientId { get; init; }
    }

    [MemoryPackable]
    public readonly partial struct WireStartRoomBattleRes
    {
        [MemoryPackOrder(0)] public bool Success { get; init; }
        [MemoryPackOrder(1)] public string BattleId { get; init; }
        [MemoryPackOrder(2)] public ulong WorldId { get; init; }
        [MemoryPackOrder(3)] public bool Started { get; init; }
        [MemoryPackOrder(4)] public string Message { get; init; }
    }

    [MemoryPackable]
    public readonly partial struct WireRoomSummary
    {
        [MemoryPackOrder(0)] public string Region { get; init; }
        [MemoryPackOrder(1)] public string ServerId { get; init; }
        [MemoryPackOrder(2)] public string RoomId { get; init; }
        [MemoryPackOrder(3)] public string RoomType { get; init; }
        [MemoryPackOrder(4)] public string Title { get; init; }
        [MemoryPackOrder(5)] public bool IsPublic { get; init; }
        [MemoryPackOrder(6)] public int MaxPlayers { get; init; }
        [MemoryPackOrder(7)] public int PlayerCount { get; init; }
        [MemoryPackOrder(8)] public string OwnerAccountId { get; init; }
        [MemoryPackOrder(9)] public long CreatedAtUnixMs { get; init; }
        [MemoryPackOrder(10)] public Dictionary<string, string>? Tags { get; init; }
    }

    [MemoryPackable]
    public readonly partial struct WireRoomPlayerSnapshot
    {
        [MemoryPackOrder(0)] public string AccountId { get; init; }
        [MemoryPackOrder(1)] public int TeamId { get; init; }
        [MemoryPackOrder(2)] public bool Ready { get; init; }
        [MemoryPackOrder(3)] public int HeroId { get; init; }
        [MemoryPackOrder(4)] public int SpawnPointId { get; init; }
        [MemoryPackOrder(5)] public int Level { get; init; }
        [MemoryPackOrder(6)] public int AttributeTemplateId { get; init; }
        [MemoryPackOrder(7)] public int BasicAttackSkillId { get; init; }
        [MemoryPackOrder(8)] public List<int>? SkillIds { get; init; }
    }

    [MemoryPackable]
    public readonly partial struct WireRoomSnapshot
    {
        [MemoryPackOrder(0)] public WireRoomSummary Summary { get; init; }
        [MemoryPackOrder(1)] public List<string>? Members { get; init; }
        [MemoryPackOrder(2)] public List<WireRoomPlayerSnapshot>? Players { get; init; }
        [MemoryPackOrder(3)] public bool CanStart { get; init; }
        [MemoryPackOrder(4)] public string BattleId { get; init; }
    }

    [MemoryPackable]
    public readonly partial struct WireWorldStartAnchor
    {
        [MemoryPackOrder(0)] public long StartServerTicks { get; init; }
        [MemoryPackOrder(1)] public long ServerTickFrequency { get; init; }
        [MemoryPackOrder(2)] public int StartFrame { get; init; }
        [MemoryPackOrder(3)] public double FixedDeltaSeconds { get; init; }
    }
}
