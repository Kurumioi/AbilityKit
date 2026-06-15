using AbilityKit.Game.Flow;
using Xunit;

namespace AbilityKit.Game.View.Runtime.Tests
{
    /// <summary>
    /// 覆盖 _battleSessionStarted / _battleFirstFrameReceived 字段迁入 scoped <see cref="IBattleRuntimeState"/> 后的语义：
    /// 默认 false、同 scope 共享、跨 scope 隔离、Reset 清零、退出战斗（scope 释放）后重入得到 fresh 实例。
    /// </summary>
    public sealed class BattleRuntimeStateTests
    {
        [Fact]
        public void Resolve_WithinScope_DefaultsToFalse()
        {
            using var host = new BattleWorldScopeHost();
            host.BeginBattle();

            var state = host.Resolve<IBattleRuntimeState>();

            Assert.False(state.SessionStarted);
            Assert.False(state.FirstFrameReceived);
        }

        [Fact]
        public void Resolve_WithinSameScope_SharesSingleInstance()
        {
            using var host = new BattleWorldScopeHost();
            host.BeginBattle();

            var first = host.Resolve<IBattleRuntimeState>();
            first.SessionStarted = true;
            first.FirstFrameReceived = true;

            var second = host.Resolve<IBattleRuntimeState>();

            Assert.Same(first, second);
            Assert.True(second.SessionStarted);
            Assert.True(second.FirstFrameReceived);
        }

        [Fact]
        public void Reset_ClearsFlags()
        {
            using var host = new BattleWorldScopeHost();
            host.BeginBattle();

            var state = host.Resolve<IBattleRuntimeState>();
            state.SessionStarted = true;
            state.FirstFrameReceived = true;

            state.Reset();

            Assert.False(state.SessionStarted);
            Assert.False(state.FirstFrameReceived);
        }

        [Fact]
        public void Reenter_AfterEndBattle_YieldsFreshState()
        {
            using var host = new BattleWorldScopeHost();

            host.BeginBattle();
            var firstBattle = host.Resolve<IBattleRuntimeState>();
            firstBattle.SessionStarted = true;
            firstBattle.FirstFrameReceived = true;
            host.EndBattle();

            host.BeginBattle();
            var secondBattle = host.Resolve<IBattleRuntimeState>();

            Assert.NotSame(firstBattle, secondBattle);
            Assert.False(secondBattle.SessionStarted);
            Assert.False(secondBattle.FirstFrameReceived);
        }
    }
}
