using System;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// ETMobaBattleDriver System
    /// 负责调用 Driver 的生命周期方法
    ///
    /// 注意：ETBattleComponentSystem.InitializeBattle() 中也会调用 battleDriver.Awake()
    /// 这个 System 主要用于确保 Handler 注册时的日志输出
    /// </summary>
    [EntitySystemOf(typeof(ETMobaBattleDriver))]
    [FriendOf(typeof(ETMobaBattleDriver))]
    public static partial class ETMobaBattleDriverSystem
    {
        [EntitySystem]
        private static void Awake(this ETMobaBattleDriver self)
        {
            // 注册所有处理器
            HandlerRegistry.RegisterAll(self);
            Log.Info($"[ETMobaBattleDriverSystem] Awake: InputHandlers={self.InputHandlers?.Count ?? 0}, SnapshotHandlers={self.SnapshotHandlers?.Count ?? 0}, LifecycleHandlers={self.LifecycleHandlers?.Count ?? 0}");
        }

        [EntitySystem]
        private static void Update(this ETMobaBattleDriver self)
        {
            // Update logic handled by TickHandler through driver.Tick()
        }

        [EntitySystem]
        private static void Destroy(this ETMobaBattleDriver self)
        {
            self.Destroy();
        }
    }
}
