using System;
using AbilityKit.Ability.Host.Framework;

namespace ET.Logic
{
    /// <summary>
    /// 销毁处理器
    /// </summary>
    [LifecycleHandler(LifecyclePhase.Destroy)]
    public sealed class DestroyHandler : IDestroyHandler
    {
        public LifecyclePhase Phase => LifecyclePhase.Destroy;

        public void Handle(ETMobaBattleDriver driver)
        {
            // 清理资源
            driver.ConfigLoader = null;
            driver.SnapshotDispatcher = null;
            driver.Units.Clear();
            driver.World = null;
            driver.HostRuntime = null;
            driver.WorldManager = null;
            driver.ViewSink = null;

            driver.RuntimeGameStarted = false;
            driver.IsRunning = false;
            driver.CurrentFrame = 0;

            Log.Info("[DestroyHandler] Done");
        }
    }
}
