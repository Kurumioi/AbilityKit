using AbilityKit.Protocol.Moba.Room;

namespace AbilityKit.Game.Battle.Agent
{
    public readonly struct GatewayRoomOpCodes
    {
        public readonly uint CreateRoom;
        public readonly uint JoinRoom;
        public readonly uint SetReady;
        public readonly uint PickHero;
        public readonly uint StartBattle;

        public GatewayRoomOpCodes(uint createRoom, uint joinRoom)
            : this(createRoom, joinRoom, RoomGatewayOpCodes.SetReady, RoomGatewayOpCodes.PickHero, RoomGatewayOpCodes.StartBattle)
        {
        }

        public GatewayRoomOpCodes(uint createRoom, uint joinRoom, uint setReady, uint pickHero, uint startBattle)
        {
            CreateRoom = createRoom;
            JoinRoom = joinRoom;
            SetReady = setReady;
            PickHero = pickHero;
            StartBattle = startBattle;
        }
    }
}
