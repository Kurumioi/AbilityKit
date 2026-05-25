using System;
using System.Collections.Generic;
using AbilityKit.Ability.Config;
using AbilityKit.Demo.Moba.Share;

namespace ET.Logic
{
    /// <summary>
    /// 配置加载服务
    /// 负责从 TextAsset 加载游戏配置（角色、属性模板等）
    /// </summary>
    public sealed class ETConfigLoaderService
    {
        private readonly ITextAssetLoader _loader;

        /// <summary>
        /// 角色配置
        /// </summary>
        public List<JsonCharacter> Characters { get; private set; }

        /// <summary>
        /// 属性模板字典
        /// </summary>
        public Dictionary<int, JsonAttributeTemplate> AttributeTemplates { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public ETConfigLoaderService(ITextAssetLoader loader)
        {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
            Characters = new List<JsonCharacter>();
            AttributeTemplates = new Dictionary<int, JsonAttributeTemplate>();
        }

        /// <summary>
        /// 加载所有配置
        /// </summary>
        public void LoadAll()
        {
            LoadCharacterConfigs();
            LoadAttributeTemplates();
        }

        /// <summary>
        /// 加载角色配置
        /// </summary>
        public void LoadCharacterConfigs()
        {
            Characters.Clear();
            var path = "Configs/moba/characters.json";

            if (_loader.TryLoadText(path, out var json) && !string.IsNullOrEmpty(json))
            {
                try
                {
                    var characters = Newtonsoft.Json.JsonConvert.DeserializeObject<List<JsonCharacter>>(json);
                    if (characters != null)
                    {
                        Characters.AddRange(characters);
                        Log.Info($"[ETConfigLoaderService] Loaded {Characters.Count} character configs");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[ETConfigLoaderService] Failed to load characters: {ex.Message}");
                }
            }
            else
            {
                Log.Warning($"[ETConfigLoaderService] Characters file not found: {path}");
            }
        }

        /// <summary>
        /// 加载属性模板配置
        /// </summary>
        public void LoadAttributeTemplates()
        {
            AttributeTemplates.Clear();
            var path = "Configs/moba/attribute_templates.json";

            if (_loader.TryLoadText(path, out var json) && !string.IsNullOrEmpty(json))
            {
                try
                {
                    var templates = Newtonsoft.Json.JsonConvert.DeserializeObject<List<JsonAttributeTemplate>>(json);
                    if (templates != null)
                    {
                        foreach (var t in templates)
                        {
                            AttributeTemplates[t.Id] = t;
                        }
                        Log.Info($"[ETConfigLoaderService] Loaded {AttributeTemplates.Count} attribute templates");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[ETConfigLoaderService] Failed to load attribute templates: {ex.Message}");
                }
            }
            else
            {
                Log.Warning($"[ETConfigLoaderService] Attribute templates file not found: {path}");
            }
        }

        /// <summary>
        /// 根据 ID 获取角色配置
        /// </summary>
        public bool TryGetCharacter(int id, out JsonCharacter config)
        {
            foreach (var c in Characters)
            {
                if (c.Id == id)
                {
                    config = c;
                    return true;
                }
            }
            config = null;
            return false;
        }

        /// <summary>
        /// 根据 ID 获取属性模板
        /// </summary>
        public bool TryGetAttributeTemplate(int id, out JsonAttributeTemplate template)
        {
            return AttributeTemplates.TryGetValue(id, out template);
        }

        /// <summary>
        /// 根据角色配置和属性模板构建 ActorSpawnData 列表
        /// </summary>
        public List<ActorSpawnData> BuildActorSpawns(List<ETPlayerSpawnData> players)
        {
            var spawns = new List<ActorSpawnData>();

            foreach (var player in players)
            {
                int actorId = player.ActorId;
                float hp = player.Hp > 0 ? player.Hp : 200f;
                float maxHp = player.MaxHp > 0 ? player.MaxHp : hp;

                spawns.Add(new ActorSpawnData(
                    actorId,
                    player.CharacterId,
                    player.CharacterName,
                    player.PositionX,
                    player.PositionY,
                    player.PositionZ,
                    player.RotationY,
                    player.Scale > 0 ? player.Scale : 1f,
                    player.TeamId,
                    hp,
                    maxHp));

                Log.Info($"[ETConfigLoaderService] Built spawn: ActorId={actorId}, Character={player.CharacterName}, Team={player.TeamId}, HP={hp}");
            }

            return spawns;
        }

        /// <summary>
        /// 添加默认生成数据
        /// </summary>
        public List<ActorSpawnData> CreateDefaultSpawns(int playerActorId = 1)
        {
            var spawns = new List<ActorSpawnData>();

            // Player character
            spawns.Add(new ActorSpawnData(
                playerActorId, 1001, "Hero_001",
                0f, 0f, 0f, 0f, 1f,
                1, 200f, 200f));

            // Default minions
            spawns.Add(new ActorSpawnData(
                2001, 2001, "Enemy_Minion_1",
                10f, 0f, 5f, 0f, 1f,
                2, 80f, 80f));

            spawns.Add(new ActorSpawnData(
                2002, 2001, "Enemy_Minion_2",
                10f, 0f, -5f, 0f, 1f,
                2, 80f, 80f));

            Log.Info($"[ETConfigLoaderService] Created {spawns.Count} default spawns");

            return spawns;
        }
    }

    /// <summary>
    /// 角色配置（JSON 反序列化）
    /// </summary>
    public class JsonCharacter
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ModelId { get; set; }
        public int AttributeTemplateId { get; set; }
        public List<int> SkillIds { get; set; }
        public List<int> PassiveSkillIds { get; set; }
    }

    /// <summary>
    /// 属性模板配置（JSON 反序列化）
    /// </summary>
    public class JsonAttributeTemplate
    {
        public int Id { get; set; }
        public List<int> ActiveSkills { get; set; }
        public List<int> PassiveSkills { get; set; }
        public float Hp { get; set; }
        public float MaxHp { get; set; }
        public float PhysicsAttack { get; set; }
        public float MagicAttack { get; set; }
        public float PhysicsDefense { get; set; }
        public float MagicDefense { get; set; }
        public float MoveSpeed { get; set; }
    }

    /// <summary>
    /// 玩家生成数据
    /// 用于传递玩家信息到 BattleDriver 创建实体
    /// </summary>
    public class ETPlayerSpawnData
    {
        public int ActorId { get; set; }
        public int CharacterId { get; set; }
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
        }

        public ETPlayerSpawnData(int actorId, int characterId, string characterName, int teamId,
            float x, float y, float z)
        {
            ActorId = actorId;
            CharacterId = characterId;
            CharacterName = characterName;
            TeamId = teamId;
            PositionX = x;
            PositionY = y;
            PositionZ = z;
            RotationY = 0f;
            Scale = 1f;
            Hp = 500f;
            MaxHp = 500f;
        }

        public ETPlayerSpawnData(int actorId, int characterId, string characterName, int teamId,
            float x, float y, float z, float rotY, float scale, float hp, float maxHp)
        {
            ActorId = actorId;
            CharacterId = characterId;
            CharacterName = characterName;
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
