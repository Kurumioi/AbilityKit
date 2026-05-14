using System;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.DI;
using WorldId = AbilityKit.Ability.World.Abstractions.WorldId;

namespace AbilityKit.Orleans.Grains.Battle;

/// <summary>
/// 服务器端简单的 World 实现
/// </summary>
public sealed class SimpleWorld : IWorld
{
    private readonly WorldId _id;
    private readonly string _worldType;
    private readonly IWorldResolver _services;
    private bool _disposed;

    public SimpleWorld(WorldId id, string worldType, IWorldResolver services)
    {
        _id = id;
        _worldType = worldType;
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public WorldId Id => _id;
    public string WorldType => _worldType;
    public IWorldResolver Services => _services;

    public void Initialize()
    {
    }

    public void Tick(float deltaTime)
    {
        // MobaWorldBootstrapModule 会注册需要在 Tick 中调用的系统
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_services is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}

/// <summary>
/// 简单的 World 工厂
/// </summary>
public sealed class SimpleWorldFactory
{
    private readonly IWorldResolver _services;

    public SimpleWorldFactory(IWorldResolver services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public IWorld Create(WorldCreateOptions options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        return new SimpleWorld(options.Id, options.WorldType, _services);
    }
}
