namespace ET.Logic
{
    /// <summary>
    /// Local MOBA scenario defaults used by the ET demo and smoke entry points.
    /// Production room and matchmaking flows should provide equivalent values from services or config assets.
    /// </summary>
    public sealed class ETLocalMobaScenarioConfig
    {
        public int MapId { get; set; } = 1;
        public int WorldId { get; set; } = 1;
        public int GameplayId { get; set; } = 1;
        public int MaxPlayers { get; set; } = 6;
        public int MinPlayers { get; set; } = 1;
        public int TickRate { get; set; } = 30;
        public int InputDelayFrames { get; set; } = 2;
        public int RandomSeed { get; set; } = 20240615;
        public int LocalTeamId { get; set; } = 1;
        public int EnemyTeamId { get; set; } = 2;
        public int EnemyPlayerId { get; set; } = 2;
        public int HeroId { get; set; } = 1001;
        public int AttributeTemplateId { get; set; } = 1001;
        public int Level { get; set; } = 1;
        public int BasicAttackSkillId { get; set; } = 100101;
        public int[] SkillIds { get; set; } = { 10010101, 10010201, 10010301, 10010401 };
        public ETTeamSpawnLayout SpawnLayout { get; set; } = ETTeamSpawnLayout.CreateDefaultLaneLayout();
        public ETBattleAutomationOptions AutomationOptions { get; set; } = ETBattleAutomationOptions.CreateDisabled();
 
        public static ETLocalMobaScenarioConfig CreateLocalDemoDefaults()
        {
            return new ETLocalMobaScenarioConfig();
        }

        public static ETLocalMobaScenarioConfig CreateLocalSmokeDefaults()
        {
            return new ETLocalMobaScenarioConfig
            {
                AutomationOptions = ETBattleAutomationOptions.CreateLocalSmokeDefaults()
            };
        }

        public string CreateMatchId(long playerId)
        {
            return $"local_moba_{MapId}_{playerId}_{RandomSeed}";
        }

        public int ResolveEnemyPlayerId(int localPlayerId)
        {
            return EnemyPlayerId == localPlayerId ? localPlayerId + 1 : EnemyPlayerId;
        }
    }

    public sealed class ETTeamSpawnLayout
    {
        public float Team1OriginX { get; set; } = 0f;
        public float Team1OriginZ { get; set; } = 0f;
        public float Team2OriginX { get; set; } = 50f;
        public float Team2OriginZ { get; set; } = 0f;
        public float SlotSpacing { get; set; } = 10f;
        public float RotationY { get; set; } = 0f;
        public float Scale { get; set; } = 1f;

        public static ETTeamSpawnLayout CreateDefaultLaneLayout()
        {
            return new ETTeamSpawnLayout();
        }

        public void ResolvePosition(int teamId, int teamSlotIndex, int localTeamId, out float x, out float z)
        {
            if (teamId == localTeamId)
            {
                x = Team1OriginX;
                z = Team1OriginZ + SlotSpacing * teamSlotIndex;
                return;
            }

            x = Team2OriginX;
            z = Team2OriginZ + SlotSpacing * teamSlotIndex;
        }
    }
}
