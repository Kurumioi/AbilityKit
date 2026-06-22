using System.Collections.Generic;
using System.Diagnostics;
using AbilityKit.Orleans.Contracts.Automation;
using AbilityKit.Orleans.Contracts.Rooms;
using Orleans.Serialization;

namespace AbilityKit.Orleans.Gateway.HttpApi;

[GenerateSerializer]
internal sealed record AccountLoginHttpRequest(
    [property: Id(0)] string AccountId,
    [property: Id(1)] int ExpireSeconds = 0,
    [property: Id(2)] bool KickExisting = false);

[GenerateSerializer]
internal sealed record SessionTokenHttpRequest(
    [property: Id(0)] string SessionToken);

[GenerateSerializer]
internal sealed record RenewSessionHttpRequest(
    [property: Id(0)] string SessionToken,
    [property: Id(1)] int ExtendSeconds = 0,
    [property: Id(2)] bool RotateToken = false);

[GenerateSerializer]
internal sealed record MarkRoomMemberOfflineRequest(
    [property: Id(0)] string AccountId,
    [property: Id(1)] string Reason);

[GenerateSerializer]
internal sealed record GameplayHttpDescriptor(
    [property: Id(0)] string RoomType,
    [property: Id(1)] int GameplayId,
    [property: Id(2)] string DisplayName,
    [property: Id(3)] int DefaultMaxPlayers,
    [property: Id(4)] bool RequiresPlayerLoadout,
    [property: Id(5)] string? DefaultWorldType,
    [property: Id(6)] int DefaultTickRate,
    [property: Id(7)] string? DefaultSyncTemplateId,
    [property: Id(8)] string[] SupportedSyncTemplateIds,
    [property: Id(9)] bool SupportsStateSyncPush,
    [property: Id(10)] bool SupportsFrameSync);

[GenerateSerializer]
internal sealed record WebCreateRoomRequest(
    [property: Id(0)] string SessionToken,
    [property: Id(1)] string Region,
    [property: Id(2)] string ServerId,
    [property: Id(3)] string RoomType,
    [property: Id(4)] string Title,
    [property: Id(5)] bool IsPublic,
    [property: Id(6)] int MaxPlayers,
    [property: Id(7)] Dictionary<string, string>? Tags,
    [property: Id(8)] bool AutoJoin = true);

[GenerateSerializer]
internal sealed record WebListRoomsRequest(
    [property: Id(0)] string SessionToken,
    [property: Id(1)] string Region,
    [property: Id(2)] string ServerId,
    [property: Id(3)] int Offset,
    [property: Id(4)] int Limit,
    [property: Id(5)] string? RoomType);

[GenerateSerializer]
internal sealed record WebRoomRequest(
    [property: Id(0)] string SessionToken,
    [property: Id(1)] string RoomId);

[GenerateSerializer]
internal sealed record WebRoomReadyRequest(
    [property: Id(0)] string SessionToken,
    [property: Id(1)] string RoomId,
    [property: Id(2)] bool Ready);

[GenerateSerializer]
internal sealed record WebRoomPickHeroRequest(
    [property: Id(0)] string SessionToken,
    [property: Id(1)] string RoomId,
    [property: Id(2)] int HeroId,
    [property: Id(3)] int TeamId,
    [property: Id(4)] int SpawnPointId,
    [property: Id(5)] int Level,
    [property: Id(6)] int AttributeTemplateId,
    [property: Id(7)] int BasicAttackSkillId,
    [property: Id(8)] List<int>? SkillIds);

[GenerateSerializer]
internal sealed record WebStartRoomBattleRequest(
    [property: Id(0)] string SessionToken,
    [property: Id(1)] string RoomId,
    [property: Id(2)] int GameplayId,
    [property: Id(3)] int RuleSetId,
    [property: Id(4)] int ConfigVersion,
    [property: Id(5)] int ProtocolVersion,
    [property: Id(6)] string? WorldType,
    [property: Id(7)] string? ClientId,
    [property: Id(8)] string? SyncTemplateId,
    [property: Id(9)] int? SyncModel,
    [property: Id(10)] string? NetworkEnvironmentId,
    [property: Id(11)] string? CarrierName,
    [property: Id(12)] bool EnableAuthoritativeWorld,
    [property: Id(13)] bool InterpolationEnabled,
    [property: Id(14)] int InputDelayFrames);

[GenerateSerializer]
internal sealed record ShooterSandboxHttpStartRequest(
    [property: Id(0)] string? SandboxId,
    [property: Id(1)] string Region,
    [property: Id(2)] string ServerId,
    [property: Id(3)] int BotCount,
    [property: Id(4)] int MaxPlayers,
    [property: Id(5)] int TickRate,
    [property: Id(6)] string? Title,
    [property: Id(7)] Dictionary<string, string>? Tags);

[GenerateSerializer]
internal sealed record ShooterSandboxHttpRequest(
    [property: Id(0)] string? SandboxId,
    [property: Id(1)] string? ServerId);

[GenerateSerializer]
internal sealed record AdminDashboardHttpRequest(
    [property: Id(0)] string? SessionToken,
    [property: Id(1)] string? Region,
    [property: Id(2)] string? ServerId,
    [property: Id(3)] string? RoomType,
    [property: Id(4)] int Limit = 20,
    [property: Id(5)] string? SandboxId = null);

[GenerateSerializer]
internal sealed record AdminDashboardHttpResponse(
    [property: Id(0)] GameplayHttpDescriptor[] Gameplays,
    [property: Id(1)] List<RoomSummary> Rooms,
    [property: Id(2)] string? AccountId,
    [property: Id(3)] string? CurrentRoomId,
    [property: Id(4)] RestoreRoomResponse? CurrentRoom,
    [property: Id(5)] RoomRuntimeState? RuntimeState,
    [property: Id(6)] ShooterSandboxState? ShooterSandbox,
    [property: Id(7)] long ServerNowTicks,
    [property: Id(8)] AdminServerStatusHttpResponse? ServerStatus = null);

[GenerateSerializer]
internal sealed record AdminServerStatusHttpResponse(
    [property: Id(0)] string EnvironmentName,
    [property: Id(1)] string ApplicationName,
    [property: Id(2)] string MachineName,
    [property: Id(3)] int ProcessId,
    [property: Id(4)] string ProcessName,
    [property: Id(5)] long StartTimeUtcTicks,
    [property: Id(6)] long UptimeSeconds,
    [property: Id(7)] long WorkingSetBytes,
    [property: Id(8)] long GcTotalMemoryBytes,
    [property: Id(9)] int ThreadCount,
    [property: Id(10)] bool MaintenanceMode,
    [property: Id(11)] bool DrainMode,
    [property: Id(12)] bool RestartRequested,
    [property: Id(13)] string? LastOperationId,
    [property: Id(14)] string? LastOperationReason,
    [property: Id(15)] string? LastOperationRequestedBy,
    [property: Id(16)] long? LastOperationRequestedAtTicks,
    [property: Id(17)] long ServerNowTicks);

[GenerateSerializer]
internal sealed record AdminServerOperationHttpRequest(
    [property: Id(0)] string? SessionToken,
    [property: Id(1)] string? Reason,
    [property: Id(2)] bool Enabled = true,
    [property: Id(3)] string? OperationId = null);

[GenerateSerializer]
internal sealed record AdminServerOperationHttpResponse(
    [property: Id(0)] bool Success,
    [property: Id(1)] string Operation,
    [property: Id(2)] string OperationId,
    [property: Id(3)] string RequestedBy,
    [property: Id(4)] string? Reason,
    [property: Id(5)] AdminServerStatusHttpResponse Status);
