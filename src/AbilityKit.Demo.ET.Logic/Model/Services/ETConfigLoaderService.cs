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
            var path = "moba/characters.json";  // basePath 已经是 Configs，所以不需要再包含 Configs/

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
            var path = "moba/attribute_templates.json";  // basePath 已经是 Configs，所以不需要再包含 Configs/

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
    /// ET-side player spawn request data.
    /// Runtime ActorId is allocated by the Runtime enter-game flow, never by ET.Logic.
    /// </summary>
    public class ETPlayerSpawnData
    {
        /// <summary>
        /// Player identity used by room, input, and Runtime player-to-actor mapping.
        /// </summary>
        public string PlayerId { get; set; }

        public const int DefaultBasicAttackSkillId = 10010101;
        private static readonly int[] DefaultSkillIds = { 10010101, 10010201, 10010301 };

        public int CharacterId { get; set; }
        public int AttributeTemplateId { get; set; }
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
            BasicAttackSkillId = DefaultBasicAttackSkillId;
            SkillIds = CloneDefaultSkillIds();
        }

        public ETPlayerSpawnData(string playerId, int characterId, string characterName, int teamId,
            float x, float y, float z)
            : this(playerId, characterId, characterName, teamId, x, y, z, 0f, 1f, 500f, 500f)
        {
        }

        public ETPlayerSpawnData(string playerId, int characterId, string characterName, int teamId,
            float x, float y, float z, float rotY, float scale, float hp, float maxHp)
            : this(playerId, characterId, 0, characterName, teamId, x, y, z, rotY, scale, hp, maxHp)
        {
        }

        public ETPlayerSpawnData(string playerId, int characterId, int attributeTemplateId, string characterName, int teamId,
            float x, float y, float z, float rotY, float scale, float hp, float maxHp)
            : this(playerId, characterId, attributeTemplateId, DefaultBasicAttackSkillId, CloneDefaultSkillIds(), characterName, teamId, x, y, z, rotY, scale, hp, maxHp)
        {
        }

        public ETPlayerSpawnData(string playerId, int characterId, int attributeTemplateId, int basicAttackSkillId, int[] skillIds, string characterName, int teamId,
            float x, float y, float z, float rotY, float scale, float hp, float maxHp)
        {
            PlayerId = playerId ?? string.Empty;
            CharacterId = characterId;
            AttributeTemplateId = attributeTemplateId;
            BasicAttackSkillId = basicAttackSkillId > 0 ? basicAttackSkillId : DefaultBasicAttackSkillId;
            SkillIds = skillIds != null && skillIds.Length > 0 ? (int[])skillIds.Clone() : CloneDefaultSkillIds();
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

        public static int[] CloneDefaultSkillIds()
        {
            return (int[])DefaultSkillIds.Clone();
        }
    }
}
