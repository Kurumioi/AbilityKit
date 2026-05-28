using System;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.World.DI;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Protocol.Moba;

namespace ET.Logic
{
    /// <summary>
    /// Battle 服务模块
    /// 注册 Battle 世界所需的所有服务
    /// 
    /// 注意：战斗逻辑层输入由运行时包注册为 IMobaBattleInputPort。
    /// ET 侧只通过端口接入，不直接替换内部输入服务。
    /// </summary>
    public sealed class BattleServiceModule : IWorldModule
    {
        public void Configure(WorldContainerBuilder builder)
        {
            // 不注册输入服务，ET Demo 的输入处理逻辑通过 IMobaBattleInputPort 接入。

            Log.Info("[BattleServiceModule] Configured (battle IO ports are handled by moba runtime)");
        }
    }
}
