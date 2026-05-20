using System;
using AbilityKit.Demo.Moba.Console.Core.Battle.Context;

namespace AbilityKit.Demo.Moba.Console.Core.Battle.Features
{
    /// <summary>
    /// Console SubFeature 接口
    /// 定义表现层模块的生命周期和依赖管理
    /// </summary>
    public interface IConsoleSubFeature
    {
        /// <summary>
        /// SubFeature 唯一标识
        /// </summary>
        string Id { get; }

        /// <summary>
        /// 依赖的其他 SubFeature ID 列表
        /// 在 OnAttach 之前，所有依赖的 SubFeature 应该已经初始化
        /// </summary>
        string[] Dependencies { get; }

        /// <summary>
        /// 附加到 Context
        /// </summary>
        void OnAttach(ConsoleBattleContext ctx);

        /// <summary>
        /// 每帧 Tick
        /// </summary>
        void Tick(ConsoleBattleContext ctx, float deltaTime);

        /// <summary>
        /// 从 Context 分离
        /// </summary>
        void OnDetach(ConsoleBattleContext ctx);
    }

    /// <summary>
    /// SubFeature 基类
    /// 提供默认实现，简化 SubFeature 创建
    /// </summary>
    public abstract class ConsoleSubFeatureBase : IConsoleSubFeature
    {
        public abstract string Id { get; }
        public virtual string[] Dependencies => Array.Empty<string>();

        protected ConsoleBattleContext? Context { get; private set; }

        public virtual void OnAttach(ConsoleBattleContext ctx)
        {
            Context = ctx;
        }

        public virtual void Tick(ConsoleBattleContext ctx, float deltaTime)
        {
        }

        public virtual void OnDetach(ConsoleBattleContext ctx)
        {
            Context = null;
        }
    }
}
