using System;
using System.Collections.Generic;
using AbilityKit.Core.Continuous;
using AbilityKit.Demo.Moba.Services;
using Xunit;

namespace AbilityKit.Demo.Moba.Tests.Continuous;

public sealed class MobaContinuousLifecycleTests
{
    [Fact]
    public void Runtime_base_transitions_active_pause_resume_and_end()
    {
        var runtime = new TestContinuous(new TestContinuousConfig(ownerId: 101));
        var ended = new List<ContinuousEndReason>();
        runtime.OnEnded += (_, reason) => ended.Add(reason);

        Assert.Equal(ContinuousState.Inactive, runtime.State);
        Assert.Equal(0f, runtime.ElapsedSeconds);

        runtime.Activate();
        runtime.Advance(1.25f);
        runtime.Advance(0f);
        runtime.Pause();
        runtime.Resume();
        runtime.End(ContinuousEndReason.Completed);

        Assert.Equal(ContinuousState.Expired, runtime.State);
        Assert.True(runtime.IsTerminated);
        Assert.Equal(1.25f, runtime.ElapsedSeconds);
        Assert.Equal(new[] { ContinuousEndReason.Completed }, ended);
    }

    [Fact]
    public void Runtime_base_interrupts_when_activation_is_rejected()
    {
        var runtime = new TestContinuous(new TestContinuousConfig(ownerId: 102), allowActivate: false);
        var ended = new List<ContinuousEndReason>();
        runtime.OnEnded += (_, reason) => ended.Add(reason);

        runtime.Activate();

        Assert.Equal(ContinuousState.Aborted, runtime.State);
        Assert.True(runtime.IsTerminated);
        Assert.Single(ended, ContinuousEndReason.Interrupted);
    }

    [Fact]
    public void Manager_registers_activates_pauses_resumes_and_ends_continuous_runtime()
    {
        var manager = new DefaultContinuousManager();
        var binder = new RecordingBinder();
        manager.AddLifecycleBinder(binder);

        var runtime = new TestContinuous(new TestContinuousConfig(ownerId: 200));

        Assert.True(manager.Register(runtime));
        Assert.Equal(1, manager.TotalCount);
        Assert.Equal(0, manager.ActiveCount);

        Assert.True(manager.TryActivate(runtime));
        Assert.Equal(1, manager.ActiveCount);
        Assert.Equal(ContinuousState.Active, runtime.State);

        Assert.True(manager.TryPause(runtime));
        Assert.Equal(0, manager.ActiveCount);
        Assert.Equal(ContinuousState.Paused, runtime.State);

        Assert.True(manager.TryResume(runtime));
        Assert.Equal(1, manager.ActiveCount);
        Assert.Equal(ContinuousState.Active, runtime.State);

        Assert.True(manager.TryEnd(runtime, ContinuousEndReason.Completed));
        Assert.Equal(0, manager.ActiveCount);
        Assert.Equal(0, manager.TotalCount);
        Assert.Equal(ContinuousState.Expired, runtime.State);
        Assert.Equal("registered,activated,paused,resumed,ended:Completed,unregistered:Completed", string.Join(",", binder.Events));
    }

    [Fact]
    public void Manager_blocks_activation_when_owner_active_tags_conflict()
    {
        var manager = new DefaultContinuousManager();
        var blockingTags = new TestTagContainer("stun");
        manager.AddAdmissionPolicy(new BlockByOwnerActiveTagsPolicy(blockingTags));

        var first = new TestContinuous(new TestTaggedContinuousConfig(ownerId: 300, tags: new TestTagContainer("stun")));
        var second = new TestContinuous(new TestTaggedContinuousConfig(ownerId: 300, tags: new TestTagContainer("stun")));

        Assert.True(manager.Register(first));
        Assert.True(manager.TryActivate(first));
        Assert.True(manager.Register(second));

        Assert.False(manager.TryActivate(second));
        Assert.Equal("Blocked by active continuous tags", manager.LastRejectReason);
        Assert.Equal(1, manager.ActiveCount);
        Assert.Equal(2, manager.TotalCount);
    }

    private sealed class TestContinuous : MobaContinuousRuntimeBase
    {
        private readonly TestContinuousConfig _config;
        private readonly bool _allowActivate;

        public TestContinuous(TestContinuousConfig config, bool allowActivate = true)
        {
            _config = config;
            _allowActivate = allowActivate;
        }

        public override IContinuousConfig Config => _config;

        protected override bool OnActivating() => _allowActivate;

        public void Advance(float deltaTimeSeconds)
        {
            AdvanceElapsed(deltaTimeSeconds);
        }
    }

    private class TestContinuousConfig : IContinuousConfig
    {
        public TestContinuousConfig(long ownerId)
        {
            OwnerId = ownerId;
        }

        public string Id => $"continuous.{OwnerId}";
        public long OwnerId { get; }
        public bool CanBeInterrupted => true;
    }

    private sealed class TestTaggedContinuousConfig : TestContinuousConfig, ITagConfig
    {
        public TestTaggedContinuousConfig(long ownerId, ITagContainer tags)
            : base(ownerId)
        {
            Tags = tags;
        }

        public ITagContainer Tags { get; }
        public ITagContainer PauseByTags => TestTagContainer.Empty;
        public ITagContainer BlockByTags => TestTagContainer.Empty;
    }

    private sealed class TestTagContainer : ITagContainer
    {
        public static readonly TestTagContainer Empty = new TestTagContainer();
        private readonly HashSet<string> _tags = new(StringComparer.Ordinal);

        public TestTagContainer(params string[] tags)
        {
            if (tags != null)
            {
                for (var i = 0; i < tags.Length; i++)
                {
                    var tag = tags[i];
                    if (!string.IsNullOrWhiteSpace(tag))
                    {
                        _tags.Add(tag);
                    }
                }
            }
        }

        public bool HasTag(string tag) => _tags.Contains(tag);
        public bool HasAny(ITagContainer other)
        {
            if (other == null || other.Count == 0)
            {
                return false;
            }

            if (other is TestTagContainer testOther)
            {
                foreach (var tag in testOther._tags)
                {
                    if (_tags.Contains(tag))
                    {
                        return true;
                    }
                }

                return false;
            }

            return other.Count > 0 && _tags.Count > 0;
        }
        public int Count => _tags.Count;
    }

    private sealed class RecordingBinder : IContinuousLifecycleBinder
    {
        public readonly List<string> Events = new();

        public void OnRegistered(IContinuous continuous, IContinuousManager manager) => Events.Add("registered");
        public void OnActivated(IContinuous continuous, IContinuousManager manager) => Events.Add("activated");
        public void OnPaused(IContinuous continuous, IContinuousManager manager) => Events.Add("paused");
        public void OnResumed(IContinuous continuous, IContinuousManager manager) => Events.Add("resumed");
        public void OnEnded(IContinuous continuous, ContinuousEndReason reason, IContinuousManager manager) => Events.Add($"ended:{reason}");
        public void OnUnregistered(IContinuous continuous, ContinuousEndReason reason, IContinuousManager manager) => Events.Add($"unregistered:{reason}");
    }
}
