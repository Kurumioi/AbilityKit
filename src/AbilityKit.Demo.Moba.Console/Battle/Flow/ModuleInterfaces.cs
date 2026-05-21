namespace AbilityKit.Demo.Moba.Console.Battle.Flow
{
    /// <summary>
    /// Module context interface
    /// </summary>
    public interface IModuleContext
    {
    }

    /// <summary>
    /// Module interface
    /// </summary>
    public interface IGameModule<TContext> where TContext : class
    {
        void OnAttach(TContext context);
        void OnDetach(TContext context);
    }

    /// <summary>
    /// Tick module interface
    /// </summary>
    public interface IGameModuleTick<TContext> : IGameModule<TContext> where TContext : class
    {
        void Tick(TContext context, float deltaTime);
    }

    /// <summary>
    /// Rebind module interface
    /// </summary>
    public interface IGameModuleRebind<TContext> : IGameModule<TContext> where TContext : class
    {
        void Rebind(TContext context);
    }

    /// <summary>
    /// Module Id interface
    /// </summary>
    public interface IModuleId
    {
        string Id { get; }
    }

    /// <summary>
    /// Module dependencies interface
    /// </summary>
    public interface IModuleDependencies
    {
        string[]? Dependencies { get; }
    }
}
