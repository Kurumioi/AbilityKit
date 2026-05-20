using System;
using AbilityKit.Demo.Moba.Console.Battle.Context;
using AbilityKit.Demo.Moba.Console.Battle.Config;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.Share;
using ShareSyncMode = AbilityKit.Demo.Moba.Share.SyncMode;

namespace AbilityKit.Demo.Moba.Console.Battle.Flow
{
    /// <summary>
    /// 创建或加入世界阶段
    /// 使用步骤系统管理世界创建流程
    /// </summary>
    public sealed class CreateOrJoinWorldPhase : IPhase, IStepBasedPhase
    {
        private StepState _state = StepState.Pending;
        private int _currentStep;
        private ConsoleBattleContext? _context;
        private BattleStartConfig? _config;
        private MobaConfigDatabase? _mobaConfig;
        private bool _worldCreated;

        public string Name => "CreateOrJoinWorld";

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
            Platform.Log.Phase("[CreateOrJoinWorld] Entered CreateOrJoinWorld phase");
            _state = StepState.Running;
            _currentStep = 0;
            _worldCreated = false;
        }

        public void OnTick(PhaseContext context, float deltaTime)
        {
            if (_state != StepState.Running || _config == null) return;

            switch (_currentStep)
            {
                case 0:
                    ExecuteSetupLocalPlayer();
                    break;
                case 1:
                    ExecuteInitializeWorldState();
                    break;
                case 2:
                    ExecuteCreateOrJoin();
                    break;
                case 3:
                    ExecuteWaitForWorldSync();
                    break;
                case 4:
                    _state = StepState.Completed;
                    Platform.Log.Phase("[CreateOrJoinWorld] All steps completed");
                    break;
            }
        }

        private void ExecuteSetupLocalPlayer()
        {
            if (_context == null || _config == null) { _currentStep++; return; }

            if (_config.Players != null && _config.Players.Count > 0)
            {
                var localPlayer = _config.Players[0];
                _context.LocalActorId = DeterministicHash.StringToActorId(localPlayer.PlayerId);
                Platform.Log.Phase($"[CreateOrJoinWorld] Step 1/4: LocalPlayer set: {localPlayer.Name} (ActorId: {_context.LocalActorId})");
            }
            else
            {
                _context.LocalActorId = 1;
                Platform.Log.Phase($"[CreateOrJoinWorld] Step 1/4: Using default LocalActorId: {_context.LocalActorId}");
            }
            _currentStep++;
        }

        private void ExecuteInitializeWorldState()
        {
            if (_config == null) { _currentStep++; return; }

            Platform.Log.Phase($"[CreateOrJoinWorld] Step 2/4: World initialized: {_config.WorldId}");
            _currentStep++;
        }

        private void ExecuteCreateOrJoin()
        {
            if (_config == null) { _currentStep++; return; }

            if (_config.SyncMode == ShareSyncMode.Lockstep)
            {
                // 本地模式：创建本地世界
                _worldCreated = true;
                Platform.Log.Phase($"[CreateOrJoinWorld] Step 3/4: Created local world: {_config.WorldId}");
            }
            else
            {
                // 远程模式：请求加入服务器世界
                Platform.Log.Phase($"[CreateOrJoinWorld] Step 3/4: Requesting to join world: {_config.WorldId}");
            }
            _currentStep++;
        }

        private void ExecuteWaitForWorldSync()
        {
            if (_config == null) { _currentStep++; return; }

            if (_config.SyncMode != ShareSyncMode.Lockstep)
            {
                // 远程模式等待同步完成
                // 这里简化处理，实际应该等待服务器响应
                Platform.Log.Phase("[CreateOrJoinWorld] Step 4/4: Waiting for world sync...");
            }
            else
            {
                // 本地模式直接完成
                Platform.Log.Phase("[CreateOrJoinWorld] Step 4/4: Local world, sync skipped");
            }
            _currentStep++;
        }

        public void OnExit(PhaseContext context, string nextPhase)
        {
            Platform.Log.Phase($"[CreateOrJoinWorld] Exiting to {nextPhase}");
            _state = StepState.Pending;
            _currentStep = 0;
        }

        public bool IsWorldReady => _worldCreated || (_config?.SyncMode != ShareSyncMode.Lockstep);

        public bool IsStepCompleted => _state == StepState.Completed;
        public bool IsStepFailed => _state == StepState.Failed;
        public string CurrentStepName => _currentStep switch
        {
            0 => "SetupLocalPlayer",
            1 => "InitializeWorldState",
            2 => "CreateOrJoin",
            3 => "WaitForWorldSync",
            _ => "Completed"
        };
    }
}
