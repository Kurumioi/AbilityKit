using System;
using AbilityKit.Demo.Moba.Console.Battle.Context;
using AbilityKit.Demo.Moba.Console.Battle.Config;
using AbilityKit.Demo.Moba.Console.Battle.ECS;
using AbilityKit.Demo.Moba.Console.Battle.ECS.Entities;
using AbilityKit.Demo.Moba.Console.Battle.Features;
using AbilityKit.Demo.Moba.Config.Core;
using EC = AbilityKit.World.ECS;

namespace AbilityKit.Demo.Moba.Console.Battle.Flow
{
    /// <summary>
    /// 战斗中阶段
    /// 使用步骤系统管理战斗初始化流程
    /// </summary>
    public sealed class InMatchPhase : IPhase, IStepBasedPhase
    {
        private readonly IBattleFlow _flow;
        private readonly IBattleFlowEvents _events;
        private readonly FeatureHost _features;
        private readonly BattleEntityFeature _entityFeature;

        private StepState _state = StepState.Pending;
        private int _currentStep;
        private double _battleTime;

        private ConsoleBattleContext? _context;
        private BattleStartConfig? _config;
        private MobaConfigDatabase? _mobaConfig;
        private int _spawnedActorCount;

        public string Name => "InMatch";
        public FeatureHost Features => _features;

        public InMatchPhase(IBattleFlow flow, IBattleFlowEvents events)
        {
            _flow = flow;
            _events = events;
            _features = new FeatureHost();
            _entityFeature = new BattleEntityFeature();

            // BattleEntityFeature 必须最先添加，确保 ECS 世界在所有 Feature 之前创建
            _features.Add(_entityFeature);
        }

        public void SetContext(
            ConsoleBattleContext context,
            BattleStartConfig config,
            MobaConfigDatabase? mobaConfig = null)
        {
            _context = context;
            _config = config;
            _mobaConfig = mobaConfig;
        }

        /// <summary>
        /// 配置战斗 Feature
        /// 由 Bootstrapper 调用
        /// 注意：BattleEntityFeature 已在此构造函数中自动添加
        /// </summary>
        public void ConfigureFeatures(Action<FeatureHost> configure)
        {
            configure(_features);
        }

        public void OnEnter(PhaseContext context)
        {
            Platform.Log.Phase("[InMatch] Entered InMatch phase");
            _state = StepState.Running;
            _currentStep = 0;
            _battleTime = 0;
            _spawnedActorCount = 0;

            // 获取 FeatureContext - 使用 IBattleFlow 自身
            if (_flow is IFeatureContext featureCtx)
            {
                _features.Attach(featureCtx);
            }
        }

        public void OnTick(PhaseContext context, float deltaTime)
        {
            _battleTime += deltaTime;

            if (_state == StepState.Running)
            {
                TickInitSteps();
            }

            // Features 由 InMatch 阶段管理（与 BattleFlow 并行）
            _features.Tick(deltaTime);
        }

        private void TickInitSteps()
        {
            if (_state != StepState.Running) return;

            switch (_currentStep)
            {
                case 0:
                    ExecuteRegisterPlayerEntities();
                    break;
                case 1:
                    ExecuteRegisterLocalPlayer();
                    break;
                case 2:
                    ExecuteInitializeBattleState();
                    break;
                case 3:
                    ExecuteNotifyBattleStarted();
                    break;
                case 4:
                    _state = StepState.Completed;
                    Platform.Log.Phase("[InMatch] All init steps completed");
                    break;
            }
        }

        private void ExecuteRegisterPlayerEntities()
        {
            if (_config == null || _config.Players == null || _config.Players.Count == 0)
            {
                Platform.Log.Battle("[InMatch] Step 1/4: No players configured");
                _currentStep++;
                return;
            }

            foreach (var player in _config.Players)
            {
                SpawnCharacter(player);
            }

            _spawnedActorCount = _config.Players.Count;
            Platform.Log.Phase($"[InMatch] Step 1/4: Spawned {_spawnedActorCount} actors");
            _currentStep++;
        }

        private void SpawnCharacter(PlayerConfig player)
        {
            if (_context?.EntityFactory == null)
            {
                Platform.Log.Error("[InMatch] EntityFactory is null, cannot spawn character");
                return;
            }

            float hp = 500f;
            float maxHp = 500f;
            float physicsAttack = 10f;
            float physicsDefense = 0f;
            float moveSpeed = 5f;

            if (_mobaConfig?.TryGetCharacter(player.HeroId, out var charConfig) == true && charConfig != null)
            {
                if (_mobaConfig.TryGetAttributeTemplate(charConfig.AttributeTemplateId, out var attrs) && attrs != null)
                {
                    hp = attrs.Hp;
                    maxHp = attrs.MaxHp;
                    physicsAttack = attrs.PhysicsAttack;
                    physicsDefense = attrs.PhysicsDefense;
                    moveSpeed = attrs.MoveSpeed;
                }

                // 使用 EntityFactory 创建角色实体
                var actorId = DeterministicHash.StringToActorId(player.PlayerId);
                var netId = _context.EntityFactory.CreateCharacter(actorId);

                // 设置变换组件
                if (_context.EntityQuery.TryGetTransform(netId, out var transform))
                {
                    transform.X = player.PositionX;
                    transform.Y = player.PositionY;
                    transform.Z = player.PositionZ;
                }

                // 设置角色组件
                if (_context.EntityQuery.TryGetCharacter(netId, out var character))
                {
                    character.HpMax = maxHp;
                    character.Hp = hp;
                    character.PhysicsAttack = physicsAttack;
                    character.PhysicsDefense = physicsDefense;
                    character.MoveSpeed = moveSpeed;
                    character.TeamId = player.TeamId;
                }

                Platform.Log.Battle($"[InMatch] Spawned {charConfig.Name} (Team {player.TeamId}) at ({player.PositionX:F1}, {player.PositionZ:F1}) as NetId={netId.Value}");
            }
            else
            {
                Platform.Log.Warn($"[InMatch] Character config not found for HeroId: {player.HeroId}");
            }
        }

        private void ExecuteRegisterLocalPlayer()
        {
            if (_context == null || _config == null) { _currentStep++; return; }

            if (_config.Players != null && _config.Players.Count > 0)
            {
                var localPlayer = _config.Players[0];
                _context.LocalActorId = DeterministicHash.StringToActorId(localPlayer.PlayerId);
                Platform.Log.Phase($"[InMatch] Step 2/4: LocalPlayer registered: ActorId={_context.LocalActorId}");
            }
            _currentStep++;
        }

        private void ExecuteInitializeBattleState()
        {
            if (_context == null) { _currentStep++; return; }

            _context.State = BattleState.InMatch;
            _context.IsInitialized = true;
            Platform.Log.Phase("[InMatch] Step 3/4: Battle state initialized");
            _currentStep++;
        }

        private void ExecuteNotifyBattleStarted()
        {
            Platform.Log.Title("BATTLE STARTED");
            Platform.Log.Phase($"[InMatch] Step 4/4: Total actors: {_spawnedActorCount}");
            _events.BattleStarted?.Invoke();
            _currentStep++;
        }

        public void OnExit(PhaseContext context, string nextPhase)
        {
            Platform.Log.Phase($"[InMatch] Exiting to {nextPhase}");
            _features.Detach();
            _events.BattleEnded?.Invoke();

            _state = StepState.Pending;
            _currentStep = 0;
        }

        public void EndBattle()
        {
            Platform.Log.Title("BATTLE ENDED");
            Platform.Log.Battle($"Total battle time: {_battleTime:F0}s");
            _flow?.TransitionTo("End");
        }

        public double BattleTime => _battleTime;

        public bool IsStepCompleted => _state == StepState.Completed;
        public bool IsStepFailed => _state == StepState.Failed;
        public string CurrentStepName => _currentStep switch
        {
            0 => "RegisterPlayerEntities",
            1 => "RegisterLocalPlayer",
            2 => "InitializeBattleState",
            3 => "NotifyBattleStarted",
            _ => "BattleRunning"
        };
    }
}
