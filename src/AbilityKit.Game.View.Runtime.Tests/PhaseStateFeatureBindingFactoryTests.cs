using System.Collections.Generic;
using AbilityKit.Game.View.Flow;
using Xunit;

namespace AbilityKit.Game.View.Runtime.Tests;

public sealed class PhaseStateFeatureBindingFactoryTests
{
    [Fact]
    public void Create_UsesSpecStateNameFeatureIdsAndClearPolicy()
    {
        var events = new List<string>();
        var ctx = new TestContext(3);
        var spec = new PhaseStateFeatureSpec("Battle.Prepare", clearBeforeEnter: true)
            .AddFeature("session")
            .AddFeature("context");
        var plan = new PhaseFeaturePlan<TestContext, IPhaseFeature<TestContext>>()
            .Add("context", (in TestContext c) => new TestFeature("context", c.Value, events))
            .Add("session", (in TestContext c) => new TestFeature("session", c.Value, events));

        var binding = PhaseStateFeatureBindingFactory.Create<TestContext, IPhaseFeature<TestContext>>(
            spec,
            feature => feature.OnAttach(in ctx),
            plan,
            clear: (in TestContext c) => events.Add($"clear:{c.Value}"));

        var installed = binding.Enter(in ctx);

        Assert.Equal("Battle.Prepare", binding.Name);
        Assert.True(binding.ClearBeforeEnter);
        Assert.Same(spec.FeatureIds, binding.FeatureIds);
        Assert.Equal(2, installed);
        Assert.Equal(new[] { "clear:3", "attach:session:3", "attach:context:3" }, events);
    }

    [Fact]
    public void Create_RunsHooksAroundInstallation()
    {
        var events = new List<string>();
        var ctx = new TestContext(7);
        var spec = new PhaseStateFeatureSpec("Battle.InMatch")
            .AddFeature("hud");
        var plan = new PhaseFeaturePlan<TestContext, IPhaseFeature<TestContext>>()
            .Add("hud", (in TestContext c) => new TestFeature("hud", c.Value, events));

        var binding = PhaseStateFeatureBindingFactory.Create<TestContext, IPhaseFeature<TestContext>>(
            spec,
            feature => feature.OnAttach(in ctx),
            plan,
            beforeEnter: (in TestContext c) => events.Add($"before:{c.Value}"),
            afterEnter: (in TestContext c, int count) => events.Add($"after:{c.Value}:{count}"));

        var installed = binding.Enter(in ctx);

        Assert.Equal(1, installed);
        Assert.Equal(new[] { "before:7", "attach:hud:7", "after:7:1" }, events);
    }

    [Fact]
    public void Create_WithActionRefs_RunsResolvedActionsAroundInstallation()
    {
        var events = new List<string>();
        var ctx = new TestContext(11);
        var spec = new PhaseStateFeatureSpec("Battle.End")
            .AddEnterBeforeAction("before.cleanup")
            .AddFeature("hud")
            .AddEnterAfterAction("after.return_lobby")
            .AddExitAction("exit.detach_session");
        var plan = new PhaseFeaturePlan<TestContext, IPhaseFeature<TestContext>>()
            .Add("hud", (in TestContext c) => new TestFeature("hud", c.Value, events));

        var binding = PhaseStateFeatureBindingFactory.Create<TestContext, IPhaseFeature<TestContext>>(
            spec,
            feature => feature.OnAttach(in ctx),
            plan,
            beforeEnter: (in TestContext c) => events.Add($"before-hook:{c.Value}"),
            afterEnter: (in TestContext c, int count) => events.Add($"after-hook:{c.Value}:{count}"),
            enterBeforeAction: (in TestContext c, string actionId) => events.Add($"before-action:{actionId}:{c.Value}"),
            enterAfterAction: (in TestContext c, string actionId, int count) => events.Add($"after-action:{actionId}:{c.Value}:{count}"),
            onExit: (in TestContext c) => events.Add($"exit-hook:{c.Value}"),
            exitAction: (in TestContext c, string actionId) => events.Add($"exit-action:{actionId}:{c.Value}"));
 
        var installed = binding.Enter(in ctx);
        binding.Exit(in ctx);
 
        Assert.Equal(new[] { "before.cleanup" }, spec.EnterBeforeActionIds);
        Assert.Equal(new[] { "after.return_lobby" }, spec.EnterAfterActionIds);
        Assert.Equal(new[] { "exit.detach_session" }, spec.ExitActionIds);
        Assert.Equal(1, installed);
        Assert.Equal(new[]
        {
            "before-hook:11",
            "before-action:before.cleanup:11",
            "attach:hud:11",
            "after-hook:11:1",
            "after-action:after.return_lobby:11:1",
            "exit-hook:11",
            "exit-action:exit.detach_session:11"
        }, events);
    }

    [Fact]
    public void Create_WithSwitchFlowRefs_RunsAfterEnterCompletes()
    {
        var events = new List<string>();
        var ctx = new TestContext(5);
        var spec = new PhaseStateFeatureSpec("Battle.Connect")
            .AddFeature("hud")
            .AddSwitchFlow("battle.advance_connect");
        var plan = new PhaseFeaturePlan<TestContext, IPhaseFeature<TestContext>>()
            .Add("hud", (in TestContext c) => new TestFeature("hud", c.Value, events));

        var binding = PhaseStateFeatureBindingFactory.Create<TestContext, IPhaseFeature<TestContext>>(
            spec,
            feature => feature.OnAttach(in ctx),
            plan,
            afterEnter: (in TestContext c, int count) => events.Add($"after-hook:{c.Value}:{count}"),
            switchFlowAction: (in TestContext c, string switchFlowId, int count) => events.Add($"switch:{switchFlowId}:{c.Value}:{count}"));

        var installed = binding.Enter(in ctx);

        Assert.Equal(new[] { "battle.advance_connect" }, spec.SwitchFlowIds);
        Assert.Equal(1, installed);
        Assert.Equal(new[]
        {
            "attach:hud:5",
            "after-hook:5:1",
            "switch:battle.advance_connect:5:1"
        }, events);
    }

    [Fact]
    public void Create_WithNullSpec_Throws()
    {
        Assert.Throws<System.ArgumentNullException>(() =>
            PhaseStateFeatureBindingFactory.Create<TestContext, IPhaseFeature<TestContext>>(
                null!,
                _ => { }));
    }

    private readonly record struct TestContext(int Value);

    private sealed class TestFeature : IPhaseFeature<TestContext>
    {
        private readonly string _id;
        private readonly int _createdWith;
        private readonly List<string> _events;

        public TestFeature(string id, int createdWith, List<string> events)
        {
            _id = id;
            _createdWith = createdWith;
            _events = events;
        }

        public void OnAttach(in TestContext ctx)
        {
            _events.Add($"attach:{_id}:{_createdWith}");
        }

        public void OnDetach(in TestContext ctx)
        {
            _events.Add($"detach:{_id}:{ctx.Value}");
        }

        public void Tick(in TestContext ctx, float deltaTime)
        {
            _events.Add($"tick:{_id}:{deltaTime:0.00}");
        }
    }
}
