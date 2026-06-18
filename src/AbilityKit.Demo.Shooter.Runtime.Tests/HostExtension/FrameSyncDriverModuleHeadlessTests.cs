using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Host.Extensions.FrameSync;
using AbilityKit.Ability.Host.Framework;
using AbilityKit.Ability.Host.Transport;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Management;
using Xunit;

namespace AbilityKit.Demo.Shooter.Runtime.Tests.HostExtension;

public sealed class FrameSyncDriverModuleHeadlessTests
{
    [Fact]
    public void HeadlessSession_WhenTicked_FlushesInputsAndBroadcastsFramePacketWithoutWorld()
    {
        var worlds = new EmptyWorldManager();
        var options = new HostRuntimeOptions();
        var runtime = new HostRuntime(worlds, options);
        var module = new FrameSyncDriverModule();
        var connection = new RecordingConnection(new ServerClientId("client-1"));
        var worldId = new WorldId("headless-world");
        var input = new PlayerInputCommand(new FrameIndex(0), new PlayerId("player-1"), 101, new byte[] { 1, 2, 3 });

        module.Install(runtime, options);
        runtime.Connect(connection);

        Assert.True(runtime.Features.TryGetFeature<IFrameSyncInputHub>(out var inputHub));
        Assert.True(runtime.Features.TryGetFeature<IFrameSyncDriverEvents>(out var events));

        WorldId flushedWorldId = default;
        FrameIndex flushedFrame = default;
        PlayerInputCommand[] flushedInputs = null!;
        events.AddInputsFlushed((id, frame, inputs) =>
        {
            flushedWorldId = id;
            flushedFrame = frame;
            flushedInputs = inputs;
        });

        module.RegisterSession(worldId);

        Assert.True(inputHub.SubmitInput(connection.ClientId, worldId, input));

        runtime.Tick(1f / 30f);

        Assert.Equal(worldId, flushedWorldId);
        Assert.Equal(1, flushedFrame.Value);
        Assert.NotNull(flushedInputs);
        var flushedInput = Assert.Single(flushedInputs);
        Assert.Equal(101, flushedInput.OpCode);
        Assert.Equal("player-1", flushedInput.Player.Value);

        var frameMessage = Assert.IsType<FrameMessage>(Assert.Single(connection.Messages));
        Assert.Equal(worldId, frameMessage.Packet.WorldId);
        Assert.Equal(1, frameMessage.Packet.Frame.Value);
        Assert.Null(frameMessage.Packet.Snapshot);
        var packetInput = Assert.Single(frameMessage.Packet.Inputs);
        Assert.Equal(101, packetInput.OpCode);
        Assert.Equal("player-1", packetInput.Player.Value);
    }

    [Fact]
    public void HeadlessSession_WhenUnregistered_RejectsLaterInput()
    {
        var options = new HostRuntimeOptions();
        var runtime = new HostRuntime(new EmptyWorldManager(), options);
        var module = new FrameSyncDriverModule();
        var worldId = new WorldId("headless-world");
        var input = new PlayerInputCommand(new FrameIndex(0), new PlayerId("player-1"), 102, new byte[] { 4 });

        module.Install(runtime, options);
        module.RegisterSession(worldId);
        module.UnregisterSession(worldId);

        Assert.True(runtime.Features.TryGetFeature<IFrameSyncInputHub>(out var inputHub));
        Assert.False(inputHub.SubmitInput(new ServerClientId("client-1"), worldId, input));
    }

    [Fact]
    public void WorldCreatedSession_WhenTicked_StillAcceptsInputThroughExistingHubPath()
    {
        var worlds = new InMemoryWorldManager();
        var options = new HostRuntimeOptions();
        var runtime = new HostRuntime(worlds, options);
        var module = new FrameSyncDriverModule();
        var connection = new RecordingConnection(new ServerClientId("client-1"));
        var worldId = new WorldId("world-backed-session");
        var input = new PlayerInputCommand(new FrameIndex(0), new PlayerId("player-1"), 103, new byte[] { 5 });

        module.Install(runtime, options);
        runtime.Connect(connection);

        runtime.CreateWorld(new WorldCreateOptions(worldId, "test-world"));

        Assert.True(runtime.Features.TryGetFeature<IFrameSyncInputHub>(out var inputHub));
        Assert.True(inputHub.SubmitInput(connection.ClientId, worldId, input));

        runtime.Tick(1f / 30f);

        var frameMessages = connection.Messages.OfType<FrameMessage>().ToArray();
        var frameMessage = Assert.Single(frameMessages);
        Assert.Equal(worldId, frameMessage.Packet.WorldId);
        Assert.Equal(1, frameMessage.Packet.Frame.Value);
        var packetInput = Assert.Single(frameMessage.Packet.Inputs);
        Assert.Equal(103, packetInput.OpCode);
    }

    private sealed class EmptyWorldManager : IWorldManager
    {
        private readonly IReadOnlyDictionary<WorldId, IWorld> _worlds = new Dictionary<WorldId, IWorld>();

        public IReadOnlyDictionary<WorldId, IWorld> Worlds => _worlds;

        public IWorld Create(WorldCreateOptions options)
        {
            throw new System.NotSupportedException("Headless frame sync tests do not create worlds.");
        }

        public bool TryGet(WorldId id, out IWorld world)
        {
            world = null!;
            return false;
        }

        public bool Destroy(WorldId id) => false;

        public void Tick(float deltaTime)
        {
        }

        public void DisposeAll()
        {
        }
    }

    private sealed class InMemoryWorldManager : IWorldManager
    {
        private readonly Dictionary<WorldId, IWorld> _worlds = new Dictionary<WorldId, IWorld>();

        public IReadOnlyDictionary<WorldId, IWorld> Worlds => _worlds;

        public IWorld Create(WorldCreateOptions options)
        {
            var world = new TestWorld(options.Id, options.WorldType);
            _worlds[world.Id] = world;
            return world;
        }

        public bool TryGet(WorldId id, out IWorld world)
        {
            return _worlds.TryGetValue(id, out world!);
        }

        public bool Destroy(WorldId id)
        {
            return _worlds.Remove(id);
        }

        public void Tick(float deltaTime)
        {
            foreach (var world in _worlds.Values)
            {
                world.Tick(deltaTime);
            }
        }

        public void DisposeAll()
        {
            _worlds.Clear();
        }
    }

    private sealed class TestWorld : IWorld
    {
        public TestWorld(WorldId id, string worldType)
        {
            Id = id;
            WorldType = worldType;
            Services = EmptyWorldResolver.Instance;
        }

        public WorldId Id { get; }

        public string WorldType { get; }

        public IWorldResolver Services { get; }

        public int TickCount { get; private set; }

        public void Initialize()
        {
        }

        public void Tick(float deltaTime)
        {
            TickCount++;
        }

        public void Dispose()
        {
        }
    }

    private sealed class EmptyWorldResolver : IWorldResolver
    {
        public static readonly EmptyWorldResolver Instance = new EmptyWorldResolver();

        private EmptyWorldResolver()
        {
        }

        public object Resolve(System.Type serviceType)
        {
            throw new System.InvalidOperationException($"Service not registered: {serviceType.FullName}");
        }

        public T Resolve<T>()
        {
            throw new System.InvalidOperationException($"Service not registered: {typeof(T).FullName}");
        }

        public bool TryResolve(System.Type serviceType, out object instance)
        {
            instance = null!;
            return false;
        }

        public bool TryResolve<T>(out T instance)
        {
            instance = default!;
            return false;
        }
    }

    private sealed class RecordingConnection : IServerConnection
    {
        public RecordingConnection(ServerClientId clientId)
        {
            ClientId = clientId;
        }

        public ServerClientId ClientId { get; }

        public List<ServerMessage> Messages { get; } = new List<ServerMessage>();

        public void Send(ServerMessage message)
        {
            Messages.Add(message);
        }
    }
}
