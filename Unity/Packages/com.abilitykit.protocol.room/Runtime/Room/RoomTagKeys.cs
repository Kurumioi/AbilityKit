namespace AbilityKit.Protocol.Room
{
    /// <summary>
    /// Well-known room tag keys shared by clients and Orleans room grains.
    /// </summary>
    public static class RoomTagKeys
    {
        public const string Gameplay = "gameplay";
        public const string WorldType = "worldType";
        public const string TickRate = "tickRate";
        public const string OfflineTimeoutSeconds = "offlineTimeoutSeconds";
    }
}
