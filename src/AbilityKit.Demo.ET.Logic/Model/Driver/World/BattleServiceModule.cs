using System;
using AbilityKit.Ability.Config;
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
    /// 注意：战斗启动、输入和快照输出由运行时包注册为 IMobaBattleRuntimePort。
    /// ET 侧只通过统一运行时端口接入，不直接替换内部战斗服务。
    /// </summary>
    public sealed class BattleServiceModule : IWorldModule
    {
        public void Configure(WorldContainerBuilder builder)
        {
            // 不注册战斗输入/输出服务，ET Demo 通过 IMobaBattleRuntimePort 接入正式运行时。
            builder.TryRegister<ITextAssetLoader>(WorldLifetime.Singleton, _ => new ETTextAssetLoader());
            builder.TryRegister<ITextAssetDirectoryLoader>(WorldLifetime.Singleton, _ => new ETTextAssetLoader());

            Log.Info("[BattleServiceModule] Configured (battle IO ports are handled by moba runtime)");
        }
    }
}
