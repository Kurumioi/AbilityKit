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
            IMobaBattleInputPort inputPort = null;

            // 从 World.Services 获取战斗逻辑层输入端口，避免外部模块直接依赖内部 Sink。
            if (driver.World?.Services != null &&
                driver.World.Services.TryResolve<IMobaBattleInputPort>(out var port) &&
                port != null)
            {
                inputPort = port;
            }
            else
            {
                Log.Error("[StartHandler] IMobaBattleInputPort not registered in World.Services!");
                throw new InvalidOperationException("IMobaBattleInputPort must be registered");
            }

            driver.InputPort = inputPort;

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
