using System;
using AbilityKit.Demo.Moba.Console.Battle.Config;
using AbilityKit.Demo.Moba.Console.Battle.Context;
using AbilityKit.Demo.Moba.Share;
using ShareSyncMode = AbilityKit.Demo.Moba.Share.SyncMode;
using IBattleSyncAdapter = AbilityKit.Demo.Moba.Console.Battle.Sync.IBattleSyncAdapter;
using StateSyncAdapter = AbilityKit.Demo.Moba.Console.Battle.Sync.StateSyncAdapter;

namespace AbilityKit.Demo.Moba.Console.Battle.Flow.Steps
{
    /// <summary>
    /// Connect 阶段步骤组
    /// 负责连接服务器（状态同步模式）或初始化本地连接
    /// </summary>
    public sealed class ConnectSteps
    {
        private readonly ConsoleBattleContext _context;
        private readonly BattleStartConfig _config;
        private readonly IBattleSyncAdapter _syncAdapter;

        private StepGroup _rootGroup = null!;
        private bool _connectRequested;

        public ConnectSteps(ConsoleBattleContext context, BattleStartConfig config, IBattleSyncAdapter syncAdapter)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _syncAdapter = syncAdapter ?? throw new ArgumentNullException(nameof(syncAdapter));
        }

        /// <summary>
        /// 创建步骤组
        /// </summary>
        public StepGroup CreateStepGroup()
        {
            _rootGroup = new StepGroup("Connect", StepMode.Sequential);

            // 1. 检查同步模式
            _rootGroup.AddStep(new ConditionalStep(
                "CheckSyncMode",
                () => _config.SyncMode == ShareSyncMode.SnapshotAuthority,
                thenAction: () => { _connectRequested = false; Platform.Log.Sync("[Connect] StateSync mode detected"); },
                elseAction: () => { _connectRequested = false; Platform.Log.Sync("[Connect] Local mode (no server connection needed)"); }
            ));

            // 2. 连接服务器（仅状态同步模式）
            _rootGroup.AddStep(new ConditionalStep(
                "ConnectToServer",
                () => _config.SyncMode == ShareSyncMode.SnapshotAuthority && !_connectRequested,
                thenAction: RequestConnect
            ));

            // 3. 等待连接完成（可跳过本地模式）
            _rootGroup.AddStep(new ConditionalStep(
                "WaitForConnection",
                () => _config.SyncMode == ShareSyncMode.SnapshotAuthority,
                thenAction: WaitForConnection,
                elseAction: () => { } // 本地模式跳过
            ));

            return _rootGroup;
        }

        private void RequestConnect()
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
                Platform.Log.Sync($"[Connect] Connection requested to {_config.Network?.Host ?? "localhost"}:{_config.Network?.Port ?? 4000}");
            }
        }

        private void WaitForConnection()
        {
            // 状态同步模式下等待连接建立
            // 这里简化为立即完成，实际应该等待 OnConnectionChanged 事件
            if (_syncAdapter.IsConnected)
            {
                Platform.Log.Sync("[Connect] Connected successfully");
            }
            else
            {
                Platform.Log.Sync("[Connect] Waiting for connection...");
            }
        }

        /// <summary>
        /// 处理连接状态变更
        /// </summary>
        public void OnConnectionChanged(bool connected)
        {
            if (connected)
            {
                Platform.Log.Sync("[Connect] Connection established!");
            }
            else
            {
                Platform.Log.Warn("[Connect] Connection lost");
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
            _connectRequested = false;
        }
    }
}
