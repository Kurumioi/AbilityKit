using System;
using AbilityKit.Demo.Moba.Console.Battle.Context;
using AbilityKit.Demo.Moba.Console.Battle.Flow;

namespace AbilityKit.Demo.Moba.Console.Battle.Features
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
    /// SubFeature 基类（Console 版本）
    /// 提供默认实现，简化 SubFeature 创建。
    /// Console 入口已不再挂接旧 GamePhase 框架，因此这里只保留 FeatureHost/IGameModule 适配。
    /// </summary>
    public abstract class ConsoleSubFeatureBase : SubFeatureBase, IConsoleSubFeature
    {
        protected ConsoleBattleContext? Context { get; private set; }

        /// <summary>
        /// SubFeature ID（使用基类的抽象属性）
        /// </summary>
        public override sealed string Id => GetSubFeatureId();

        /// <summary>
        /// SubFeature 依赖（使用基类的虚属性）
        /// </summary>
        public override sealed string[] Dependencies => GetSubFeatureDependencies();

        /// <summary>
        /// 获取 SubFeature ID（子类实现）
        /// </summary>
        protected abstract string GetSubFeatureId();

        /// <summary>
        /// 获取 SubFeature 依赖（子类可覆盖）
        /// </summary>
        protected virtual string[] GetSubFeatureDependencies() => Array.Empty<string>();

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

        protected override void OnAttachInternal(IFeatureContext ctx)
        {
            if (ctx is FeatureContextAdapter adapter && adapter.Context != null)
            {
                OnAttach(adapter.Context);
            }
        }

        protected override void OnDetachInternal(IFeatureContext ctx)
        {
            if (Context != null)
            {
                OnDetach(Context);
            }
        }

        public override void Tick(IFeatureContext ctx, float deltaTime)
        {
            if (Context != null)
            {
                Tick(Context, deltaTime);
            }
            base.Tick(ctx, deltaTime);
        }

    }
}
