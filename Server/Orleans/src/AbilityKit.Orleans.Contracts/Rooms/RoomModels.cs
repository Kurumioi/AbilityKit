using System.Collections.Generic;
using AbilityKit.Orleans.Contracts.Battle;
using Orleans.Serialization;

namespace AbilityKit.Orleans.Contracts.Rooms;

[GenerateSerializer]
public sealed record RoomSummary(
    [property: Id(0)] string Region,
    [property: Id(1)] string ServerId,
    [property: Id(2)] string RoomId,
    [property: Id(3)] string RoomType,
    [property: Id(4)] string Title,
    [property: Id(5)] bool IsPublic,
    [property: Id(6)] int MaxPlayers,
    [property: Id(7)] int PlayerCount,
    [property: Id(8)] string OwnerAccountId,
    [property: Id(9)] long CreatedAtUnixMs,
    [property: Id(10)] Dictionary<string, string>? Tags);

[GenerateSerializer]
public sealed record CreateRoomRequest(
    [property: Id(0)] string AccountId,
    [property: Id(1)] string Region,
    [property: Id(2)] string ServerId,
    [property: Id(3)] string RoomType,
    [property: Id(4)] string Title,
    [property: Id(5)] bool IsPublic,
    [property: Id(6)] int MaxPlayers,
    [property: Id(7)] Dictionary<string, string>? Tags);

[GenerateSerializer]
public sealed record CreateRoomResponse(
    [property: Id(0)] string RoomId);

[GenerateSerializer]
public sealed record JoinRoomRequest(
    [property: Id(0)] string AccountId,
    [property: Id(1)] string Region,
    [property: Id(2)] string ServerId,
    [property: Id(3)] string RoomId);

[GenerateSerializer]
public sealed record LeaveRoomRequest(
    [property: Id(0)] string AccountId,
    [property: Id(1)] string Region,
    [property: Id(2)] string ServerId,
    [property: Id(3)] string RoomId);

[GenerateSerializer]
public sealed record RoomPlayerSnapshot(
    [property: Id(0)] string AccountId,
    [property: Id(1)] int TeamId,
    [property: Id(2)] bool Ready,
    [property: Id(3)] int HeroId,
    [property: Id(4)] int SpawnPointId,
    [property: Id(5)] int Level,
    [property: Id(6)] int AttributeTemplateId,
    [property: Id(7)] int BasicAttackSkillId,
    [property: Id(8)] List<int>? SkillIds);

[GenerateSerializer]
public sealed record RoomSnapshot(
    [property: Id(0)] RoomSummary Summary,
    [property: Id(1)] List<string> Members,
    [property: Id(2)] List<RoomPlayerSnapshot> Players,
    [property: Id(3)] bool CanStart,
    [property: Id(4)] string? BattleId,
    [property: Id(5)] WorldStartAnchor? WorldStartAnchor);

[GenerateSerializer]
public sealed record RoomReadyRequest(
    [property: Id(0)] string AccountId,
    [property: Id(1)] bool Ready);

[GenerateSerializer]
public sealed record RoomPickHeroRequest(
    [property: Id(0)] string AccountId,
    [property: Id(1)] int HeroId,
    [property: Id(2)] int TeamId,
    [property: Id(3)] int SpawnPointId,
    [property: Id(4)] int Level,
    [property: Id(5)] int AttributeTemplateId,
    [property: Id(6)] int BasicAttackSkillId,
    [property: Id(7)] List<int>? SkillIds);

[GenerateSerializer]
public sealed record StartRoomBattleRequest(
    [property: Id(0)] string AccountId,
    [property: Id(1)] int GameplayId,
    [property: Id(2)] int RuleSetId,
    [property: Id(3)] int ConfigVersion,
    [property: Id(4)] int ProtocolVersion,
    [property: Id(5)] string? WorldType,
    [property: Id(6)] string? ClientId);

[GenerateSerializer]
public sealed record StartRoomBattleResponse(
    [property: Id(0)] string BattleId,
    [property: Id(1)] ulong WorldId,
    [property: Id(2)] bool Started,
    [property: Id(3)] WorldStartAnchor? WorldStartAnchor,
    [property: Id(4)] long ServerNowTicks);

[GenerateSerializer]
public sealed record ListRoomsRequest(
    [property: Id(0)] string AccountId,
    [property: Id(1)] string Region,
    [property: Id(2)] string ServerId,
    [property: Id(3)] int Offset,
    [property: Id(4)] int Limit,
    [property: Id(5)] string? RoomType);

[GenerateSerializer]
public sealed record ListRoomsResponse(
    [property: Id(0)] List<RoomSummary> Rooms,
    [property: Id(1)] int NextOffset);
