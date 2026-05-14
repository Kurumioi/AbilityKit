using System.Reflection;

namespace AbilityKit.Orleans.Gateway.TcpGateway.Handler;

/// <summary>
/// 基于 Attribute 的 Handler 注册表
/// </summary>
public sealed class HandlerRegistry
{
    private readonly Dictionary<uint, RequestHandlerBase> _handlers = new();
    private readonly object _lock = new();

    /// <summary>
    /// 从程序集注册所有带 GatewayHandler 特性的 Handler
    /// </summary>
    public void RegisterFromAssembly(Assembly assembly)
    {
        var handlerType = typeof(RequestHandlerBase);

        foreach (var type in assembly.GetTypes())
        {
            if (!type.IsClass || type.IsAbstract || !handlerType.IsAssignableFrom(type))
                continue;

            var attr = type.GetCustomAttribute<GatewayHandlerAttribute>();
            if (attr == null)
                continue;

            var handler = (RequestHandlerBase)Activator.CreateInstance(type)!;
            Register(attr.OpCode, handler);
        }
    }

    /// <summary>
    /// 注册一个 Handler
    /// </summary>
    public void Register(uint opCode, RequestHandlerBase handler)
    {
        lock (_lock)
        {
            if (_handlers.ContainsKey(opCode))
            {
                throw new InvalidOperationException($"Handler for OpCode {opCode} is already registered.");
            }
            _handlers[opCode] = handler;
        }
    }

    /// <summary>
    /// 获取指定 OpCode 的 Handler
    /// </summary>
    public RequestHandlerBase? GetHandler(uint opCode)
    {
        lock (_lock)
        {
            return _handlers.TryGetValue(opCode, out var handler) ? handler : null;
        }
    }

    /// <summary>
    /// 获取所有已注册的 Handler
    /// </summary>
    public IReadOnlyDictionary<uint, RequestHandlerBase> GetAllHandlers()
    {
        lock (_lock)
        {
            return new Dictionary<uint, RequestHandlerBase>(_handlers);
        }
    }
}
