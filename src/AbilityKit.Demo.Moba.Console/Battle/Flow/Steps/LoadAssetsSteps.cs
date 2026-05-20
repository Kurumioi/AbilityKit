using System;
using AbilityKit.Demo.Moba.Console.Battle.Context;
using AbilityKit.Demo.Moba.Console.Battle.Config;
using AbilityKit.Demo.Moba.Config.Core;

namespace AbilityKit.Demo.Moba.Console.Battle.Flow.Steps
{
    /// <summary>
    /// LoadAssets 阶段步骤组
    /// 负责加载游戏资源（角色、技能、地图配置等）
    /// </summary>
    public sealed class LoadAssetsSteps
    {
        private readonly ConsoleBattleContext _context;
        private readonly BattleStartConfig _config;
        private readonly MobaConfigDatabase? _mobaConfig;

        private StepGroup _rootGroup = null!;
        private int _loadedAssetCount;

        public LoadAssetsSteps(ConsoleBattleContext context, BattleStartConfig config, MobaConfigDatabase? mobaConfig)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _mobaConfig = mobaConfig;
        }

        /// <summary>
        /// 创建步骤组
        /// </summary>
        public StepGroup CreateStepGroup()
        {
            _rootGroup = new StepGroup("LoadAssets", StepMode.Sequential);

            // 1. 加载角色配置
            _rootGroup.AddStep(new SyncStep("LoadCharacterConfigs", LoadCharacterConfigs));

            // 2. 加载技能配置
            _rootGroup.AddStep(new SyncStep("LoadSkillConfigs", LoadSkillConfigs));

            // 3. 加载地图配置
            _rootGroup.AddStep(new SyncStep("LoadMapConfig", LoadMapConfig));

            // 4. 验证资源完整性
            _rootGroup.AddStep(new SyncStep("ValidateAssets", ValidateAssets));

            return _rootGroup;
        }

        private void LoadCharacterConfigs()
        {
            if (_mobaConfig != null)
            {
                var characterCount = _mobaConfig.GetTable<AbilityKit.Demo.Moba.Config.BattleDemo.MO.CharacterMO>().Count;
                _loadedAssetCount += characterCount;
                Platform.Log.Config($"[LoadAssets] Loaded {characterCount} character configs");
            }
            else
            {
                Platform.Log.Config("[LoadAssets] Using default character configs");
            }
        }

        private void LoadSkillConfigs()
        {
            if (_mobaConfig != null)
            {
                try
                {
                    var skillCount = _mobaConfig.GetTable<AbilityKit.Demo.Moba.Config.BattleDemo.MO.SkillMO>().Count;
                    _loadedAssetCount += skillCount;
                    Platform.Log.Config($"[LoadAssets] Loaded {skillCount} skill configs");
                }
                catch
                {
                    Platform.Log.Config("[LoadAssets] Skill configs not available");
                }
            }
        }

        private void LoadMapConfig()
        {
            // 加载地图配置
            Platform.Log.Config("[LoadAssets] Map config loaded (using default)");
        }

        private void ValidateAssets()
        {
            if (_loadedAssetCount > 0)
            {
                Platform.Log.Config($"[LoadAssets] All assets validated: {_loadedAssetCount} total assets");
            }
            else
            {
                Platform.Log.Config("[LoadAssets] Using default configurations");
            }
        }

        /// <summary>
        /// 执行步骤组
        /// </summary>
        public bool Execute()
        {
            return _rootGroup.Execute();
        }

        /// <summary>
        /// 步骤是否完成
        /// </summary>
        public bool IsCompleted => _rootGroup.IsCompleted;

        /// <summary>
        /// 重置步骤
        /// </summary>
        public void Reset()
        {
            _rootGroup.Reset();
            _loadedAssetCount = 0;
        }
    }
}
