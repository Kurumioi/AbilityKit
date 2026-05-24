using AbilityKit.Ability.World.DI;

namespace ET.Logic
{
    /// <summary>
    /// ETWorldResolverComponent System
    /// 管理 moba.core World 解析器的生命周期
    ///
    /// 注意：业务逻辑在此 System 中实现
    /// </summary>
    [EntitySystemOf(typeof(ETWorldResolverComponent))]
    [FriendOf(typeof(ETWorldResolverComponent))]
    public static partial class ETWorldResolverComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ETWorldResolverComponent self)
        {
            Log.Info("[ETWorldResolver] ETWorldResolverComponent awake");
        }

        [EntitySystem]
        private static void Destroy(this ETWorldResolverComponent self)
        {
            // 释放 moba.core World 资源
            self.Initializer?.Dispose();
            self.Initializer = null;
            self.Resolver = null;
            Log.Info("[ETWorldResolver] ETWorldResolverComponent destroyed");
        }
    }
}
