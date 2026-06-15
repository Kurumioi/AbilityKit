using System;
using AbilityKit.Game.Flow;
using Xunit;

namespace AbilityKit.Game.View.Runtime.Tests
{
    /// <summary>
    /// Step 2：验证 <see cref="BattleWorldScopeHost"/> 的 per-battle scope 生命周期闭环——
    /// 进入战斗建 scope、退出战斗 Dispose、重入换新 scope 并隔离实例、Dispose 安全。
    /// </summary>
    public sealed class BattleWorldScopeHostTests
    {
        [Fact]
        public void BeginBattle_CreatesActiveScope()
        {
            using var host = new BattleWorldScopeHost();

            Assert.False(host.HasActiveScope);
            Assert.Equal(0, host.ScopeGeneration);

            host.BeginBattle();

            Assert.True(host.HasActiveScope);
            Assert.Equal(1, host.ScopeGeneration);
        }

        [Fact]
        public void Resolve_WithoutActiveScope_Throws()
        {
            using var host = new BattleWorldScopeHost();

            Assert.Throws<InvalidOperationException>(() => host.Resolve<IBattleScopedSession>());
        }

        [Fact]
        public void Resolve_WithinActiveScope_SharesSingleInstance()
        {
            using var host = new BattleWorldScopeHost();
            host.BeginBattle();

            var a = host.Resolve<IBattleScopedSession>();
            var b = host.Resolve<IBattleScopedSession>();

            Assert.Same(a, b);
        }

        [Fact]
        public void EndBattle_DisposesScopedInstances()
        {
            using var host = new BattleWorldScopeHost();
            host.BeginBattle();
            var session = host.Resolve<IBattleScopedSession>();

            Assert.False(session.Disposed);

            host.EndBattle();

            Assert.False(host.HasActiveScope);
            Assert.True(session.Disposed);
        }

        [Fact]
        public void EndBattle_WithoutBegin_IsSafe()
        {
            using var host = new BattleWorldScopeHost();

            host.EndBattle();
            host.EndBattle();

            Assert.False(host.HasActiveScope);
        }

        [Fact]
        public void Reenter_DisposesPreviousScope_AndIsolatesInstances()
        {
            using var host = new BattleWorldScopeHost();

            host.BeginBattle();
            var first = host.Resolve<IBattleScopedSession>();
            host.EndBattle();

            host.BeginBattle();
            var second = host.Resolve<IBattleScopedSession>();

            Assert.True(first.Disposed);
            Assert.False(second.Disposed);
            Assert.NotSame(first, second);
            Assert.NotEqual(first.InstanceId, second.InstanceId);
            Assert.Equal(2, host.ScopeGeneration);
        }

        [Fact]
        public void BeginBattle_WhenScopeAlreadyActive_DisposesOldScope()
        {
            using var host = new BattleWorldScopeHost();

            host.BeginBattle();
            var first = host.Resolve<IBattleScopedSession>();

            // 不调用 EndBattle 直接重入（模拟异常路径/重复进入），旧 scope 应被释放。
            host.BeginBattle();
            var second = host.Resolve<IBattleScopedSession>();

            Assert.True(first.Disposed);
            Assert.False(second.Disposed);
            Assert.NotSame(first, second);
            Assert.Equal(2, host.ScopeGeneration);
        }

        [Fact]
        public void Dispose_ReleasesActiveScope()
        {
            var host = new BattleWorldScopeHost();
            host.BeginBattle();
            var session = host.Resolve<IBattleScopedSession>();

            host.Dispose();

            Assert.True(session.Disposed);
        }

        [Fact]
        public void Dispose_IsIdempotent()
        {
            var host = new BattleWorldScopeHost();
            host.BeginBattle();

            host.Dispose();
            host.Dispose();
        }

        [Fact]
        public void Resolve_AfterDispose_Throws()
        {
            var host = new BattleWorldScopeHost();
            host.Dispose();

            Assert.Throws<ObjectDisposedException>(() => host.BeginBattle());
        }
    }
}
