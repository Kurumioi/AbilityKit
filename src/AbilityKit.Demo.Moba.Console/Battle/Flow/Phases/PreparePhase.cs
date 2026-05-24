using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Console.Battle.Context;
using AbilityKit.Demo.Moba.Console.Battle.Config;
using AbilityKit.Ability.World.DI;

namespace AbilityKit.Demo.Moba.Console.Battle.Flow
{
    /// <summary>
    /// 准备阶段
    /// 使用步骤系统管理配置加载流程
    /// </summary>
    public sealed class PreparePhase : IPhase, IStepBasedPhase
    {
        private StepState _state = StepState.Pending;
        private int _currentStep;
        private ConsoleBattleContext? _context;
        private BattleStartConfig? _config;
        private IWorldResolver? _worldResolver;

        public string Name => "Prepare";

        public void SetContext(ConsoleBattleContext context, BattleStartConfig config, IEnumerable<IWorldModule> modules)
        {
            _context = context;
            _config = config;
            _modules = modules;
        }

        private IEnumerable<IWorldModule>? _modules;

        public void OnEnter(PhaseContext context)
        {
            _state = StepState.Running;
            _currentStep = 0;
        }

        public void OnTick(PhaseContext context, float deltaTime)
        {
            if (_state != StepState.Running || _context == null || _config == null) return;

            switch (_currentStep)
            {
                case 0:
                    ExecuteConfigureWorld();
                    break;
                case 1:
                    ExecuteInitializeEcsWorld();
                    break;
                case 2:
                    ExecuteValidateConfig();
                    break;
                case 3:
                    _state = StepState.Completed;
                    Platform.Log.Phase("[Prepare] All steps completed");
                    break;
            }
        }

        private void ExecuteConfigureWorld()
        {
            // 配置已在 Bootstrapper.ConfigureWorld() 中完成
            // 这里只需要记录日志
            Platform.Log.Phase("[Prepare] Step 1/3: World configured (via Bootstrapper)");
            _currentStep++;
        }

        private void ExecuteInitializeEcsWorld()
        {
            if (_context == null) { _currentStep++; return; }

            _context.InitializeEcsWorld();
            Platform.Log.Phase("[Prepare] Step 2/3: ECS World initialized");
            _currentStep++;
        }

        private void ExecuteValidateConfig()
        {
            if (_config == null) { _currentStep++; return; }

            if (string.IsNullOrEmpty(_config.WorldId))
            {
                Platform.Log.Error("[Prepare] Validation failed: WorldId is required");
                _state = StepState.Failed;
                return;
            }

            Platform.Log.Phase("[Prepare] Step 3/3: Config validated");
            _currentStep++;
        }

        public void OnExit(PhaseContext context, string nextPhase)
        {
            Platform.Log.Phase($"[Prepare] Exiting to {nextPhase}");

            if (_state == StepState.Failed)
            {
                Platform.Log.Error("[Prepare] Phase failed");
            }

            _state = StepState.Pending;
            _currentStep = 0;
        }

        public bool IsStepCompleted => _state == StepState.Completed;
        public bool IsStepFailed => _state == StepState.Failed;
        public string CurrentStepName => _currentStep switch
        {
            0 => "ConfigureWorld",
            1 => "InitializeEcsWorld",
            2 => "ValidateConfig",
            _ => "Completed"
        };
    }

    /// <summary>
    /// 基于步骤的阶段接口
    /// </summary>
    public interface IStepBasedPhase
    {
        bool IsStepCompleted { get; }
        bool IsStepFailed { get; }
        string CurrentStepName { get; }
    }

    internal enum StepState
    {
        Pending,
        Running,
        Completed,
        Failed
    }
}
