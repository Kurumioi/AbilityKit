using System;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Demo.Moba.Services;

namespace ET.Logic
{
    /// <summary>
    /// 启动处理器
    /// </summary>
    [LifecycleHandler(LifecyclePhase.Start)]
    public sealed class StartHandler : IStartHandler
    {
        public LifecyclePhase Phase => LifecyclePhase.Start;

        public void Handle(ETMobaBattleDriver driver)
        {
            IMobaBattleRuntimePort runtime = null;
            if (driver.World?.Services == null ||
                !driver.World.Services.TryResolve(out runtime) ||
                runtime == null ||
                !runtime.Status.IsReadyForBattleLoop)
            {
                var status = runtime != null ? runtime.Status.ToString() : "runtime port missing";
                Log.Error($"[StartHandler] IMobaBattleRuntimePort is not ready: {status}");
                throw new InvalidOperationException("IMobaBattleRuntimePort must be ready for battle loop");
            }

            driver.IsRunning = true;
            driver.LastTickTime = GetCurrentTimeSeconds();
            driver.CurrentFrame = 0;
            driver.LogicTimeSeconds = 0;
        }

        private static double GetCurrentTimeSeconds()
        {
            return (double)Environment.TickCount64 / 1000.0;
        }
    }
}
