using AbilityKit.Ability.Host.Framework;
using AbilityKit.Demo.Moba.Share;

namespace ET.Logic
{
    /// <summary>
    /// Dispatches lifecycle calls to registered lifecycle handlers by capability interface.
    /// </summary>
    public static class ETBattleLifecycleDispatcher
    {
        public static void Initialize(ETMobaBattleDriver driver, in BattleStartPlan plan, IBattleViewEventSink viewSink)
        {
            foreach (var handler in driver.LifecycleHandlers)
            {
                if (handler is IInitializeHandler initializeHandler)
                {
                    initializeHandler.Handle(driver, in plan, viewSink);
                }
            }
        }

        public static void Start(ETMobaBattleDriver driver)
        {
            if (driver.LifecycleHandlers == null || driver.LifecycleHandlers.Count == 0)
            {
                Log.Warning("[ETMobaBattleDriver] No LifecycleHandlers registered!");
                return;
            }

            foreach (var handler in driver.LifecycleHandlers)
            {
                if (handler is IStartHandler startHandler)
                {
                    startHandler.Handle(driver);
                }
            }
        }

        public static void Stop(ETMobaBattleDriver driver)
        {
            foreach (var handler in driver.LifecycleHandlers)
            {
                if (handler is IStopHandler stopHandler)
                {
                    stopHandler.Handle(driver);
                }
            }
        }

        public static void Destroy(ETMobaBattleDriver driver)
        {
            foreach (var handler in driver.LifecycleHandlers)
            {
                if (handler is IDestroyHandler destroyHandler)
                {
                    destroyHandler.Handle(driver);
                }
            }
        }

        public static void Tick(ETMobaBattleDriver driver, float deltaTime)
        {
            foreach (var handler in driver.LifecycleHandlers)
            {
                if (handler is ITickHandler tickHandler)
                {
                    tickHandler.Handle(driver, deltaTime);
                }
            }
        }
    }
}
