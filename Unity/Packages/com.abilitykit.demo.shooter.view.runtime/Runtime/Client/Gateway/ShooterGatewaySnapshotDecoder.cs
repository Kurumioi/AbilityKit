using System;
using AbilityKit.Protocol.Room;

namespace AbilityKit.Demo.Shooter.View
{
    public sealed class ShooterGatewaySnapshotDecoder
    {
        public bool IsSnapshotPush(uint opCode)
        {
            return opCode == RoomGatewayOpCodes.SnapshotPushed || opCode == RoomGatewayOpCodes.DeltaSnapshotPushed;
        }

        public ShooterGatewaySnapshot Decode(ArraySegment<byte> payload)
        {
            var wire = WireRoomGatewayBinary.Deserialize<WireStateSyncSnapshotPush>(payload);
            return ShooterGatewaySnapshotMapper.ToGatewaySnapshot(in wire);
        }
    }
}
