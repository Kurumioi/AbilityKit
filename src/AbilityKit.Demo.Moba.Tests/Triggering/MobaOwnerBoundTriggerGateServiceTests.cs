using AbilityKit.Demo.Moba.Services.Triggering;
using Xunit;

namespace AbilityKit.Demo.Moba.Tests.Triggering;

public sealed class MobaOwnerBoundTriggerGateServiceTests
{
    [Fact]
    public void Registered_gate_can_match_block_and_complete_owner_bound_trigger()
    {
        var service = new MobaOwnerBoundTriggerGateService();
        var gate = new TestGate(ownerKey: 10, triggerId: 1001, canExecute: false);

        service.RegisterGate(gate);

        Assert.True(service.HasGate(10, 1001));
        Assert.False(service.HasGate(10, 1002));
        Assert.False(service.CanExecute(10, 1001));
        Assert.True(service.CanExecute(10, 1002));

        service.Complete(10, 1001);
        service.Complete(10, 1002);

        Assert.Equal(1, gate.CompleteCount);
    }

    private sealed class TestGate : IMobaOwnerBoundTriggerGate
    {
        private readonly long _ownerKey;
        private readonly int _triggerId;
        private readonly bool _canExecute;

        public TestGate(long ownerKey, int triggerId, bool canExecute)
        {
            _ownerKey = ownerKey;
            _triggerId = triggerId;
            _canExecute = canExecute;
        }

        public int CompleteCount { get; private set; }

        public bool IsMatch(long ownerKey, int triggerId)
        {
            return ownerKey == _ownerKey && triggerId == _triggerId;
        }

        public bool CanExecute(long ownerKey, int triggerId)
        {
            return _canExecute;
        }

        public void Complete(long ownerKey, int triggerId)
        {
            CompleteCount++;
        }
    }
}
