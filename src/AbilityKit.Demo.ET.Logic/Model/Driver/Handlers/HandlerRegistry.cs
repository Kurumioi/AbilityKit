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
        public static void RegisterSnapshotHandlers(ETMobaBattleDriver driver)
        {
            if (driver == null)
            {
                throw new ArgumentNullException(nameof(driver));
            }

            driver.SnapshotHandlers.Clear();

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
    }
}
