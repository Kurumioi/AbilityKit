using System;
using AbilityKit.Demo.Moba.Console.Battle.Config;
using AbilityKit.Demo.Moba.Console.Battle.Context;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World;
using AbilityKit.Ability.World.Services;
using System.Collections.Generic;

namespace AbilityKit.Demo.Moba.Console.Battle.Flow.Steps
{
    /// <summary>
    /// Prepare 阶段步骤组
    /// 负责加载配置、构建世界
    /// </summary>
    public sealed class PrepareSteps
    {
        private readonly ConsoleBattleContext _context;
        private readonly BattleStartConfig _config;
        private readonly IEnumerable<IWorldModule> _modules;

        private StepGroup _rootGroup = null!;
        private IWorldResolver _worldResolver = null!;

        public PrepareSteps(ConsoleBattleContext context, BattleStartConfig config, IEnumerable<IWorldModule> modules)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _modules = modules ?? throw new ArgumentNullException(nameof(modules));
        }

        /// <summary>
        /// 创建步骤组
        /// </summary>
        public StepGroup CreateStepGroup()
        {
            _rootGroup = new StepGroup("Prepare", StepMode.Sequential);

            // 1. 配置 World DI 容器
            _rootGroup.AddStep(new SyncStep("ConfigureWorld", ConfigureWorld));

            // 2. 初始化 ECS 世界
            _rootGroup.AddStep(new SyncStep("InitializeEcsWorld", InitializeEcsWorld));

            // 3. 验证配置
            _rootGroup.AddStep(new SyncStep("ValidateConfig", ValidateConfig));

            return _rootGroup;
        }

        private void ConfigureWorld()
        {
            var builder = new WorldContainerBuilder();

            foreach (var module in _modules)
            {
                module.Configure(builder);
            }

            _worldResolver = builder.Build();

            // 手动 resolve TriggerPlanJsonDatabase 来触发其加载
            Platform.Log.System("[Prepare] Resolving TriggerPlanJsonDatabase...");
            try
            {
                var triggerDb = _worldResolver.Resolve<AbilityKit.Triggering.Runtime.Plan.Json.TriggerPlanJsonDatabase>();
                Platform.Log.System($"[Prepare] TriggerPlanJsonDatabase resolved, records={triggerDb?.Records?.Count ?? 0}");
            }
            catch (Exception ex)
            {
                Platform.Log.Error($"[Prepare] Failed to resolve TriggerPlanJsonDatabase: {ex.Message}");
            }

            Platform.Log.System("[Prepare] World configured");
        }

        private void InitializeEcsWorld()
        {
            _context.InitializeEcsWorld();
            Platform.Log.System("[Prepare] ECS World initialized");
        }

        private void ValidateConfig()
        {
            if (_config == null)
            {
                throw new InvalidOperationException("BattleStartConfig is null");
            }

            if (string.IsNullOrEmpty(_config.WorldId))
            {
                throw new InvalidOperationException("WorldId is required");
            }

            Platform.Log.System("[Prepare] Config validated");
        }

        /// <summary>
        /// 获取构建的世界解析器
        /// </summary>
        public IWorldResolver WorldResolver => _worldResolver;

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
        }
    }
}
