using System;
using AbilityKit.Ability.Share.ECS;
using AbilityKit.Ability.Share.ECS.Entitas;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World;
using AbilityKit.Core.Common.Log;
using AbilityKit.Demo.Moba.Systems.Bootstrap.Flow;
using AbilityKit.ECS;

namespace AbilityKit.Demo.Moba.Systems
{
    /// <summary>
    /// Moba World Bootstrap Module
    /// 委托给新的 Flow Bootstrap 系统
    /// </summary>
    public sealed partial class MobaWorldBootstrapModule : IWorldModule, IEntitasSystemsInstaller
    {
        public const int InitOpCode = 2000;

        private static readonly MobaBootstrapFlow _flowBootstrap;

        static MobaWorldBootstrapModule()
        {
            // 触发静态初始化，确保所有 Stage 被注册
            MobaBootstrapFlowModule.EnsureInitialized();
            _flowBootstrap = new MobaBootstrapFlow();
        }

        public void Configure(WorldContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            Log.Info("[MobaWorldBootstrapModule] Configure called - creating MobaBootstrapFlow");
            Log.Info($"[MobaWorldBootstrapModule] Stage count: {MobaBootstrapStageRegistry.Count}");

            foreach (var stage in MobaBootstrapStageRegistry.GetAllStages())
            {
                Log.Info($"[MobaWorldBootstrapModule] Found stage: {stage.Name}");
            }

            _flowBootstrap.Configure(builder);
            Log.Info("[MobaWorldBootstrapModule] Configure done");
        }

        public void Install(global::Entitas.IContexts contexts, global::Entitas.Systems systems, IWorldResolver services)
        {
            if (contexts == null) throw new ArgumentNullException(nameof(contexts));
            if (systems == null) throw new ArgumentNullException(nameof(systems));
            if (services == null) throw new ArgumentNullException(nameof(services));

            Log.Info("[MobaWorldBootstrapModule] Install: checking Entitas ECS services registration");

            // 检查 IContexts 是否是生成的 Contexts 类型
            if (contexts is global::Contexts generatedContexts)
            {
                Log.Info("[MobaWorldBootstrapModule] Install: detected generated Contexts type, services should be auto-registered");
            }
            else
            {
                Log.Warning("[MobaWorldBootstrapModule] Install: IContexts is not generated Contexts type, manual registration may be needed");
            }

            // 安装 Entitas ECS 系统
            AutoSystemInstaller.Install(
                contexts,
                systems,
                services,
                assemblies: new[] { typeof(MobaWorldBootstrapModule).Assembly, typeof(AbilityKit.Core.Common.Projectile.ProjectileTickSystem).Assembly },
                namespacePrefixes: new[]
                {
                    "AbilityKit.Demo.Moba",
                    "AbilityKit.Core.Common.Projectile",
                }
            );

            // 执行 Flow Bootstrap Install
            _flowBootstrap.Install(contexts, systems, services);
        }
    }
}

