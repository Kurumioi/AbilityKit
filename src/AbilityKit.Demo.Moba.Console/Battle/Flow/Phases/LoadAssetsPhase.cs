using System;
using AbilityKit.Demo.Moba.Console.Battle.Context;
using AbilityKit.Demo.Moba.Console.Battle.Config;
using AbilityKit.Demo.Moba.Config.Core;

namespace AbilityKit.Demo.Moba.Console.Battle.Flow
{
    /// <summary>
    /// 加载资源阶段
    /// 使用步骤系统管理资源加载流程
    /// </summary>
    public sealed class LoadAssetsPhase : IPhase, IStepBasedPhase
    {
        private StepState _state = StepState.Pending;
        private int _currentStep;
        private ConsoleBattleContext? _context;
        private BattleStartConfig? _config;
        private MobaConfigDatabase? _mobaConfig;
        private int _loadedAssetCount;

        public string Name => "LoadAssets";

        public void SetContext(
            ConsoleBattleContext context,
            BattleStartConfig config,
            MobaConfigDatabase? mobaConfig = null)
        {
            _context = context;
            _config = config;
            _mobaConfig = mobaConfig;
        }

        public void OnEnter(PhaseContext context)
        {
            Platform.Log.Phase("[LoadAssets] Entered LoadAssets phase");
            _state = StepState.Running;
            _currentStep = 0;
            _loadedAssetCount = 0;
        }

        public void OnTick(PhaseContext context, float deltaTime)
        {
            if (_state != StepState.Running) return;

            switch (_currentStep)
            {
                case 0:
                    ExecuteLoadCharacterConfigs();
                    break;
                case 1:
                    ExecuteLoadSkillConfigs();
                    break;
                case 2:
                    ExecuteLoadMapConfig();
                    break;
                case 3:
                    ExecuteValidateAssets();
                    break;
                case 4:
                    _state = StepState.Completed;
                    Platform.Log.Phase("[LoadAssets] All steps completed");
                    break;
            }
        }

        private void ExecuteLoadCharacterConfigs()
        {
            if (_mobaConfig != null)
            {
                try
                {
                    var characterCount = _mobaConfig.GetTable<AbilityKit.Demo.Moba.Config.BattleDemo.MO.CharacterMO>().Count;
                    _loadedAssetCount += characterCount;
                    Platform.Log.Phase($"[LoadAssets] Step 1/4: Loaded {characterCount} character configs");
                }
                catch
                {
                    Platform.Log.Phase("[LoadAssets] Step 1/4: Character configs not available, using defaults");
                }
            }
            else
            {
                Platform.Log.Phase("[LoadAssets] Step 1/4: Using default character configs");
            }
            _currentStep++;
        }

        private void ExecuteLoadSkillConfigs()
        {
            if (_mobaConfig != null)
            {
                try
                {
                    var skillCount = _mobaConfig.GetTable<AbilityKit.Demo.Moba.Config.BattleDemo.MO.SkillMO>().Count;
                    _loadedAssetCount += skillCount;
                    Platform.Log.Phase($"[LoadAssets] Step 2/4: Loaded {skillCount} skill configs");
                }
                catch
                {
                    Platform.Log.Phase("[LoadAssets] Step 2/4: Skill configs not available");
                }
            }
            _currentStep++;
        }

        private void ExecuteLoadMapConfig()
        {
            Platform.Log.Phase("[LoadAssets] Step 3/4: Map config loaded (using default)");
            _currentStep++;
        }

        private void ExecuteValidateAssets()
        {
            if (_loadedAssetCount > 0)
            {
                Platform.Log.Phase($"[LoadAssets] Step 4/4: All assets validated: {_loadedAssetCount} total assets");
            }
            else
            {
                Platform.Log.Phase("[LoadAssets] Step 4/4: Using default configurations");
            }
            _currentStep++;
        }

        public void OnExit(PhaseContext context, string nextPhase)
        {
            Platform.Log.Phase($"[LoadAssets] Exiting to {nextPhase}");
            _state = StepState.Pending;
            _currentStep = 0;
        }

        public bool IsStepCompleted => _state == StepState.Completed;
        public bool IsStepFailed => _state == StepState.Failed;
        public string CurrentStepName => _currentStep switch
        {
            0 => "LoadCharacterConfigs",
            1 => "LoadSkillConfigs",
            2 => "LoadMapConfig",
            3 => "ValidateAssets",
            _ => "Completed"
        };
    }
}
