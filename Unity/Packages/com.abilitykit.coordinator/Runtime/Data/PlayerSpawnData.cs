namespace AbilityKit.Coordinator
{
    /// <summary>
    /// Player spawn data
    /// </summary>
    public struct PlayerSpawnData
    {
        /// <summary>
        /// Player identifier
        /// </summary>
        public int PlayerId;

        /// <summary>
        /// Character identifier
        /// </summary>
        public int CharacterId;

        /// <summary>
        /// Team identifier
        /// </summary>
        public int TeamId;

        /// <summary>
        /// Spawn position X
        /// </summary>
        public float X;

        /// <summary>
        /// Spawn position Y
        /// </summary>
        public float Y;

        /// <summary>
        /// Spawn position Z
        /// </summary>
        public float Z;

        /// <summary>
        /// Player name (optional)
        /// </summary>
        public string Name;

        public PlayerSpawnData(int playerId, int characterId, int teamId, float x, float y, float z, string name = null)
        {
            PlayerId = playerId;
            CharacterId = characterId;
            TeamId = teamId;
            X = x;
            Y = y;
            Z = z;
            Name = name ?? $"Player_{playerId}";
        }

        public static PlayerSpawnData CreateLocalPlayer(int playerId, int characterId, float x, float z)
        {
            return new PlayerSpawnData(playerId, characterId, 1, x, 0, z, "LocalPlayer");
        }
    }
}
