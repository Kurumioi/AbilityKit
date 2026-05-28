using System;
using AbilityKit.Ability.Config;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Ability.World;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.DI;
using AbilityKit.Demo.Moba.Share;

namespace ET.Logic
{
    /// <summary>
    /// 初始化处理器
    /// </summary>
    [LifecycleHandler(LifecyclePhase.Initialize)]
    public sealed class InitializeHandler : IInitializeHandler
    {
        public LifecyclePhase Phase => LifecyclePhase.Initialize;

        public void Handle(ETMobaBattleDriver driver, in BattleStartPlan plan, IBattleViewEventSink viewSink)
        {
            if (driver == null)
                throw new ArgumentNullException(nameof(driver));
            if (viewSink == null)
                throw new ArgumentNullException(nameof(viewSink));

            driver.Plan = plan;
            driver.ViewSink = viewSink;
            driver.TextAssetLoader = null;
            driver.TickRate = plan.TickRate > 0 ? plan.TickRate : 30;

            try
            {
                // 初始化配置加载器
                InitializeConfigLoader(driver);

                // 初始化快照分发器
                driver.SnapshotDispatcher = new FrameSnapshotDispatcher();

                // 创建 World
                InitializeWorld(driver, plan);

                // 重置状态
                driver.CurrentFrame = 0;
                driver.LogicTimeSeconds = 0;
                driver.IsRunning = false;

                Log.Info($"[InitializeHandler] Done: TickRate={driver.TickRate}, WorldId={driver.Plan.WorldId}");
                Log.Info($"[InitializeHandler] World: {driver.World?.Id}, Services: {driver.World?.Services != null}");
            }
            catch (InvalidOperationException ex)
            {
                Log.Error($"[InitializeHandler] Configuration error: {ex.Message}");
                throw new InvalidOperationException($"Failed to initialize battle driver: {ex.Message}", ex);
            }
            catch (ArgumentException ex)
            {
                Log.Error($"[InitializeHandler] Invalid argument: {ex.Message}");
                throw new ArgumentException($"Failed to initialize battle driver: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Log.Error($"[InitializeHandler] Unexpected error: {ex.GetType().Name} - {ex.Message}");
                throw new InvalidOperationException($"Failed to initialize battle driver due to unexpected error", ex);
            }
        }

        private void InitializeConfigLoader(ETMobaBattleDriver driver)
        {
            try
            {
                driver.ConfigLoader = new ETConfigLoaderService(new ETTextAssetLoader(""));
                driver.ConfigLoader.LoadAll();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize config loader: {ex.Message}", ex);
            }
        }

        private void InitializeWorld(ETMobaBattleDriver driver, in BattleStartPlan plan)
        {
            // 使用 LogicWorldRegistry 获取对应的 Creator
            var worldType = BattleWorldTypes.Battle;
            var creator = LogicWorldRegistry.GetCreator(worldType);
            if (creator == null)
            {
                throw new InvalidOperationException($"No LogicWorldCreator registered for world type '{worldType}'");
            }

            // 通过 Creator 创建并初始化 World
            creator.CreateAndInitialize(
                driver,
                plan.WorldId,
                plan.MapId,
                plan.PlayerId,
                plan.TickRate > 0 ? plan.TickRate : 30);

            Log.Info($"[InitializeHandler] World initialized via LogicWorldRegistry: Type={worldType}");
        }
    }
}
