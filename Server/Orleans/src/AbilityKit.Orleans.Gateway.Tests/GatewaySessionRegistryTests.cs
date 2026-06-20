using System.Threading.Tasks;
using AbilityKit.Orleans.Gateway.Gateway.Core;
using AbilityKit.Orleans.Gateway.Gateway.Abstractions;
using Xunit;

namespace AbilityKit.Orleans.Gateway.Tests;

public sealed class GatewaySessionRegistryTests
{
    [Fact]
    public void Register_should_store_session_and_unbind_on_unregister()
    {
        var registry = new GatewaySessionRegistry();
        var session = new FakeGatewayTransportSession(7);

        registry.Register(session.ConnectionId, session);

        Assert.True(registry.TryGetSession(session.ConnectionId, out var stored));
        Assert.Same(session, stored);

        registry.Unregister(session.ConnectionId);

        Assert.False(registry.TryGetSession(session.ConnectionId, out _));
    }

    private sealed class FakeGatewayTransportSession : IGatewayTransportSession
    {
        public FakeGatewayTransportSession(long connectionId)
        {
            ConnectionId = connectionId;
        }

        public long ConnectionId { get; }

        public GatewaySessionContext Context { get; } = new(7);

        public Task SendResponseAsync(uint opCode, uint seq, byte[] payload, System.Threading.CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task SendServerPushAsync(uint opCode, byte[] payload, System.Threading.CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
