using System;
using AbilityKit.Ability.Host.Framework;

namespace ET.Logic
{
    /// <summary>
    /// 停止处理器
    /// </summary>
    [LifecycleHandler(LifecyclePhase.Stop)]
    public sealed class StopHandler : IStopHandler
    {
        public LifecyclePhase Phase => LifecyclePhase.Stop;

        public void Handle(ETMobaBattleDriver driver)
        {
            driver.IsRunning = false;
            driver.RuntimeGameStarted = false;
            Log.Info("[StopHandler] Done");
        }
    }
}
