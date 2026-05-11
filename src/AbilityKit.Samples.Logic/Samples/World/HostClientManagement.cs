using System;
using System.Collections.Generic;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Ability.Host.Transport;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Management;
using AbilityKit.Ability.World.Services;
using AbilityKit.Samples.Abstractions;

namespace AbilityKit.Samples.Logic.Samples.World
{
    /// <summary>
    /// HostClientManagement - 演示 Host 客户端管理和消息广播
    /// 包括客户端连接、断开、消息发送等
    /// </summary>
    [Sample]
    public sealed class HostClientManagement : SampleBase
    {
        public override string Title => "Host Client Management";
        public override string Description => "Host 客户端连接管理与消息广播机制";
        public override SampleCategory Category => SampleCategory.World;

        private HostRuntime _host;
        private WorldTypeRegistry _registry;

        protected override void OnRun()
        {
            Log("=== Host Client Management 示例 ===");
            Output.Divider();

            // 1. 初始化 WorldTypeRegistry 和 HostRuntime
            Log("【1】初始化 WorldTypeRegistry 和 HostRuntime");
            _registry = new WorldTypeRegistry();
            _registry.Register("TestWorld", options => CreateWorldInstance(options));

            var factory = new RegistryWorldFactory(_registry);
            var worldManager = new WorldManager(factory);
            _host = new HostRuntime(worldManager, CreateOptions());
            Log("HostRuntime 创建完成");

            Output.Line();

            // 2. 创建测试 World
            Log("【2】创建测试 World");
            var world = _host.CreateWorld(new WorldCreateOptions
            {
                Id = new WorldId("test-world"),
                WorldType = "TestWorld"
            });
            Log($"World 创建成功: {world.Id.Value}");
            Log("  (创建 World 时会广播 WorldCreatedMessage)");

            Output.Line();

            // 3. 客户端连接
            Log("【3】客户端连接");
            Log("使用 host.Connect(connection) 添加客户端:");

            var client1 = new DemoServerConnection(new ServerClientId("player-1"));
            var client2 = new DemoServerConnection(new ServerClientId("player-2"));
            var client3 = new DemoServerConnection(new ServerClientId("player-3"));

            _host.Connect(client1);
            _host.Connect(client2);
            _host.Connect(client3);

            Log($"  已连接 {3} 个客户端:");
            Log($"    - Client 1: {client1.ClientId}");
            Log($"    - Client 2: {client2.ClientId}");
            Log($"    - Client 3: {client3.ClientId}");

            Output.Line();

            // 4. 广播消息
            Log("【4】广播消息");
            Log("使用 host.Broadcast(message) 向所有客户端发送消息:");

            var broadcastMsg = new TestBroadcastMessage("Hello all players!");
            _host.Broadcast(broadcastMsg);

            Log($"  已广播: {broadcastMsg.GetType().Name}");
            Log($"  各客户端收到消息数:");
            Log($"    - Client 1: {client1.ReceivedMessages.Count}");
            Log($"    - Client 2: {client2.ReceivedMessages.Count}");
            Log($"    - Client 3: {client3.ReceivedMessages.Count}");

            Output.Line();

            // 5. 点对点消息
            Log("【5】点对点消息");
            Log("使用 host.SendTo(connection, message) 向特定客户端发送:");

            var privateMsg = new TestPrivateMessage("Secret message for player-2");
            _host.SendTo(client2, privateMsg);

            Log($"  向 {client2.ClientId} 发送: {privateMsg.Content}");
            Log($"  各客户端收到消息数:");
            Log($"    - Client 1: {client1.ReceivedMessages.Count} (无变化)");
            Log($"    - Client 2: {client2.ReceivedMessages.Count} (+1)");
            Log($"    - Client 3: {client3.ReceivedMessages.Count} (无变化)");

            Output.Line();

            // 6. Hook 回调演示
            Log("【6】Hook 回调");
            Log("配置 HostRuntimeOptions 可监听消息发送:");

            Log($"  OnBeforeSendMessage 触发次数: {_beforeSendCount}");
            Log($"  OnAfterSendMessage 触发次数: {_afterSendCount}");

            Output.Line();

            // 7. 断开连接
            Log("【7】断开客户端连接");
            Log("使用 host.Disconnect(clientId) 移除客户端:");

            _host.Disconnect(client2.ClientId);
            Log($"  已断开 Client 2: {client2.ClientId}");

            _host.Broadcast(new TestBroadcastMessage("Player2 left"));
            Log("  广播 'Player2 left'");

            Log($"  各客户端收到消息数:");
            Log($"    - Client 1: {client1.ReceivedMessages.Count}");
            Log($"    - Client 2: {client2.ReceivedMessages.Count} (不再收到)");
            Log($"    - Client 3: {client3.ReceivedMessages.Count}");

            Output.Line();

            // 8. Tick 与生命周期
            Log("【8】Tick 与生命周期钩子");
            Log("HostRuntime.Tick 会驱动所有 World 的 Tick:");

            _host.Tick(0.016f);
            Log("  Tick(0.016f) 执行完成");

            Output.Line();

            // 9. 销毁 World
            Log("【9】销毁 World");
            Log("使用 host.DestroyWorld(worldId) 销毁 World:");

            _host.DestroyWorld(new WorldId("test-world"));
            Log("  World 已销毁");
            Log("  (销毁时会广播 WorldDestroyedMessage)");

            Output.Divider();
        }

        private int _beforeSendCount;
        private int _afterSendCount;

        private HostRuntimeOptions CreateOptions()
        {
            var options = new HostRuntimeOptions();

            options.OnBeforeSendMessage = (clientId, msg) =>
            {
                _beforeSendCount++;
            };

            options.OnAfterSendMessage = (clientId, msg) =>
            {
                _afterSendCount++;
            };

            options.OnWorldCreated = world =>
            {
                Log($"    [Hook] WorldCreated: {world.Id.Value}");
            };

            options.OnWorldDestroyed = id =>
            {
                Log($"    [Hook] WorldDestroyed: {id.Value}");
            };

            return options;
        }

        private IWorld CreateWorldInstance(WorldCreateOptions options)
        {
            var builder = new WorldContainerBuilder();
            builder.RegisterServiceType<IWorldClock, WorldClock>(WorldLifetime.Singleton);
            builder.RegisterServiceType<IWorldLogger, NullWorldLogger>(WorldLifetime.Singleton);
            var container = builder.Build();
            return new SimpleWorldInstance(options.Id, options.WorldType, container);
        }
    }

    #region Demo Messages

    public sealed class TestBroadcastMessage : ServerMessage
    {
        public readonly string Content;

        public TestBroadcastMessage(string content)
        {
            Content = content;
        }

        public override string ToString() => $"Broadcast: {Content}";
    }

    public sealed class TestPrivateMessage : ServerMessage
    {
        public readonly string Content;

        public TestPrivateMessage(string content)
        {
            Content = content;
        }

        public override string ToString() => $"Private: {Content}";
    }

    #endregion

    #region Demo Server Connection

    public sealed class DemoServerConnection : IServerConnection
    {
        private readonly List<ServerMessage> _receivedMessages = new List<ServerMessage>();

        public DemoServerConnection(ServerClientId clientId)
        {
            ClientId = clientId;
        }

        public ServerClientId ClientId { get; }

        public IReadOnlyList<ServerMessage> ReceivedMessages => _receivedMessages;

        public void Send(ServerMessage message)
        {
            _receivedMessages.Add(message);
        }
    }

    #endregion

    #region SimpleWorldInstance

    public sealed class SimpleWorldInstance : IWorld
    {
        private readonly WorldId _id;
        private readonly string _worldType;
        private readonly IWorldResolver _services;
        private bool _disposed;

        public SimpleWorldInstance(WorldId id, string worldType, IWorldResolver services)
        {
            _id = id;
            _worldType = worldType;
            _services = services;
        }

        public WorldId Id => _id;
        public string WorldType => _worldType;
        public IWorldResolver Services => _services;

        public void Initialize() { }

        public void Tick(float deltaTime)
        {
            if (_services.TryResolve<IWorldClock>(out var clock))
            {
                clock.Tick(deltaTime);
            }
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

    #endregion
}
