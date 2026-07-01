using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Services.Triggering;
using AbilityKit.Demo.Moba.Runtime.Application.Services.Triggering;
using Xunit;

namespace AbilityKit.Demo.Moba.Tests.Triggering;

public sealed class MobaTriggerExecutionGatewayTests
{
    [Fact]
    public void ExecuteDirectTrigger_records_invalid_trigger_id_without_service_dependency()
    {
        var diagnostics = new TestDiagnosticsService();
        var gateway = new MobaTriggerExecutionGateway(effects: null, subscriptions: null, diagnostics);

        var request = MobaTriggerExecutionRequest<string>.Create(0, "payload", "test.invalid");
        gateway.ExecuteDirectTrigger(in request);

        Assert.Equal(1, gateway.Stats.DirectInvalidRequests);
        Assert.Equal(0, gateway.Stats.DirectRequests);
        Assert.Equal(0, gateway.Stats.DirectExecuted);
        Assert.Equal(0, gateway.Stats.LastTriggerId);
        Assert.Equal("test.invalid", gateway.Stats.LastSource);
        Assert.Equal(1, diagnostics.CounterValue("moba.trigger.direct.invalidId"));
        Assert.Contains(diagnostics.Warnings, item => item.Key == "moba.trigger.direct.invalidId");
    }

    [Fact]
    public void ExecuteDirectTrigger_records_missing_effect_service()
    {
        var diagnostics = new TestDiagnosticsService();
        var gateway = new MobaTriggerExecutionGateway(effects: null, subscriptions: null, diagnostics);
        var payload = new TestPayload();

        var request = MobaTriggerExecutionRequest<TestPayload>.Create(7001, payload, "test.direct");
        var ex = Assert.Throws<InvalidOperationException>(() => gateway.ExecuteDirectTrigger(in request));

        Assert.Contains(nameof(MobaEffectExecutionService), ex.Message);
        Assert.Equal(1, gateway.Stats.DirectRequests);
        Assert.Equal(1, gateway.Stats.DirectMissingService);
        Assert.Equal(0, gateway.Stats.DirectExecuted);
        Assert.Equal(7001, gateway.Stats.LastTriggerId);
        Assert.Equal("test.direct", gateway.Stats.LastSource);
        Assert.Equal(nameof(TestPayload), gateway.Stats.LastPayloadType);
        Assert.Equal(1, diagnostics.CounterValue("moba.trigger.direct.requested"));
        Assert.Equal(1, diagnostics.CounterValue("moba.trigger.direct.missingEffects"));
    }

    [Fact]
    public void ApplyOwnerBoundTriggers_records_missing_subscription_service_and_trigger_count()
    {
        var diagnostics = new TestDiagnosticsService();
        var gateway = new MobaTriggerExecutionGateway(effects: null, subscriptions: null, diagnostics);

        var ex = Assert.Throws<InvalidOperationException>(() => gateway.ApplyOwnerBoundTriggers(new[] { 1, 2, 3 }, ownerKey: 9001, source: "test.owner.apply"));

        Assert.Contains(nameof(MobaTriggerPlanSubscriptionService), ex.Message);
        Assert.Equal(1, gateway.Stats.OwnerApplyRequests);
        Assert.Equal(1, gateway.Stats.OwnerMissingService);
        Assert.Equal(0, gateway.Stats.OwnerApplied);
        Assert.Equal(9001, gateway.Stats.LastOwnerKey);
        Assert.Equal(3, gateway.Stats.LastTriggerCount);
        Assert.Equal("apply", gateway.Stats.LastOwnerOperation);
        Assert.Equal("test.owner.apply", gateway.Stats.LastSource);
        Assert.Equal(1, diagnostics.CounterValue("moba.trigger.owner.apply.requested"));
        Assert.Equal(1, diagnostics.CounterValue("moba.trigger.owner.apply.missingSubscriptions"));
        Assert.Equal(3d, diagnostics.SampleValue("moba.trigger.owner.apply.count"));
    }

    [Fact]
    public void StopOwnerBoundTriggers_records_missing_subscription_service()
    {
        var diagnostics = new TestDiagnosticsService();
        var gateway = new MobaTriggerExecutionGateway(effects: null, subscriptions: null, diagnostics);

        var ex = Assert.Throws<InvalidOperationException>(() => gateway.StopOwnerBoundTriggers(ownerKey: 9002, source: "test.owner.stop"));

        Assert.Contains(nameof(MobaTriggerPlanSubscriptionService), ex.Message);
        Assert.Equal(1, gateway.Stats.OwnerStopRequests);
        Assert.Equal(1, gateway.Stats.OwnerMissingService);
        Assert.Equal(0, gateway.Stats.OwnerStopped);
        Assert.Equal(9002, gateway.Stats.LastOwnerKey);
        Assert.Equal("stop", gateway.Stats.LastOwnerOperation);
        Assert.Equal("test.owner.stop", gateway.Stats.LastSource);
        Assert.Equal(1, diagnostics.CounterValue("moba.trigger.owner.stop.requested"));
        Assert.Equal(1, diagnostics.CounterValue("moba.trigger.owner.stop.missingSubscriptions"));
    }

    [Fact]
    public void OwnerBound_operations_record_invalid_owner_key()
    {
        var diagnostics = new TestDiagnosticsService();
        var gateway = new MobaTriggerExecutionGateway(effects: null, subscriptions: null, diagnostics);

        gateway.ApplyOwnerBoundTriggers(new[] { 1 }, ownerKey: 0, source: "test.owner.invalid.apply");
        gateway.StopOwnerBoundTriggers(ownerKey: 0, source: "test.owner.invalid.stop");

        Assert.Equal(2, gateway.Stats.OwnerInvalidRequests);
        Assert.Equal("stop", gateway.Stats.LastOwnerOperation);
        Assert.Equal("test.owner.invalid.stop", gateway.Stats.LastSource);
        Assert.Equal(2, diagnostics.CounterValue("moba.trigger.owner.invalidOwnerKey"));
    }

    private sealed class TestPayload
    {
    }

    private sealed class TestDiagnosticsService : IMobaBattleDiagnosticsService
    {
        public readonly Dictionary<string, long> Counters = new();
        public readonly Dictionary<string, double> Samples = new();
        public readonly List<KeyValuePair<string, string>> Warnings = new();

        public long GetTimestamp() => 0L;
        public bool IsEnabled(string channel) => true;
        public bool ShouldSample(string channel) => true;
        public MobaBattleDiagnosticScope Measure(string metricName, double warnThresholdMs = 0d, string context = null) => default;
        public void RecordDuration(string metricName, long startTimestamp, double warnThresholdMs = 0d, string context = null) { }
        public void Counter(string counterName, long value = 1L) => Counters[counterName] = CounterValue(counterName) + value;
        public void Gauge(string gaugeName, long value) { }
        public void Sample(string sampleName, double value) => Samples[sampleName] = value;
        public void Warning(string key, string message, int maxCount = MobaBattleDiagnosticsDefaults.DefaultWarningLimit) => Warnings.Add(new KeyValuePair<string, string>(key, message));
        public void Warning(string key, Func<string> messageFactory, int maxCount = MobaBattleDiagnosticsDefaults.DefaultWarningLimit) => Warnings.Add(new KeyValuePair<string, string>(key, messageFactory != null ? messageFactory() : null));
        public void Exception(string key, Exception exception, string context, int maxCount = MobaBattleDiagnosticsDefaults.DefaultExceptionLimit) { }
        public IReadOnlyList<MobaBattleDiagnosticWarningRecord> GetWarningsSnapshot() => Array.Empty<MobaBattleDiagnosticWarningRecord>();

        public long CounterValue(string key) => Counters.TryGetValue(key, out var value) ? value : 0L;
        public double SampleValue(string key) => Samples.TryGetValue(key, out var value) ? value : 0d;
    }
}
