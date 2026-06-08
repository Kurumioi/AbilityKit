using System.Collections.Generic;
using MemoryPack;

namespace AbilityKit.Protocol.Room
{
    [MemoryPackable]
    public partial struct WireRoomGuestLoginReq
    {
        [MemoryPackOrder(0)] public string GuestId { get; set; }
    }

    [MemoryPackable]
    public partial struct WireRoomGuestLoginRes
    {
        [MemoryPackOrder(0)] public bool Success { get; set; }
        [MemoryPackOrder(1)] public string SessionToken { get; set; }
        [MemoryPackOrder(2)] public string AccountId { get; set; }
        [MemoryPackOrder(3)] public string Message { get; set; }
    }

    [MemoryPackable]
    public partial struct WireCreateRoomReq
    {
        [MemoryPackOrder(0)] public string SessionToken { get; set; }
        [MemoryPackOrder(1)] public string Region { get; set; }
        [MemoryPackOrder(2)] public string ServerId { get; set; }
        [MemoryPackOrder(3)] public string RoomType { get; set; }
        [MemoryPackOrder(4)] public string Title { get; set; }
        [MemoryPackOrder(5)] public bool IsPublic { get; set; }
        [MemoryPackOrder(6)] public int MaxPlayers { get; set; }
        [MemoryPackOrder(7)] public Dictionary<string, string>? Tags { get; set; }
    }

    [MemoryPackable]
    public partial struct WireCreateRoomRes
    {
        [MemoryPackOrder(0)] public bool Success { get; set; }
        [MemoryPackOrder(1)] public string RoomId { get; set; }
        [MemoryPackOrder(2)] public ulong NumericRoomId { get; set; }
        [MemoryPackOrder(3)] public string Message { get; set; }
    }

    [MemoryPackable]
    public partial struct WireJoinRoomReq
    {
        [MemoryPackOrder(0)] public string SessionToken { get; set; }
        [MemoryPackOrder(1)] public string Region { get; set; }
        [MemoryPackOrder(2)] public string ServerId { get; set; }
        [MemoryPackOrder(3)] public string RoomId { get; set; }
    }

    [MemoryPackable]
    public partial struct WireJoinRoomRes
    {
        [MemoryPackOrder(0)] public bool Success { get; set; }
        [MemoryPackOrder(1)] public string RoomId { get; set; }
        [MemoryPackOrder(2)] public ulong NumericRoomId { get; set; }
        [MemoryPackOrder(3)] public WireRoomSnapshot Snapshot { get; set; }
        [MemoryPackOrder(4)] public WireWorldStartAnchor WorldStartAnchor { get; set; }
        [MemoryPackOrder(5)] public string Message { get; set; }
    }

    [MemoryPackable]
    public partial struct WireRoomReadyReq
    {
        [MemoryPackOrder(0)] public string SessionToken { get; set; }
        [MemoryPackOrder(1)] public string RoomId { get; set; }
        [MemoryPackOrder(2)] public bool Ready { get; set; }
    }

    [MemoryPackable]
    public partial struct WireRoomSnapshotRes
    {
        [MemoryPackOrder(0)] public bool Success { get; set; }
        [MemoryPackOrder(1)] public string RoomId { get; set; }
        [MemoryPackOrder(2)] public ulong NumericRoomId { get; set; }
        [MemoryPackOrder(3)] public WireRoomSnapshot Snapshot { get; set; }
        [MemoryPackOrder(4)] public string Message { get; set; }
    }

    [MemoryPackable]
    public partial struct WireRoomPickHeroReq
    {
        [MemoryPackOrder(0)] public string SessionToken { get; set; }
        [MemoryPackOrder(1)] public string RoomId { get; set; }
        [MemoryPackOrder(2)] public int HeroId { get; set; }
        [MemoryPackOrder(3)] public int TeamId { get; set; }
        [MemoryPackOrder(4)] public int SpawnPointId { get; set; }
        [MemoryPackOrder(5)] public int Level { get; set; }
        [MemoryPackOrder(6)] public int AttributeTemplateId { get; set; }
        [MemoryPackOrder(7)] public int BasicAttackSkillId { get; set; }
        [MemoryPackOrder(8)] public List<int>? SkillIds { get; set; }
    }

    [MemoryPackable]
    public partial struct WireStartRoomBattleReq
    {
        [MemoryPackOrder(0)] public string SessionToken { get; set; }
        [MemoryPackOrder(1)] public string RoomId { get; set; }
        [MemoryPackOrder(2)] public int GameplayId { get; set; }
        [MemoryPackOrder(3)] public int RuleSetId { get; set; }
        [MemoryPackOrder(4)] public int ConfigVersion { get; set; }
        [MemoryPackOrder(5)] public int ProtocolVersion { get; set; }
        [MemoryPackOrder(6)] public string WorldType { get; set; }
        [MemoryPackOrder(7)] public string ClientId { get; set; }
    }

    [MemoryPackable]
    public partial struct WireStartRoomBattleRes
    {
        [MemoryPackOrder(0)] public bool Success { get; set; }
        [MemoryPackOrder(1)] public string BattleId { get; set; }
        [MemoryPackOrder(2)] public ulong WorldId { get; set; }
        [MemoryPackOrder(3)] public bool Started { get; set; }
        [MemoryPackOrder(4)] public string Message { get; set; }
    }

    [MemoryPackable]
    public partial struct WireSubmitBattleInputReq
    {
        [MemoryPackOrder(0)] public string SessionToken { get; set; }
        [MemoryPackOrder(1)] public string BattleId { get; set; }
        [MemoryPackOrder(2)] public ulong WorldId { get; set; }
        [MemoryPackOrder(3)] public int Frame { get; set; }
        [MemoryPackOrder(4)] public uint PlayerId { get; set; }
        [MemoryPackOrder(5)] public int InputOpCode { get; set; }
        [MemoryPackOrder(6)] public byte[]? Payload { get; set; }
    }

    [MemoryPackable]
    public partial struct WireSubmitBattleInputRes
    {
        [MemoryPackOrder(0)] public bool Success { get; set; }
        [MemoryPackOrder(1)] public int AcceptedFrame { get; set; }
        [MemoryPackOrder(2)] public string Message { get; set; }
    }

    [MemoryPackable]
    public partial struct WireSubscribeStateSyncReq
    {
        [MemoryPackOrder(0)] public string SessionToken { get; set; }
        [MemoryPackOrder(1)] public string BattleId { get; set; }
        [MemoryPackOrder(2)] public string RoomId { get; set; }
    }

    [MemoryPackable]
    public partial struct WireSubscribeStateSyncRes
    {
        [MemoryPackOrder(0)] public bool Success { get; set; }
        [MemoryPackOrder(1)] public string Message { get; set; }
    }

    [MemoryPackable]
    public partial struct WireStateSyncSnapshotPush
    {
        [MemoryPackOrder(0)] public ulong WorldId { get; set; }
        [MemoryPackOrder(1)] public int Frame { get; set; }
        [MemoryPackOrder(2)] public double Timestamp { get; set; }
        [MemoryPackOrder(3)] public bool IsFullSnapshot { get; set; }
        [MemoryPackOrder(4)] public List<WireStateSyncActorSnapshot>? Actors { get; set; }
    }

    [MemoryPackable]
    public partial struct WireStateSyncActorSnapshot
    {
        [MemoryPackOrder(0)] public int ActorId { get; set; }
        [MemoryPackOrder(1)] public float X { get; set; }
        [MemoryPackOrder(2)] public float Y { get; set; }
        [MemoryPackOrder(3)] public float Z { get; set; }
        [MemoryPackOrder(4)] public float Rotation { get; set; }
        [MemoryPackOrder(5)] public float VelocityX { get; set; }
        [MemoryPackOrder(6)] public float VelocityZ { get; set; }
        [MemoryPackOrder(7)] public float Hp { get; set; }
        [MemoryPackOrder(8)] public float HpMax { get; set; }
        [MemoryPackOrder(9)] public int TeamId { get; set; }
    }

    [MemoryPackable]
    public partial struct WireRoomSummary
    {
        [MemoryPackOrder(0)] public string Region { get; set; }
        [MemoryPackOrder(1)] public string ServerId { get; set; }
        [MemoryPackOrder(2)] public string RoomId { get; set; }
        [MemoryPackOrder(3)] public string RoomType { get; set; }
        [MemoryPackOrder(4)] public string Title { get; set; }
        [MemoryPackOrder(5)] public bool IsPublic { get; set; }
        [MemoryPackOrder(6)] public int MaxPlayers { get; set; }
        [MemoryPackOrder(7)] public int PlayerCount { get; set; }
        [MemoryPackOrder(8)] public string OwnerAccountId { get; set; }
        [MemoryPackOrder(9)] public long CreatedAtUnixMs { get; set; }
        [MemoryPackOrder(10)] public Dictionary<string, string>? Tags { get; set; }
    }

    [MemoryPackable]
    public partial struct WireRoomPlayerSnapshot
    {
        [MemoryPackOrder(0)] public string AccountId { get; set; }
        [MemoryPackOrder(1)] public int TeamId { get; set; }
        [MemoryPackOrder(2)] public bool Ready { get; set; }
        [MemoryPackOrder(3)] public int HeroId { get; set; }
        [MemoryPackOrder(4)] public int SpawnPointId { get; set; }
        [MemoryPackOrder(5)] public int Level { get; set; }
        [MemoryPackOrder(6)] public int AttributeTemplateId { get; set; }
        [MemoryPackOrder(7)] public int BasicAttackSkillId { get; set; }
        [MemoryPackOrder(8)] public List<int>? SkillIds { get; set; }
    }

    [MemoryPackable]
    public partial struct WireRoomSnapshot
    {
        [MemoryPackOrder(0)] public WireRoomSummary Summary { get; set; }
        [MemoryPackOrder(1)] public List<string>? Members { get; set; }
        [MemoryPackOrder(2)] public List<WireRoomPlayerSnapshot>? Players { get; set; }
        [MemoryPackOrder(3)] public bool CanStart { get; set; }
        [MemoryPackOrder(4)] public string BattleId { get; set; }
    }

    [MemoryPackable]
    public partial struct WireWorldStartAnchor
    {
        [MemoryPackOrder(0)] public long StartServerTicks { get; set; }
        [MemoryPackOrder(1)] public long ServerTickFrequency { get; set; }
        [MemoryPackOrder(2)] public int StartFrame { get; set; }
        [MemoryPackOrder(3)] public double FixedDeltaSeconds { get; set; }
    }
}
