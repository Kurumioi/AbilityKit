using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Core.Logging;

namespace ET.Logic.Model.Driver
{
    /// <summary>
    /// ET 环境下专用的服务注册模块
    /// 手动注册所有 moba.core 需要的服务，以确保在 ET 环境下正确工作
    /// </summary>
    public sealed class ETMobaServicesModule : IWorldModule
    {
        public void Configure(WorldContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            
            Log.Info("[ETMobaServicesModule] Configuring ET-specific services...");
            
            // 注册 ActorEntityInitPipeline
            RegisterServiceByReflection(builder, "AbilityKit.Demo.Moba.Util.Generator.ActorEntityInitPipeline");
            
            // 注册 IMobaSkillPipelineLibrary
            RegisterServiceByReflection(builder, "AbilityKit.Demo.Moba.Services.Skill.Pipeline.TableDrivenMobaSkillPipelineLibrary");
            
            // 注册 TriggerRunner<IWorldResolver>
            RegisterTriggerRunnerGeneric(builder);
            
            Log.Info("[ETMobaServicesModule] Configuration complete");
        }
        
        private void RegisterTriggerRunnerGeneric(WorldContainerBuilder builder)
        {
            try
            {
                // 查找非泛型 TriggerRunner 的实现类型
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                Type triggerRunnerType = null;
                
                foreach (var asm in assemblies)
                {
                    if (asm == null || asm.IsDynamic || string.IsNullOrEmpty(asm.FullName)) continue;
                    try
                    {
                        // 查找 TriggerRunner<IWorldResolver>
                        var types = asm.GetTypes();
                        foreach (var t in types)
                        {
                            if (t == null) continue;
                            if (t.IsGenericTypeDefinition) continue;
                            if (t.IsInterface || t.IsAbstract) continue;
                            
                            var args = t.GetGenericArguments();
                            if (args.Length == 1 && 
                                t.Name.Contains("TriggerRunner") &&
                                args[0].Name == "IWorldResolver")
                            {
                                triggerRunnerType = t;
                                Log.Info($"[ETMobaServicesModule] Found TriggerRunner<IWorldResolver>: {t.FullName}");
                                break;
                            }
                        }
                        if (triggerRunnerType != null) break;
                    }
                    catch
                    {
                        // 忽略类型加载错误
                    }
                }
                
                if (triggerRunnerType != null)
                {
                    // 构建泛型类型 TriggerRunner<IWorldResolver>
                    var iwrType = typeof(IWorldResolver);
                    var genericType = triggerRunnerType.MakeGenericType(iwrType);
                    
                    // 使用 factory 方法注册
                    builder.TryRegister(
                        genericType,
                        WorldLifetime.Scoped,
                        services =>
                        {
                            // 获取非泛型 TriggerRunner
                            var nonGenericRunner = services.Resolve<global::AbilityKit.Ability.Triggering.Runtime.TriggerRunner>();
                            // 创建泛型版本
                            return Activator.CreateInstance(genericType, nonGenericRunner);
                        });
                    
                    Log.Info($"[ETMobaServicesModule] Registered TriggerRunner<IWorldResolver>");
                }
                else
                {
                    Log.Warning("[ETMobaServicesModule] TriggerRunner<IWorldResolver> not found");
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[ETMobaServicesModule] Failed to register TriggerRunner<IWorldResolver>: {ex.Message}");
            }
        }
        
        private void RegisterServiceByReflection(WorldContainerBuilder builder, string typeName)
        {
            try
            {
                // 尝试在所有已加载的程序集中查找类型
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                Type foundType = null;
                string foundAssembly = null;
                
                foreach (var asm in assemblies)
                {
                    if (asm == null || string.IsNullOrEmpty(asm.FullName)) continue;
                    
                    // 跳过 dynamic 和反射-only 程序集
                    if (asm.IsDynamic) continue;
                    
                    try
                    {
                        var type = asm.GetType(typeName);
                        if (type != null && !type.IsInterface && !type.IsAbstract)
                        {
                            foundType = type;
                            foundAssembly = asm.FullName;
                            break;
                        }
                    }
                    catch
                    {
                        // 忽略类型加载错误
                    }
                }
                
                if (foundType != null)
                {
                    // 获取 [WorldService] 属性
                    var attrs = foundType.GetCustomAttributes(typeof(WorldServiceAttribute), false);
                    if (attrs != null && attrs.Length > 0)
                    {
                        var attr = (WorldServiceAttribute)attrs[0];
                        if (attr.ServiceType != null && attr.ServiceType.IsAssignableFrom(foundType))
                        {
                            builder.TryRegisterType(attr.ServiceType, foundType, attr.Lifetime);
                            Log.Info($"[ETMobaServicesModule] Registered {attr.ServiceType.Name} -> {foundType.Name} from {foundAssembly}");
                            return;
                        }
                    }
                    
                    // 如果没有属性，尝试作为 IService 注册
                    if (typeof(IService).IsAssignableFrom(foundType))
                    {
                        builder.TryRegisterType(foundType, foundType, WorldLifetime.Scoped);
                        Log.Info($"[ETMobaServicesModule] Registered {foundType.Name} (as IService) from {foundAssembly}");
                        return;
                    }
                    
                    Log.Warning($"[ETMobaServicesModule] Found {typeName} but no registration method available");
                }
                else
                {
                    Log.Warning($"[ETMobaServicesModule] Type not found: {typeName}");
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[ETMobaServicesModule] Failed to register {typeName}: {ex.Message}");
            }
        }
    }
}
