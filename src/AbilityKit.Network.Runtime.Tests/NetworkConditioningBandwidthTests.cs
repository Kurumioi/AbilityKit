using System;
using System.Collections.Generic;
using AbilityKit.Network.Abstractions;
using AbilityKit.Network.Protocol;
using AbilityKit.Network.Runtime;
using AbilityKit.Network.Runtime.Conditioning;
using Xunit;

namespace AbilityKit.Network.Runtime.Tests;

public sealed class NetworkConditioningBandwidthTests
{
    private static readonly NetworkPacketHeader TestHeader =
        new NetworkPacketHeader(NetworkPacketFlags.None, 1u, 1u, 0u);

    [Fact]
    public void BandwidthHoldsPacketUntilSerializationCompletes()
    {
        long now = 0;
        var middleware = CreateMiddleware(bandwidthKbps: 8, () => now);
        var delivered = new List<string>();

        ScheduleInbound(middleware, payloadBytes: 2, "in", delivered);

        middleware.Advance(1);
        Assert.Empty(delivered);

        middleware.Advance(2);
        Assert.Equal(new[] { "in" }, delivered);
    }

    [Fact]
    public void PacketsInSameDirectionShareSerializationQueue()
    {
        long now = 0;
        var middleware = CreateMiddleware(bandwidthKbps: 8, () => now);
        var delivered = new List<string>();

        ScheduleInbound(middleware, payloadBytes: 1, "first", delivered);
        ScheduleInbound(middleware, payloadBytes: 1, "second", delivered);

        middleware.Advance(1);
        Assert.Equal(new[] { "first" }, delivered);

        middleware.Advance(2);
        Assert.Equal(new[] { "first", "second" }, delivered);
    }

    [Fact]
    public void InboundAndOutboundUseIndependentBandwidthQueues()
    {
        long now = 0;
        var middleware = CreateMiddleware(bandwidthKbps: 8, () => now);
        var delivered = new List<string>();
        var payload = new byte[1];

        middleware.OnInbound(null!, TestHeader, new ArraySegment<byte>(payload), (_, _) => delivered.Add("in"));
        middleware.OnOutbound(null!, TestHeader, new ArraySegment<byte>(payload), (_, _) => delivered.Add("out"));

        middleware.Advance(1);

        Assert.Equal(new[] { "in", "out" }, delivered);
    }

    [Fact]
    public void ZeroBandwidthMeansUnlimited()
    {
        long now = 0;
        var middleware = CreateMiddleware(bandwidthKbps: 0, () => now);
        var delivered = new List<string>();

        ScheduleInbound(middleware, payloadBytes: 4096, "in", delivered);
        middleware.Advance(now);

        Assert.Equal(new[] { "in" }, delivered);
    }

    private static NetworkConditioningMiddleware CreateMiddleware(int bandwidthKbps, Func<long> clock)
    {
        var profile = new NetworkConditionProfile(0, 0, 0d, 0d, bandwidthKbps);
        return new NetworkConditioningMiddleware(profile, clock, seed: 1);
    }

    private static void ScheduleInbound(
        NetworkConditioningMiddleware middleware,
        int payloadBytes,
        string label,
        ICollection<string> delivered)
    {
        middleware.OnInbound(
            null!,
            TestHeader,
            new ArraySegment<byte>(new byte[payloadBytes]),
            (_, _) => delivered.Add(label));
    }
}

public sealed class NetworkPipelineMutationTests
{
    [Fact]
    public void AddFirstRunsBeforeExistingMiddlewareAndRemoveRestoresChain()
    {
        var calls = new List<string>();
        var pipeline = new NetworkPipeline();
        var existing = new RecordingMiddleware("existing", calls);
        var first = new RecordingMiddleware("first", calls);

        pipeline.Add(existing);
        pipeline.AddFirst(first);

        pipeline.ProcessInbound(new StubSessionContext(), default, default, (_, _) => calls.Add("terminal"));
        Assert.Equal(new[] { "first", "existing", "terminal" }, calls);
        Assert.Equal(2, pipeline.Count);

        calls.Clear();
        Assert.True(pipeline.Remove(first));
        Assert.False(pipeline.Remove(first));
        pipeline.ProcessInbound(new StubSessionContext(), default, default, (_, _) => calls.Add("terminal"));

        Assert.Equal(new[] { "existing", "terminal" }, calls);
        Assert.Equal(1, pipeline.Count);
    }

    private sealed class RecordingMiddleware : INetworkMiddleware
    {
        private readonly string _name;
        private readonly ICollection<string> _calls;

        public RecordingMiddleware(string name, ICollection<string> calls)
        {
            _name = name;
            _calls = calls;
        }

        public void OnInbound(
            ISessionContext context,
            NetworkPacketHeader header,
            ArraySegment<byte> payload,
            Action<NetworkPacketHeader, ArraySegment<byte>> next)
        {
            _calls.Add(_name);
            next(header, payload);
        }

        public void OnOutbound(
            ISessionContext context,
            NetworkPacketHeader header,
            ArraySegment<byte> payload,
            Action<NetworkPacketHeader, ArraySegment<byte>> next)
        {
            _calls.Add(_name);
            next(header, payload);
        }
    }

    private sealed class StubSessionContext : ISessionContext
    {
        public ISession Session => null!;
        public IDispatcher Dispatcher => InlineDispatcher.Instance;
        public void Send(NetworkPacketHeader header, ArraySegment<byte> payload) { }
    }
}
