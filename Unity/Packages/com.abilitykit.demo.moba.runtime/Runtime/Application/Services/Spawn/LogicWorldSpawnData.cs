namespace AbilityKit.Demo.Moba.Services
{
    /// <summary>
    /// Logic-world spawn data used by runtime services.
    /// External hosts must map their own launch data into this runtime model before world bootstrap.
    /// </summary>
    public readonly struct LogicWorldSpawnData
    {
        public readonly int PlayerId;
        public readonly int CharacterId;
        public readonly int TeamId;
        public readonly float X;
        public readonly float Y;
        public readonly float Z;
        public readonly string Name;

        public LogicWorldSpawnData(int playerId, int characterId, int teamId, float x, float y, float z, string name = null)
        {
            PlayerId = playerId;
            CharacterId = characterId;
            TeamId = teamId;
            X = x;
            Y = y;
            Z = z;
            Name = name;
        }
    }
}
