using System.Reflection;
using AbilityKit.Orleans.Gateway.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace AbilityKit.Orleans.Gateway.Core;

/// <summary>
/// Gateway Handler 注册表
/// </summary>
public sealed class GatewayHandlerRegistry : IGatewayHandlerRegistry
{
    private readonly Dictionary<uint, IGatewayRequestHandler> _handlers = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly object _lock = new();

    public GatewayHandlerRegistry(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 从程序集注册所有带 GatewayHandler 特性的 Handler（使用 DI）
    /// </summary>
    public void RegisterFromAssembly(Assembly assembly)
    {
        var handlerType = typeof(IGatewayRequestHandler);
        var attrType = typeof(GatewayHandlerAttribute);

        foreach (var type in assembly.GetTypes())
        {
            if (!type.IsClass || type.IsAbstract || !handlerType.IsAssignableFrom(type))
                continue;

            var attr = type.GetCustomAttribute(attrType) as GatewayHandlerAttribute;
            if (attr == null)
                continue;

            // 使用 DI 容器创建实例
            var handler = _serviceProvider.GetRequiredService(type) as IGatewayRequestHandler;
            if (handler != null)
            {
                Register(attr.OpCode, handler);
            }
        }
    }

    public void Register(uint opCode, IGatewayRequestHandler handler)
    {
        lock (_lock)
        {
            if (_handlers.ContainsKey(opCode))
                throw new InvalidOperationException($"Handler for OpCode {opCode} already registered.");
            _handlers[opCode] = handler;
        }
    }

    public IGatewayRequestHandler? GetHandler(uint opCode)
    {
        lock (_lock)
        {
            return _handlers.TryGetValue(opCode, out var handler) ? handler : null;
        }
    }

    public IReadOnlyDictionary<uint, IGatewayRequestHandler> GetAllHandlers()
    {
        lock (_lock)
        {
            return new Dictionary<uint, IGatewayRequestHandler>(_handlers);
        }
    }
}

/// <summary>
/// Handler 注册属性
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class GatewayHandlerAttribute : Attribute
{
    public uint OpCode { get; }
    public GatewayHandlerAttribute(uint opCode) => OpCode = opCode;
}
