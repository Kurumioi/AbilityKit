using AbilityKit.Ability.World.DI;
using AbilityKit.Game.Flow;
using Xunit;

namespace AbilityKit.Game.View.Runtime.Tests
{
    /// <summary>
    /// Step 1 验证：<see cref="BattleWorldModule"/> 的 scope 边界闭环（compose / resolve / 隔离 / dispose）。
    /// 这一步只验证 world.di 作用域语义对 battle 服务成立，尚未接入 flow。
    /// </summary>
    public sealed class BattleWorldModuleTests
    {
        private static WorldContainer BuildContainer()
        {
            return new WorldContainerBuilder()
                .AddModule(new BattleWorldModule())
                .Build();
        }

        [Fact]
        public void Compose_RegistersBattleScopedServices()
        {
            using var container = BuildContainer();

            Assert.True(container.IsRegistered(typeof(IBattleScopedSession)));
            Assert.True(container.IsRegistered(typeof(IBattleScopedClock)));
        }

        [Fact]
        public void Resolve_WithinSameScope_SharesSingleInstance()
        {
            using var container = BuildContainer();
            using var scope = container.CreateScope();

            var a = scope.Resolve<IBattleScopedSession>();
            var b = scope.Resolve<IBattleScopedSession>();

            Assert.Same(a, b);
        }

        [Fact]
        public void Resolve_ConstructorInjection_SharesScopedDependency()
        {
            using var container = BuildContainer();
            using var scope = container.CreateScope();

            var clock = scope.Resolve<IBattleScopedClock>();
            var session = scope.Resolve<IBattleScopedSession>();

            // 时钟构造注入拿到的 session，应与 scope 内直接解析到的为同一引用。
            Assert.Same(session, clock.Session);
        }

        [Fact]
        public void Resolve_AcrossDifferentScopes_IsolatesInstances()
        {
            using var container = BuildContainer();

            IBattleScopedSession first;
            IBattleScopedSession second;

            using (var scopeA = container.CreateScope())
            {
                first = scopeA.Resolve<IBattleScopedSession>();
            }

            using (var scopeB = container.CreateScope())
            {
                second = scopeB.Resolve<IBattleScopedSession>();
            }

            Assert.NotSame(first, second);
            Assert.NotEqual(first.InstanceId, second.InstanceId);
        }

        [Fact]
        public void DisposeScope_DisposesScopedInstances()
        {
            using var container = BuildContainer();

            IBattleScopedSession session;
            using (var scope = container.CreateScope())
            {
                session = scope.Resolve<IBattleScopedSession>();
                Assert.False(session.Disposed);
            }

            // 退出 scope 后，scoped 实例应被自动释放（模拟"退出战斗阶段"）。
            Assert.True(session.Disposed);
        }

        [Fact]
        public void DisposeScope_DoesNotAffectOtherLiveScope()
        {
            using var container = BuildContainer();
            using var liveScope = container.CreateScope();

            var liveSession = liveScope.Resolve<IBattleScopedSession>();

            using (var temp = container.CreateScope())
            {
                _ = temp.Resolve<IBattleScopedSession>();
            }

            // 一个 scope 释放不应波及另一个仍存活的 scope（模拟多 world 并存）。
            Assert.False(liveSession.Disposed);
        }
    }
}
