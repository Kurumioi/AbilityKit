using System;
using AbilityKit.Demo.Moba.Console.Battle.Context;
using AbilityKit.Demo.Moba.Console.Battle.Config;
using AbilityKit.Demo.Moba.Config.Core;

namespace AbilityKit.Demo.Moba.Console.Battle.Flow.Steps
{
    /// <summary>
    /// InMatch 阶段步骤组
    /// 负责战斗初始化、实体生成、战斗开始等
    /// </summary>
    public sealed class InMatchSteps
    {
        private readonly ConsoleBattleContext _context;
        private readonly BattleStartConfig _config;
        private readonly MobaConfigDatabase? _mobaConfig;
        private readonly Action _onBattleStarted;

        private StepGroup _rootGroup = null!;
        private int _spawnedActorCount;

        public InMatchSteps(
            ConsoleBattleContext context,
            BattleStartConfig config,
            MobaConfigDatabase? mobaConfig,
            Action? onBattleStarted = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _mobaConfig = mobaConfig;
            _onBattleStarted = onBattleStarted ?? (() => { });
        }

        /// <summary>
        /// 创建步骤组
        /// </summary>
        public StepGroup CreateStepGroup()
        {
            _rootGroup = new StepGroup("InMatch", StepMode.Sequential);

            // 1. 注册玩家实体
            _rootGroup.AddStep(new SyncStep("RegisterPlayerEntities", RegisterPlayerEntities));

            // 2. 注册本地玩家
            _rootGroup.AddStep(new SyncStep("RegisterLocalPlayer", RegisterLocalPlayer));

            // 3. 初始化战斗状态
            _rootGroup.AddStep(new SyncStep("InitializeBattleState", InitializeBattleState));

            // 4. 触发战斗开始
            _rootGroup.AddStep(new SyncStep("NotifyBattleStarted", NotifyBattleStarted));

            return _rootGroup;
        }

        private void RegisterPlayerEntities()
        {
            if (_config.Players == null || _config.Players.Count == 0)
            {
                Platform.Log.Battle("[InMatch] No players configured");
                return;
            }

            foreach (var player in _config.Players)
            {
                CreateCharacterFromPlayer(player);
            }

            _spawnedActorCount = _config.Players.Count;
            Platform.Log.Battle($"[InMatch] Spawned {_spawnedActorCount} actors");
        }

        private void CreateCharacterFromPlayer(PlayerConfig player)
        {
            float physicsAttack = 10f;
            float physicsDefense = 0f;
            float moveSpeed = 5f;
            float hp = 500f;
            float maxHp = 500f;

            if (_mobaConfig?.TryGetCharacter(player.HeroId, out var charConfig) == true && charConfig != null)
            {
                if (_mobaConfig.TryGetAttributeTemplate(charConfig.AttributeTemplateId, out var attrs) && attrs != null)
                {
                    physicsAttack = attrs.PhysicsAttack;
                    physicsDefense = attrs.PhysicsDefense;
                    moveSpeed = attrs.MoveSpeed;
                    hp = attrs.Hp;
                    maxHp = attrs.MaxHp;
                }

                Platform.Log.Battle($"[InMatch] Spawned {charConfig.Name} (Team {player.TeamId}) at ({player.PositionX:F1}, {player.PositionZ:F1})");
            }
            else
            {
                Platform.Log.Warn($"[InMatch] Character config not found for HeroId: {player.HeroId}, using defaults");
            }
        }

        private void RegisterLocalPlayer()
        {
            if (_config.Players != null && _config.Players.Count > 0)
            {
                var localPlayer = _config.Players[0];
                _context.LocalActorId = DeterministicHash.StringToActorId(localPlayer.PlayerId);
                Platform.Log.Battle($"[InMatch] LocalPlayer registered: ActorId={_context.LocalActorId}");
            }
        }

        private void InitializeBattleState()
        {
            _context.State = BattleState.InMatch;
            _context.IsInitialized = true;
            Platform.Log.Battle($"[InMatch] Battle state initialized");
        }

        private void NotifyBattleStarted()
        {
            Platform.Log.Title("BATTLE STARTED");
            Platform.Log.Battle($"Total actors: {_spawnedActorCount}");
            _onBattleStarted();
        }

        /// <summary>
        /// 获取生成的角色数量
        /// </summary>
        public int SpawnedActorCount => _spawnedActorCount;

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
            _spawnedActorCount = 0;
        }
    }
}
