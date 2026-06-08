using System;
using AbilityKit.Ability.Config;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Demo.Moba.Config;
using AbilityKit.Demo.Moba.View.Config;

namespace AbilityKit.Game.Battle.Moba.Config
{
    /// <summary>
    /// 配置模块 - 复用运行时包的 MobaConfigDatabase 注册
    /// 视图包负责注册 View 层依赖，如 ITextAssetLoader
    /// </summary>
    [WorldService(typeof(ITextAssetLoader), WorldLifetime.Singleton)]
    public sealed class MobaConfigWorldModule : IWorldModule
    {
        public void Configure(WorldContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            // ResourcesTextAssetLoader 已经通过 WorldServiceAttribute 自动注册
            // 这里不需要显式注册
        }
    }
}
