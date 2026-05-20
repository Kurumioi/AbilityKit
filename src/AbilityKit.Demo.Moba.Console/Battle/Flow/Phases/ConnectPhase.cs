using System;
using AbilityKit.Demo.Moba.Console.Battle.Context;
using AbilityKit.Demo.Moba.Console.Battle.Config;
using AbilityKit.Demo.Moba.Share;
using ShareSyncMode = AbilityKit.Demo.Moba.Share.SyncMode;
using IBattleSyncAdapter = AbilityKit.Demo.Moba.Console.Battle.Sync.IBattleSyncAdapter;
using StateSyncAdapter = AbilityKit.Demo.Moba.Console.Battle.Sync.StateSyncAdapter;

namespace AbilityKit.Demo.Moba.Console.Battle.Flow
{
    /// <summary>
    /// 连接阶段
    /// 使用步骤系统管理连接流程
    /// </summary>
    public sealed class ConnectPhase : IPhase, IStepBasedPhase
    {
        private StepState _state = StepState.Pending;
        private int _currentStep;
        private ConsoleBattleContext? _context;
        private BattleStartConfig? _config;
        private IBattleSyncAdapter? _syncAdapter;
        private bool _connectRequested;
        private bool _connectionCompleted;

        public string Name => "Connect";

        public void SetContext(
            ConsoleBattleContext context,
            BattleStartConfig config,
            IBattleSyncAdapter syncAdapter)
        {
            _context = context;
            _config = config;
            _syncAdapter = syncAdapter;
        }

        public void OnEnter(PhaseContext context)
        {
            Platform.Log.Phase("[Connect] Entered Connect phase");
            _state = StepState.Running;
            _currentStep = 0;
            _connectRequested = false;
            _connectionCompleted = false;
        }

        public void OnTick(PhaseContext context, float deltaTime)
        {
            if (_state != StepState.Running || _config == null) return;

            switch (_currentStep)
            {
                case 0:
                    ExecuteCheckSyncMode();
                    break;
                case 1:
                    ExecuteConnect();
                    break;
                case 2:
                    ExecuteWaitForConnection();
                    break;
                case 3:
                    _state = StepState.Completed;
                    Platform.Log.Phase("[Connect] All steps completed");
                    break;
            }
        }

        private void ExecuteCheckSyncMode()
        {
            if (_config == null) { _currentStep++; return; }

            if (_config.SyncMode == ShareSyncMode.SnapshotAuthority)
            {
                Platform.Log.Phase("[Connect] Step 1/3: StateSync mode detected, connection required");
            }
            else
            {
                Platform.Log.Phase("[Connect] Step 1/3: Local mode, no server connection needed");
            }
            _currentStep++;
        }

        private void ExecuteConnect()
        {
            if (_config == null || _syncAdapter == null) { _currentStep++; return; }

            if (_config.SyncMode == ShareSyncMode.SnapshotAuthority && !_connectRequested)
            {
                if (_syncAdapter is StateSyncAdapter stateSync)
                {
                    if (_config.Network != null)
                    {
                        stateSync.Connect();
                    }
                    else
                    {
                        stateSync.Connect(
                            host: "localhost",
                            port: 4000,
                            roomId: _config.WorldId,
                            playerId: _config.PlayerId
                        );
                    }
                    _connectRequested = true;
                    Platform.Log.Phase($"[Connect] Step 2/3: Connection requested");
                }
            }
            _currentStep++;
        }

        private void ExecuteWaitForConnection()
        {
            if (_config == null || _syncAdapter == null) { _currentStep++; return; }

            if (_config.SyncMode == ShareSyncMode.SnapshotAuthority)
            {
                // 检查连接状态
                if (_syncAdapter.IsConnected || _connectionCompleted)
                {
                    Platform.Log.Phase("[Connect] Step 3/3: Connected successfully");
                    _currentStep++;
                }
                else
                {
                    // 继续等待
                    return;
                }
            }
            else
            {
                // 本地模式直接完成
                _connectionCompleted = true;
                Platform.Log.Phase("[Connect] Step 3/3: Local mode, skipped");
                _currentStep++;
            }
        }

        public void OnExit(PhaseContext context, string nextPhase)
        {
            Platform.Log.Phase($"[Connect] Exiting to {nextPhase}");
            _state = StepState.Pending;
            _currentStep = 0;
        }

        /// <summary>
        /// 触发连接完成事件
        /// </summary>
        public void NotifyConnectionChanged(bool connected)
        {
            if (connected)
            {
                _connectionCompleted = true;
                Platform.Log.Sync("[Connect] Connection established!");
            }
            else
            {
                Platform.Log.Warn("[Connect] Connection lost");
            }
        }

        public bool IsStepCompleted => _state == StepState.Completed;
        public bool IsStepFailed => _state == StepState.Failed;
        public string CurrentStepName => _currentStep switch
        {
            0 => "CheckSyncMode",
            1 => "Connect",
            2 => "WaitForConnection",
            _ => "Completed"
        };
    }
}
