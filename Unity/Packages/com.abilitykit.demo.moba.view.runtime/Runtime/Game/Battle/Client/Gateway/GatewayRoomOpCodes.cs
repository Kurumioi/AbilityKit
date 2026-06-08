using RoomOpCodes = AbilityKit.Protocol.Moba.Room.RoomGatewayOpCodes;
using StateSyncOpCodes = AbilityKit.Protocol.Moba.StateSync.OpCodes;
using FrameSyncOpCodes = AbilityKit.Protocol.Moba.Generated.GatewayFrameSync.OpCodes;

namespace AbilityKit.Game.Battle.Agent
{
    public readonly struct GatewayRoomOpCodes
    {
        public readonly uint CreateRoom;
        public readonly uint JoinRoom;
        public readonly uint SubscribeStateSync;
        public readonly uint SetReady;
        public readonly uint PickHero;
        public readonly uint StartBattle;
        public readonly uint SubmitBattleInput;
        public readonly uint SnapshotPushed;
        public readonly uint DeltaSnapshotPushed;

        public GatewayRoomOpCodes(uint createRoom, uint joinRoom)
            : this(createRoom, joinRoom, StateSyncOpCodes.SubscribeStateSync, RoomOpCodes.SetReady, RoomOpCodes.PickHero, RoomOpCodes.StartBattle, FrameSyncOpCodes.SubmitFrameInput)
        {
        }

        public GatewayRoomOpCodes(uint createRoom, uint joinRoom, uint setReady, uint pickHero, uint startBattle)
            : this(createRoom, joinRoom, StateSyncOpCodes.SubscribeStateSync, setReady, pickHero, startBattle, FrameSyncOpCodes.SubmitFrameInput)
        {
        }

        public GatewayRoomOpCodes(uint createRoom, uint joinRoom, uint setReady, uint pickHero, uint startBattle, uint submitBattleInput)
            : this(createRoom, joinRoom, StateSyncOpCodes.SubscribeStateSync, setReady, pickHero, startBattle, submitBattleInput)
        {
        }

        public GatewayRoomOpCodes(uint createRoom, uint joinRoom, uint subscribeStateSync, uint setReady, uint pickHero, uint startBattle, uint submitBattleInput)
            : this(createRoom, joinRoom, subscribeStateSync, setReady, pickHero, startBattle, submitBattleInput, StateSyncOpCodes.SnapshotPushed, StateSyncOpCodes.DeltaSnapshotPushed)
        {
        }

        public GatewayRoomOpCodes(uint createRoom, uint joinRoom, uint subscribeStateSync, uint setReady, uint pickHero, uint startBattle, uint submitBattleInput, uint snapshotPushed, uint deltaSnapshotPushed)
        {
            CreateRoom = createRoom;
            JoinRoom = joinRoom;
            SubscribeStateSync = subscribeStateSync;
            SetReady = setReady;
            PickHero = pickHero;
            StartBattle = startBattle;
            SubmitBattleInput = submitBattleInput;
            SnapshotPushed = snapshotPushed;
            DeltaSnapshotPushed = deltaSnapshotPushed;
        }
    }
}
