using System;
using System.Collections.Generic;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Management;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Ability.World;
using AbilityKit.Demo.Moba.Share;

namespace ET.Logic
{
    /// <summary>
    /// ET Demo Battle World 类型标识
    /// </summary>
    public static class BattleWorldTypes
    {
        public const string Battle = "battle";
    }

    /// <summary>
    /// Battle World 选项
    /// </summary>
    public sealed class BattleWorldOptions
    {
        public int MapId { get; set; }
        public int LocalPlayerId { get; set; }
    }

    /// <summary>
    /// Battle World 工厂类
    /// 使用 EntitasWorld 来支持 moba.core ECS 世界
    /// </summary>
    public sealed class BattleWorldFactory : IWorldFactory
    {
        public static readonly BattleWorldFactory Instance = new BattleWorldFactory();

        private BattleWorldFactory() { }

        public IWorld Create(WorldCreateOptions options)
        {
            var worldId = options.Id;
            var worldType = options.WorldType ?? BattleWorldTypes.Battle;

            Log.Info($"[BattleWorldFactory] Creating EntitasWorld: Id={worldId}, Type={worldType}");

            // 使用 EntitasWorld 来支持 moba.core ECS 世界
            var world = new EntitasWorld(options);

            Log.Info($"[BattleWorldFactory] EntitasWorld created: Id={worldId}, Type={worldType}");
            return world;
        }
    }
}
