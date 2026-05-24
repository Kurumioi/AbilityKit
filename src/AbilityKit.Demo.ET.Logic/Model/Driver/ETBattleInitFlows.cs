using System;
using AbilityKit.Ability.Config;
using AbilityKit.Ability.Flow;
using AbilityKit.Ability.Flow.Nodes;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Ability.World;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.Management;
using AbilityKit.Ability.World.Services;
using AbilityKit.Demo.Moba.Share;
using AbilityKit.Demo.Moba.Worlds.Blueprints;
using EC = AbilityKit.World.ECS;

namespace ET.Logic
{
    /// <summary>
    /// 战斗初始化参数
    /// </summary>
    public sealed class BattleInitArgs
    {
        public BattleStartPlan Plan { get; set; }
        public IBattleViewEventSink ViewSink { get; set; }
        public ITextAssetLoader TextAssetLoader { get; set; }
    }

    /// <summary>
    /// 战斗初始化上下文
    /// </summary>
    public sealed class BattleInitContext
    {
        public IWorldManager WorldManager { get; set; }
        public HostRuntime HostRuntime { get; set; }
        public IWorld World { get; set; }
        public IBattleViewEventSink ViewSink { get; set; }
        public ETConfigLoaderService ConfigLoader { get; set; }
    }

    #region Flow Nodes

    /// <summary>
    /// 配置日志节点
    /// </summary>
    public sealed class ConfigureLogSinkNode : IFlowNode
    {
        public void Enter(FlowContext ctx) { }

        public FlowStatus Tick(FlowContext ctx, float dt)
        {
            Log.Info("[Flow] Configuring AbilityKit log sink...");
            return FlowStatus.Succeeded;
        }

        public void Exit(FlowContext ctx) { }

        public void Interrupt(FlowContext ctx) { }
    }

    /// <summary>
    /// 创建 World 管理器节点
    /// </summary>
    public sealed class CreateWorldManagerNode : IFlowNode
    {
        public void Enter(FlowContext ctx) { }

        public FlowStatus Tick(FlowContext ctx, float dt)
        {
            if (ctx.TryGet<BattleInitContext>(out var initCtx) && initCtx?.WorldManager != null)
            {
                return FlowStatus.Succeeded;
            }

            Log.Info("[Flow] Creating WorldManager...");

            var registry = new WorldTypeRegistry();
            MobaWorldBlueprintsRegistration.RegisterAll(registry, options => new EntitasWorld(options));

            var worldManager = new WorldManager(new RegistryWorldFactory(registry));

            if (!ctx.TryGet<BattleInitContext>(out initCtx))
            {
                initCtx = new BattleInitContext();
                ctx.Set(initCtx);
            }
            initCtx.WorldManager = worldManager;

            Log.Info("[Flow] WorldManager created");
            return FlowStatus.Succeeded;
        }

        public void Exit(FlowContext ctx) { }

        public void Interrupt(FlowContext ctx) { }
    }

    /// <summary>
    /// 创建 HostRuntime 节点
    /// </summary>
    public sealed class CreateHostRuntimeNode : IFlowNode
    {
        public void Enter(FlowContext ctx) { }

        public FlowStatus Tick(FlowContext ctx, float dt)
        {
            if (ctx.TryGet<BattleInitContext>(out var initCtx) && initCtx?.HostRuntime != null)
            {
                return FlowStatus.Succeeded;
            }

            Log.Info("[Flow] Creating HostRuntime...");

            if (!ctx.TryGet<BattleInitContext>(out initCtx) || initCtx?.WorldManager == null)
            {
                return FlowStatus.Running;
            }

            var hostOptions = new HostRuntimeOptions();
            var hostRuntime = new HostRuntime(initCtx.WorldManager, hostOptions);

            if (!ctx.TryGet<BattleInitContext>(out initCtx))
            {
                initCtx = new BattleInitContext();
                ctx.Set(initCtx);
            }
            initCtx.HostRuntime = hostRuntime;

            Log.Info("[Flow] HostRuntime created");
            return FlowStatus.Succeeded;
        }

        public void Exit(FlowContext ctx) { }

        public void Interrupt(FlowContext ctx) { }
    }

    /// <summary>
    /// 创建 World 节点
    /// </summary>
    public sealed class CreateWorldNode : IFlowNode
    {
        private bool _worldCreated;

        public void Enter(FlowContext ctx)
        {
            _worldCreated = false;
        }

        public FlowStatus Tick(FlowContext ctx, float dt)
        {
            if (_worldCreated)
            {
                return FlowStatus.Succeeded;
            }

            if (!ctx.TryGet<BattleInitContext>(out var initCtx))
            {
                return FlowStatus.Running;
            }

            if (initCtx?.WorldManager == null || initCtx?.HostRuntime == null)
            {
                return FlowStatus.Running;
            }

            Log.Info("[Flow] Creating World...");

            var worldId = new WorldId("MobaBattle");
            var worldOptions = new WorldCreateOptions(worldId, MobaBattleWorldBlueprint.Type);
            var world = initCtx.HostRuntime.CreateWorld(worldOptions);

            initCtx.World = world;
            _worldCreated = true;

            Log.Info("[Flow] World created");
            return FlowStatus.Succeeded;
        }

        public void Exit(FlowContext ctx) { }

        public void Interrupt(FlowContext ctx) { }
    }

    /// <summary>
    /// 创建配置加载器节点
    /// </summary>
    public sealed class CreateConfigLoaderNode : IFlowNode
    {
        public void Enter(FlowContext ctx) { }

        public FlowStatus Tick(FlowContext ctx, float dt)
        {
            if (ctx.TryGet<BattleInitContext>(out var initCtx) && initCtx?.ConfigLoader != null)
            {
                return FlowStatus.Succeeded;
            }

            var args = ctx.TryGet<BattleInitArgs>(out var a) ? a : null;
            var loader = args?.TextAssetLoader ?? new ETTextAssetLoader("");

            var configLoader = new ETConfigLoaderService(loader);
            configLoader.LoadAll();

            if (!ctx.TryGet<BattleInitContext>(out initCtx))
            {
                initCtx = new BattleInitContext();
                ctx.Set(initCtx);
            }
            initCtx.ConfigLoader = configLoader;

            Log.Info("[Flow] ConfigLoader created and loaded");
            return FlowStatus.Succeeded;
        }

        public void Exit(FlowContext ctx) { }

        public void Interrupt(FlowContext ctx) { }
    }

    /// <summary>
    /// 注册视图事件接收器节点
    /// </summary>
    public sealed class RegisterViewSinkNode : IFlowNode
    {
        public void Enter(FlowContext ctx) { }

        public FlowStatus Tick(FlowContext ctx, float dt)
        {
            if (!ctx.TryGet<BattleInitContext>(out var initCtx))
            {
                return FlowStatus.Running;
            }

            if (ctx.TryGet<BattleInitArgs>(out var args) && args?.ViewSink != null)
            {
                initCtx.ViewSink = args.ViewSink;
                Log.Info("[Flow] ViewSink registered");
            }

            return FlowStatus.Succeeded;
        }

        public void Exit(FlowContext ctx) { }

        public void Interrupt(FlowContext ctx) { }
    }

    #endregion

    #region Flow Root Provider

    /// <summary>
    /// 战斗初始化流程根节点提供者
    /// </summary>
    public sealed class BattleInitFlowRootProvider : IFlowRootProvider<BattleInitArgs>
    {
        public IFlowNode CreateRoot(BattleInitArgs args)
        {
            return new SequenceNode(
                new ConfigureLogSinkNode(),
                new CreateWorldManagerNode(),
                new CreateHostRuntimeNode(),
                new CreateConfigLoaderNode(),
                new RegisterViewSinkNode(),
                new CreateWorldNode()
            );
        }
    }

    #endregion
}
