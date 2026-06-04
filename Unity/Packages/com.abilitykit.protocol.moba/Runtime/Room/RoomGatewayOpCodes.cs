namespace AbilityKit.Protocol.Moba.Room
{
    /// <summary>
    /// Room gateway opcodes used by Orleans Gateway room entrypoints.
    /// </summary>
    public static class RoomGatewayOpCodes
    {
        public const uint CreateRoom = 101;
        public const uint JoinRoom = 102;
        public const uint SetReady = 104;
        public const uint PickHero = 105;
        public const uint StartBattle = 106;
    }
}
