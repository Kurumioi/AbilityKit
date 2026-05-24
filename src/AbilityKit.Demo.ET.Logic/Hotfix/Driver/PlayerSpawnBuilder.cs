using System.Collections.Generic;
using AbilityKit.Ability.Config;

namespace ET.Logic
{
    /// <summary>
    /// 玩家生成列表构建器
    /// 负责从配置或玩家注册信息构建 ETPlayerSpawnData 列表
    /// </summary>
    public static class PlayerSpawnBuilder
    {
        /// <summary>
        /// 从玩家注册列表构建生成数据
        /// </summary>
        public static List<ETPlayerSpawnData> BuildSpawnList(List<PlayerRegistration> players)
        {
            var spawnList = new List<ETPlayerSpawnData>();

            int team1Count = 0;
            int team2Count = 0;

            foreach (var player in players)
            {
                float x, z;
                if (player.TeamId == 1)
                {
                    x = 0f;
                    z = 10f * team1Count;
                    team1Count++;
                }
                else
                {
                    x = 50f;
                    z = 10f * team2Count;
                    team2Count++;
                }

                var spawnData = new ETPlayerSpawnData(
                    player.PlayerId,
                    player.CharacterId,
                    player.PlayerName,
                    player.TeamId,
                    x,
                    0f,
                    z);

                spawnList.Add(spawnData);
            }

            return spawnList;
        }

        /// <summary>
        /// 从配置构建生成列表
        /// </summary>
        public static List<ETPlayerSpawnData> BuildSpawnListFromConfig(ITextAssetLoader loader, int localPlayerId)
        {
            var players = new List<ETPlayerSpawnData>();

            if (loader == null)
            {
                return players;
            }

            var configLoader = new ETConfigLoaderService(loader);
            configLoader.LoadAll();

            if (configLoader.Characters.Count == 0)
            {
                return players;
            }

            int actorIdBase = localPlayerId > 0 ? localPlayerId : 1;

            // Team 1 本地玩家
            if (configLoader.TryGetCharacter(1001, out var heroConfig))
            {
                var attrs = configLoader.TryGetAttributeTemplate(heroConfig.AttributeTemplateId, out var attr) ? attr : null;
                float hp = attrs?.Hp ?? 500f;
                float maxHp = (attrs?.MaxHp > 0 ? attrs.MaxHp : hp);

                players.Add(new ETPlayerSpawnData(actorIdBase, heroConfig.Id, heroConfig.Name, 1, 0f, 0f, 0f));
                Log.Info($"[PlayerSpawnBuilder] Loaded player: {heroConfig.Name} (Team 1)");
            }

            // Team 1 AI
            for (int i = 2; i <= 3; i++)
            {
                int heroId = 1000 + i;
                if (configLoader.TryGetCharacter(heroId, out var aiConfig))
                {
                    configLoader.TryGetAttributeTemplate(aiConfig.AttributeTemplateId, out var aiAttr);
                    float hp = aiAttr?.Hp ?? 500f;
                    float maxHp = (aiAttr?.MaxHp > 0 ? aiAttr.MaxHp : hp);

                    players.Add(new ETPlayerSpawnData(actorIdBase + i, aiConfig.Id, aiConfig.Name, 1, 10f * (i - 1), 0f, 0f));
                    Log.Info($"[PlayerSpawnBuilder] Loaded AI: {aiConfig.Name} (Team 1)");
                }
            }

            // Team 2 敌人
            for (int i = 1; i <= 3; i++)
            {
                int heroId = 1000 + i;
                if (configLoader.TryGetCharacter(heroId, out var enemyConfig))
                {
                    configLoader.TryGetAttributeTemplate(enemyConfig.AttributeTemplateId, out var enemyAttr);
                    float hp = enemyAttr?.Hp ?? 500f;
                    float maxHp = (enemyAttr?.MaxHp > 0 ? enemyAttr.MaxHp : hp);

                    players.Add(new ETPlayerSpawnData(2000 + i, enemyConfig.Id, enemyConfig.Name, 2, 0f, 0f, 50f + 10f * (i - 1)));
                    Log.Info($"[PlayerSpawnBuilder] Loaded enemy: {enemyConfig.Name} (Team 2)");
                }
            }

            return players;
        }
    }
}
