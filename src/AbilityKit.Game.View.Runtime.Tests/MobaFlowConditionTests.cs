using AbilityKit.Game.Flow;
using Xunit;

namespace AbilityKit.Game.View.Runtime.Tests;

public sealed class MobaFlowConditionTests
{
    private static MobaFlowConditionContext AllReady()
    {
        return new MobaFlowConditionContext(
            battleRequested: true,
            authenticated: true,
            roomReady: true,
            connectivityReady: true,
            assetsReady: true);
    }

    [Fact]
    public void BattleEntryReady_RequiresAllGates()
    {
        var ctx = AllReady();

        Assert.True(ctx.BattleEntryReady);
    }

    [Theory]
    [InlineData(false, true, true, true, true)]
    [InlineData(true, false, true, true, true)]
    [InlineData(true, true, false, true, true)]
    [InlineData(true, true, true, false, true)]
    [InlineData(true, true, true, true, false)]
    public void BattleEntryReady_FalseWhenAnyGateMissing(
        bool battleRequested,
        bool authenticated,
        bool roomReady,
        bool connectivityReady,
        bool assetsReady)
    {
        var ctx = new MobaFlowConditionContext(
            battleRequested,
            authenticated,
            roomReady,
            connectivityReady,
            assetsReady);

        Assert.False(ctx.BattleEntryReady);
    }

    [Fact]
    public void Evaluate_EmptyConditionId_ReturnsTrue()
    {
        var resolver = new MobaFlowConditionResolver();
        var ctx = new MobaFlowConditionContext(false, false, false, false, false);

        Assert.True(resolver.Evaluate(string.Empty, in ctx));
        Assert.True(resolver.Evaluate(null!, in ctx));
    }

    [Fact]
    public void Evaluate_UnknownConditionId_ReturnsFalse()
    {
        var resolver = new MobaFlowConditionResolver();
        var ctx = AllReady();

        Assert.False(resolver.Evaluate("not_a_real_condition", in ctx));
    }

    [Fact]
    public void Evaluate_EachAtomicConditionId_MapsToMatchingGate()
    {
        var resolver = new MobaFlowConditionResolver();
        var ctx = new MobaFlowConditionContext(
            battleRequested: true,
            authenticated: false,
            roomReady: true,
            connectivityReady: false,
            assetsReady: true);

        Assert.True(resolver.Evaluate(MobaFlowConditionIds.BattleRequested, in ctx));
        Assert.False(resolver.Evaluate(MobaFlowConditionIds.Authenticated, in ctx));
        Assert.True(resolver.Evaluate(MobaFlowConditionIds.RoomReady, in ctx));
        Assert.False(resolver.Evaluate(MobaFlowConditionIds.ConnectivityReady, in ctx));
        Assert.True(resolver.Evaluate(MobaFlowConditionIds.AssetsReady, in ctx));
    }

    [Fact]
    public void Evaluate_BattleEntryReadyId_MatchesContextCombination()
    {
        var resolver = new MobaFlowConditionResolver();

        var ready = AllReady();
        var notReady = new MobaFlowConditionContext(true, true, true, true, false);

        Assert.True(resolver.Evaluate(MobaFlowConditionIds.BattleEntryReady, in ready));
        Assert.False(resolver.Evaluate(MobaFlowConditionIds.BattleEntryReady, in notReady));
    }
}
