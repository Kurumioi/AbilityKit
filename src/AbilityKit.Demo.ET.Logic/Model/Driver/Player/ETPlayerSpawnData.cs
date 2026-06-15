namespace ET.Logic
{
    /// <summary>
    /// ET-side player spawn request data.
    /// Runtime ActorId is allocated by the Runtime enter-game flow, never by ET.Logic.
    /// </summary>
    public class ETPlayerSpawnData
    {
        /// <summary>
        /// Player identity used by room, input, and Runtime player-to-actor mapping.
        /// </summary>
        public string PlayerId { get; set; }

        public int CharacterId { get; set; }
        public int AttributeTemplateId { get; set; }
        public int Level { get; set; }
        public int BasicAttackSkillId { get; set; }
        public int[] SkillIds { get; set; }
        public string CharacterName { get; set; }
        public int TeamId { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }
        public float RotationY { get; set; }
        public float Scale { get; set; }
        public float Hp { get; set; }
        public float MaxHp { get; set; }

        public ETPlayerSpawnData()
        {
            PlayerId = string.Empty;
            CharacterName = string.Empty;
            Level = 0;
        }

        public ETPlayerSpawnData(string playerId, int characterId, int attributeTemplateId, int level, int basicAttackSkillId, int[] skillIds, string characterName, int teamId,
            float x, float y, float z, float rotY, float scale, float hp, float maxHp)
        {
            PlayerId = playerId ?? string.Empty;
            CharacterId = characterId;
            AttributeTemplateId = attributeTemplateId;
            Level = level;
            BasicAttackSkillId = basicAttackSkillId;
            SkillIds = skillIds != null && skillIds.Length > 0 ? (int[])skillIds.Clone() : null;
            CharacterName = characterName ?? string.Empty;
            TeamId = teamId;
            PositionX = x;
            PositionY = y;
            PositionZ = z;
            RotationY = rotY;
            Scale = scale;
            Hp = hp;
            MaxHp = maxHp;
        }
    }
}
