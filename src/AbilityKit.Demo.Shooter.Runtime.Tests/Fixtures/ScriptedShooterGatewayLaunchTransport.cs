using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AbilityKit.Demo.Shooter.View;
using AbilityKit.Protocol.Room;

namespace AbilityKit.Demo.Shooter.Runtime.Tests;

internal sealed class ScriptedShooterGatewayLaunchTransport : IShooterRoomGatewayRequestTransport
{
    public readonly List<uint> OpCodes = new List<uint>();

    public ArraySegment<byte> LastPayload { get; private set; }

    public Task<ArraySegment<byte>> SendRequestAsync(uint opCode, ArraySegment<byte> payload, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        OpCodes.Add(opCode);
        LastPayload = TestByteSegments.Copy(payload);

        switch (opCode)
        {
            case RoomGatewayOpCodes.CreateRoom:
                return Task.FromResult(WireRoomGatewayBinary.Serialize(new WireCreateRoomRes
                {
                    Success = true,
                    RoomId = "room-launch",
                    NumericRoomId = 1041ul,
                    Message = "created"
                }));
            case RoomGatewayOpCodes.JoinRoom:
                return Task.FromResult(WireRoomGatewayBinary.Serialize(new WireJoinRoomRes
                {
                    Success = true,
                    RoomId = "room-launch",
                    NumericRoomId = 1041ul,
                    Snapshot = new WireRoomSnapshot { BattleId = "battle-prelaunch", CanStart = true },
                    WorldStartAnchor = new WireWorldStartAnchor
                    {
                        StartServerTicks = 123456L,
                        ServerTickFrequency = 10000000L,
                        StartFrame = 0,
                        FixedDeltaSeconds = 1d / 30d
                    },
                    Message = "joined"
                }));
            case RoomGatewayOpCodes.SetReady:
                return Task.FromResult(WireRoomGatewayBinary.Serialize(new WireRoomSnapshotRes
                {
                    Success = true,
                    RoomId = "room-launch",
                    NumericRoomId = 1041ul,
                    Snapshot = new WireRoomSnapshot { BattleId = "battle-ready", CanStart = true },
                    Message = "ready"
                }));
            case RoomGatewayOpCodes.StartBattle:
                return Task.FromResult(WireRoomGatewayBinary.Serialize(new WireStartRoomBattleRes
                {
                    Success = true,
                    BattleId = "battle-launch",
                    WorldId = 9041ul,
                    Started = true,
                    WorldStartAnchor = new WireWorldStartAnchor
                    {
                        StartServerTicks = 123456L,
                        ServerTickFrequency = 10000000L,
                        StartFrame = 0,
                        FixedDeltaSeconds = 1d / 30d
                    },
                    ServerNowTicks = 123456L,
                    Message = "started"
                }));
            case RoomGatewayOpCodes.SubscribeStateSync:
                return Task.FromResult(WireRoomGatewayBinary.Serialize(new WireSubscribeStateSyncRes
                {
                    Success = true,
                    Message = "subscribed"
                }));
            case RoomGatewayOpCodes.SubmitBattleInput:
                return Task.FromResult(WireRoomGatewayBinary.Serialize(new WireSubmitBattleInputRes
                {
                    Success = true,
                    AcceptedFrame = 0,
                    Message = "accepted"
                }));
            default:
                throw new InvalidOperationException("Unexpected room gateway opCode: " + opCode);
        }
    }
}
