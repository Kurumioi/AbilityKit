using System;

namespace ET.Logic
{
    /// <summary>
    /// Builds a local MOBA room scenario for demo and smoke execution.
    /// Keeps synthetic room commands outside the formal room component.
    /// </summary>
    public static class ETLocalMobaScenarioInitializer
    {
        public static void SetupRoom(ETMobaRoomComponent roomComponent, ETLocalMobaScenarioConfig config)
        {
            if (roomComponent == null)
            {
                throw new ArgumentNullException(nameof(roomComponent));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (roomComponent.RoomOrchestrator == null)
            {
                throw new InvalidOperationException("Room must be initialized before applying local scenario setup.");
            }

            var localPlayerId = int.Parse(roomComponent.LocalPlayerId.Value);
            var enemyPlayerId = config.ResolveEnemyPlayerId(localPlayerId);
            var skillIds = config.SkillIds ?? Array.Empty<int>();

            roomComponent.JoinRoom(localPlayerId, config.LocalTeamId);
            roomComponent.PickHero(localPlayerId, config.HeroId, config.AttributeTemplateId, config.Level, config.BasicAttackSkillId, skillIds);

            roomComponent.JoinRoom(enemyPlayerId, config.EnemyTeamId);
            roomComponent.PickHero(enemyPlayerId, config.HeroId, config.AttributeTemplateId, config.Level, config.BasicAttackSkillId, skillIds);
            roomComponent.SetPlayerReady(enemyPlayerId, ready: true);

            roomComponent.SetPlayerReady(localPlayerId, ready: true);

            Log.Info($"[ETLocalMobaScenario] Room setup complete: LocalPlayer={localPlayerId}, EnemyPlayer={enemyPlayerId}, HeroId={config.HeroId}, SkillCount={skillIds.Length}");
        }
    }
}
