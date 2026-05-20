using System;
using AbilityKit.Demo.Moba.Console.Battle.Context;
using AbilityKit.Demo.Moba.Console.Battle.Config;
using AbilityKit.Demo.Moba.Share;
using ShareSyncMode = AbilityKit.Demo.Moba.Share.SyncMode;

namespace AbilityKit.Demo.Moba.Console.Battle.Flow.Steps
{
    /// <summary>
    /// CreateOrJoinWorld 阶段步骤组
    /// 负责创建或加入游戏世界
    /// </summary>
    public sealed class CreateOrJoinWorldSteps
    {
        private readonly ConsoleBattleContext _context;
        private readonly BattleStartConfig _config;

        private StepGroup _rootGroup = null!;
        private bool _worldCreated;

        public CreateOrJoinWorldSteps(ConsoleBattleContext context, BattleStartConfig config)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// 创建步骤组
        /// </summary>
        public StepGroup CreateStepGroup()
        {
            _rootGroup = new StepGroup("CreateOrJoinWorld", StepMode.Sequential);

            // 1. 设置本地玩家
            _rootGroup.AddStep(new SyncStep("SetupLocalPlayer", SetupLocalPlayer));

            // 2. 初始化世界状态
            _rootGroup.AddStep(new SyncStep("InitializeWorldState", InitializeWorldState));

            // 3. 根据同步模式选择创建或加入
            _rootGroup.AddStep(new ConditionalStep(
                "CreateOrJoin",
                () => _config.SyncMode == ShareSyncMode.Lockstep,
                thenAction: CreateLocalWorld,
                elseAction: JoinRemoteWorld
            ));

            // 4. 等待世界同步完成
            _rootGroup.AddStep(new ConditionalStep(
                "WaitForWorldSync",
                () => _config.SyncMode != ShareSyncMode.Lockstep,
                thenAction: WaitForWorldSync,
                elseAction: () => { } // 本地模式跳过
            ));

            return _rootGroup;
        }

        private void SetupLocalPlayer()
        {
            if (_config.Players != null && _config.Players.Count > 0)
            {
                var localPlayer = _config.Players[0];
                _context.LocalActorId = DeterministicHash.StringToActorId(localPlayer.PlayerId);
                Platform.Log.Battle($"[CreateOrJoinWorld] LocalPlayer: {localPlayer.Name} (ActorId: {_context.LocalActorId})");
            }
            else
            {
                _context.LocalActorId = 1;
                Platform.Log.Battle($"[CreateOrJoinWorld] Using default LocalActorId: {_context.LocalActorId}");
            }
        }

        private void InitializeWorldState()
        {
            // 初始化世界状态
            Platform.Log.Battle($"[CreateOrJoinWorld] World initialized: {_config.WorldId}");
        }

        private void CreateLocalWorld()
        {
            // 本地模式：创建本地世界
            _worldCreated = true;
            Platform.Log.Battle($"[CreateOrJoinWorld] Created local world: {_config.WorldId}");
        }

        private void JoinRemoteWorld()
        {
            // 远程模式：请求加入服务器世界
            Platform.Log.Battle($"[CreateOrJoinWorld] Requesting to join world: {_config.WorldId}");
        }

        private void WaitForWorldSync()
        {
            // 等待服务器同步世界状态
            Platform.Log.Battle("[CreateOrJoinWorld] Waiting for world sync...");
        }

        /// <summary>
        /// 世界是否已创建/加入
        /// </summary>
        public bool IsWorldReady => _worldCreated || _config.SyncMode != ShareSyncMode.Lockstep;

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
            _worldCreated = false;
        }
    }
}
