using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Demo.Moba.Share;

namespace ET.Logic
{
    /// <summary>
    /// Handler 注册器 - 基于反射自动发现和注册处理器
    /// </summary>
    public static class HandlerRegistry
    {
        private static readonly Assembly[] _assemblies;

        static HandlerRegistry()
        {
            _assemblies = new[] { typeof(HandlerRegistry).Assembly };
        }

        /// <summary>
        /// 注册所有处理器到 Driver
        /// </summary>
        public static void RegisterAll(ETMobaBattleDriver driver)
        {
            RegisterSnapshotHandlers(driver);
            RegisterLifecycleHandlers(driver);
        }

        private static void RegisterSnapshotHandlers(ETMobaBattleDriver driver)
        {
            driver.SnapshotHandlers.Clear();

            // 扫描所有 SnapshotHandler 实现
            var handlerType = typeof(ISnapshotHandler);
            var types = _assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => handlerType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var type in types)
            {
                try
                {
                    var handler = Activator.CreateInstance(type) as ISnapshotHandler;
                    if (handler != null)
                    {
                        driver.SnapshotHandlers.Add(handler);
                        Log.Debug($"[HandlerRegistry] Registered SnapshotHandler: {type.Name}");
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"[HandlerRegistry] Failed to create SnapshotHandler {type.Name}: {ex.Message}");
                }
            }

            Log.Info($"[HandlerRegistry] SnapshotHandlers registered: {driver.SnapshotHandlers.Count}");
        }

        private static void RegisterLifecycleHandlers(ETMobaBattleDriver driver)
        {
            driver.LifecycleHandlers.Clear();

            // 扫描所有 LifecycleHandler 实现
            var handlerType = typeof(ILifecycleHandler);
            var types = _assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => handlerType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var type in types)
            {
                try
                {
                    var handler = Activator.CreateInstance(type) as ILifecycleHandler;
                    if (handler != null)
                    {
                        driver.LifecycleHandlers.Add(handler);
                        Log.Debug($"[HandlerRegistry] Registered LifecycleHandler: {type.Name}");
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"[HandlerRegistry] Failed to create LifecycleHandler {type.Name}: {ex.Message}");
                }
            }

            Log.Info($"[HandlerRegistry] LifecycleHandlers registered: {driver.LifecycleHandlers.Count}");
        }
    }
}
