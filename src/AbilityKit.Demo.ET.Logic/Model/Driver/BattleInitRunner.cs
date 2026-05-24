using System;
using System.Threading.Tasks;
using AbilityKit.Ability.Config;
using AbilityKit.Ability.Flow;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.Management;
using AbilityKit.Demo.Moba.Share;

namespace ET.Logic
{
    /// <summary>
    /// 战斗初始化运行器
    /// 封装 Flow 执行逻辑，提供同步和异步两种执行方式
    /// </summary>
    public sealed class BattleInitRunner
    {
        private readonly IFlowRootProvider<BattleInitArgs> _rootProvider;

        public BattleInitRunner(IFlowRootProvider<BattleInitArgs> rootProvider)
        {
            _rootProvider = rootProvider ?? throw new ArgumentNullException(nameof(rootProvider));
        }

        /// <summary>
        /// 同步执行初始化流程
        /// </summary>
        public BattleInitContext Execute(BattleInitArgs args)
        {
            var context = new BattleInitContext();
            var flowCtx = new FlowContext();
            flowCtx.Set(args);
            flowCtx.Set(context);

            var root = _rootProvider.CreateRoot(args);
            root.Enter(flowCtx);

            FlowStatus status;
            do
            {
                status = root.Tick(flowCtx, 0f);
            } while (status == FlowStatus.Running);

            root.Exit(flowCtx);

            if (status == FlowStatus.Failed)
            {
                throw new InvalidOperationException("Battle initialization failed");
            }

            return context;
        }

        /// <summary>
        /// 异步执行初始化流程
        /// </summary>
        public async Task<BattleInitContext> ExecuteAsync(BattleInitArgs args)
        {
            return await Task.Run(() => Execute(args));
        }

        /// <summary>
        /// 创建默认的初始化运行器
        /// </summary>
        public static BattleInitRunner CreateDefault()
        {
            return new BattleInitRunner(new BattleInitFlowRootProvider());
        }
    }
}
