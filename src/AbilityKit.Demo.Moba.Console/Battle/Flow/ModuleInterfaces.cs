namespace AbilityKit.Demo.Moba.Console.Battle.Flow
{
    /// <summary>
    /// 模块上下文接口。
    /// </summary>
    public interface IModuleContext
    {
    }

    /// <summary>
    /// 模块接口。
    /// </summary>
    public interface IGameModule<TContext> where TContext : class
    {
        void OnAttach(TContext context);
        void OnDetach(TContext context);
    }

    /// <summary>
    /// Tick 模块接口。
    /// </summary>
    public interface IGameModuleTick<TContext> : IGameModule<TContext> where TContext : class
    {
        void Tick(TContext context, float deltaTime);
    }

    /// <summary>
    /// 重绑定模块接口。
    /// </summary>
    public interface IGameModuleRebind<TContext> : IGameModule<TContext> where TContext : class
    {
        void Rebind(TContext context);
    }

    /// <summary>
    /// 模块 ID 接口。
    /// </summary>
    public interface IModuleId
    {
        string Id { get; }
    }

    /// <summary>
    /// 模块依赖接口。
    /// </summary>
    public interface IModuleDependencies
    {
        string[]? Dependencies { get; }
    }
}
