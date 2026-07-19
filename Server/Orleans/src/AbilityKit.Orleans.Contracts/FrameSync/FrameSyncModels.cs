using System.Collections.Generic;
using Orleans.Serialization;

namespace AbilityKit.Orleans.Contracts.FrameSync;

[GenerateSerializer]
public sealed record FrameSyncStartOptions(
    [property: Id(0)] ulong RoomId,
    [property: Id(1)] ulong WorldId,
    [property: Id(2)] int TickRate,
    [property: Id(3)] string? BattleId,
    [property: Id(4)] string? SyncTemplateId);

[GenerateSerializer]
public sealed record FrameInputItem(
    [property: Id(0)] uint PlayerId,
    [property: Id(1)] int OpCode,
    [property: Id(2)] byte[] Payload);

[GenerateSerializer]
public enum FrameInputSubmitReason
{
    None = 0,
    WorldMismatch = 1,
    NegativeFrame = 2,
    FrameAlreadyProcessed = 3,
    FrameTooFarAhead = 4,
    RateLimited = 100
}

[GenerateSerializer]
public sealed record FrameInputSubmitResult(
    [property: Id(0)] bool Accepted,
    [property: Id(1)] int ServerFrame,
    [property: Id(2)] FrameInputSubmitReason Reason);

[GenerateSerializer]
public sealed record FramePushedEvent(
    [property: Id(0)] ulong RoomId,
    [property: Id(1)] ulong WorldId,
    [property: Id(2)] int Frame,
    [property: Id(3)] List<FrameInputItem> Inputs);
