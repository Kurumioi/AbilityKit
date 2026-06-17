namespace AbilityKit.Protocol.Room
{
    /// <summary>
    /// Room gateway opcodes used by Orleans Gateway room entrypoints.
    /// </summary>
    public static class RoomGatewayOpCodes
    {
        public const uint GuestLogin = 100;
        public const uint CreateRoom = 101;
        public const uint JoinRoom = 102;
        public const uint SubscribeStateSync = 103;
        public const uint SetReady = 104;
        public const uint PickHero = 105;
        public const uint StartBattle = 106;
        public const uint SubmitBattleInput = 107;
        public const uint RequestFullStateSync = 108;
        public const uint RestoreRoom = 109;

        public const uint SnapshotPushed = 9002;
        public const uint DeltaSnapshotPushed = 9003;
    }
}
