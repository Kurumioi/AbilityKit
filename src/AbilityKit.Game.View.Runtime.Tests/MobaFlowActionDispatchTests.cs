using System.Collections.Generic;
using AbilityKit.Game.Flow;
using Xunit;

namespace AbilityKit.Game.View.Runtime.Tests
{
    /// <summary>
    /// Records method calls against <see cref="IMobaFlowActionTarget"/> for test assertions.
    /// </summary>
    internal sealed class FakeFlowActionTarget : IMobaFlowActionTarget
    {
        public List<string> Calls { get; } = new();

        public void ResetBattleSessionRuntimeState() => Calls.Add(nameof(ResetBattleSessionRuntimeState));
        public void ReturnLobbyAfterBattleEnd() => Calls.Add(nameof(ReturnLobbyAfterBattleEnd));
        public void TryAdvanceOnConnectEnter() => Calls.Add(nameof(TryAdvanceOnConnectEnter));
        public void TryAdvanceOnCreateOrJoinWorldEnter() => Calls.Add(nameof(TryAdvanceOnCreateOrJoinWorldEnter));
        public void TryAdvanceOnLoadAssetsEnter() => Calls.Add(nameof(TryAdvanceOnLoadAssetsEnter));
    }

    public class MobaFlowActionExecutorTests
    {
        private readonly MobaFlowActionExecutor _executor = new();
        private readonly FakeFlowActionTarget _target = new();

        private MobaFlowActionContext MakeCtx(int installedCount = 0) => new(_target, installedCount);

        [Fact]
        public void ResetBattleSessionRuntimeState_Dispatches()
        {
            var ctx = MakeCtx();
            Assert.True(_executor.Execute(MobaFlowActionIds.ResetBattleSessionRuntimeState, in ctx));
            Assert.Equal(new[] { nameof(FakeFlowActionTarget.ResetBattleSessionRuntimeState) }, _target.Calls);
        }

        [Fact]
        public void ReturnLobbyAfterBattleEnd_Dispatches()
        {
            var ctx = MakeCtx();
            Assert.True(_executor.Execute(MobaFlowActionIds.ReturnLobbyAfterBattleEnd, in ctx));
            Assert.Equal(new[] { nameof(FakeFlowActionTarget.ReturnLobbyAfterBattleEnd) }, _target.Calls);
        }

        [Fact]
        public void EmptyActionId_ReturnsTrue_NoDispatch()
        {
            var ctx = MakeCtx();
            Assert.True(_executor.Execute(string.Empty, in ctx));
            Assert.Empty(_target.Calls);
        }

        [Fact]
        public void UnknownAction_ReturnsFalse()
        {
            var ctx = MakeCtx();
            Assert.False(_executor.Execute("unknown_action", in ctx));
            Assert.Empty(_target.Calls);
        }
    }

    public class MobaFlowSwitchExecutorTests
    {
        private readonly MobaFlowSwitchExecutor _executor = new();
        private readonly FakeFlowActionTarget _target = new();

        private MobaFlowActionContext MakeCtx(int installedCount = 0) => new(_target, installedCount);

        [Fact]
        public void AdvanceOnConnectEnter_Dispatches()
        {
            var ctx = MakeCtx();
            Assert.True(_executor.Execute(MobaFlowSwitchIds.AdvanceOnConnectEnter, in ctx));
            Assert.Equal(new[] { nameof(FakeFlowActionTarget.TryAdvanceOnConnectEnter) }, _target.Calls);
        }

        [Fact]
        public void AdvanceOnCreateOrJoinWorldEnter_Dispatches()
        {
            var ctx = MakeCtx();
            Assert.True(_executor.Execute(MobaFlowSwitchIds.AdvanceOnCreateOrJoinWorldEnter, in ctx));
            Assert.Equal(new[] { nameof(FakeFlowActionTarget.TryAdvanceOnCreateOrJoinWorldEnter) }, _target.Calls);
        }

        [Fact]
        public void AdvanceOnLoadAssetsEnter_Dispatches()
        {
            var ctx = MakeCtx();
            Assert.True(_executor.Execute(MobaFlowSwitchIds.AdvanceOnLoadAssetsEnter, in ctx));
            Assert.Equal(new[] { nameof(FakeFlowActionTarget.TryAdvanceOnLoadAssetsEnter) }, _target.Calls);
        }

        [Fact]
        public void EmptySwitchFlowId_ReturnsTrue_NoDispatch()
        {
            var ctx = MakeCtx();
            Assert.True(_executor.Execute(string.Empty, in ctx));
            Assert.Empty(_target.Calls);
        }

        [Fact]
        public void UnknownSwitch_ReturnsFalse()
        {
            var ctx = MakeCtx();
            Assert.False(_executor.Execute("unknown_switch", in ctx));
            Assert.Empty(_target.Calls);
        }
    }
}
