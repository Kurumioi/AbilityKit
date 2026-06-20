using System;
using System.Collections.Generic;
using System.Linq;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Demo.Moba.Components;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Services.Passive;
using AbilityKit.Demo.Moba.Share.Config;
using Xunit;

namespace AbilityKit.Demo.Moba.Tests.Passive;

public sealed class MobaPassiveSkillLifecycleServiceTests
{
    [Fact]
    public void Sync_actor_passives_keeps_multiple_passives_independent_and_dedupes_duplicate_ids()
    {
        using var trace = new MobaTraceRegistry();
        var service = new MobaPassiveSkillLifecycleService(CreateConfigDatabase(), trace);
        var actor = CreateActor(
            new PassiveSkillRuntime { PassiveSkillId = 101, Level = 1 },
            new PassiveSkillRuntime { PassiveSkillId = 102, Level = 1 },
            new PassiveSkillRuntime { PassiveSkillId = 101, Level = 2 });

        service.SyncActorPassives(actor, frame: 1);

        Assert.True(actor.hasPassiveSkillTriggerListeners);
        Assert.Equal(new[] { 101, 102 }, actor.passiveSkillTriggerListeners.Active.Select(x => x.PassiveSkillId).OrderBy(x => x));
        Assert.True(actor.hasOngoingTriggerPlans);
        Assert.Equal(2, actor.ongoingTriggerPlans.Active.Count);
        Assert.All(actor.ongoingTriggerPlans.Active, entry => Assert.True(service.IsPassiveOwnerKey(entry.OwnerKey)));
        Assert.Contains(actor.ongoingTriggerPlans.Active, entry => entry.TriggerIds.SequenceEqual(new[] { 1001 }));
        Assert.Contains(actor.ongoingTriggerPlans.Active, entry => entry.TriggerIds.SequenceEqual(new[] { 1002 }));
    }

    [Fact]
    public void Complete_owner_bound_trigger_starts_cooldown_and_blocks_until_time_passes()
    {
        var time = new TestFrameTime { TimeSeconds = 1.5f };
        using var trace = new MobaTraceRegistry();
        var service = new MobaPassiveSkillLifecycleService(CreateConfigDatabase(), trace, frameTime: time);
        var runtime = new PassiveSkillRuntime { PassiveSkillId = 101, Level = 1 };
        var actor = CreateActor(runtime);

        service.SyncActorPassives(actor, frame: 1);
        var entry = Assert.Single(actor.ongoingTriggerPlans.Active);

        Assert.True(service.CanExecuteOwnerBoundTrigger(entry.OwnerKey, 1001));

        service.CompleteOwnerBoundTrigger(entry.OwnerKey, 1001);

        Assert.Equal(2500L, runtime.CooldownEndTimeMs);
        Assert.False(service.CanExecuteOwnerBoundTrigger(entry.OwnerKey, 1001));

        time.TimeSeconds = 2.5f;

        Assert.True(service.CanExecuteOwnerBoundTrigger(entry.OwnerKey, 1001));
    }

    [Fact]
    public void Removing_one_passive_keeps_other_passive_owner_binding_active()
    {
        using var trace = new MobaTraceRegistry();
        var service = new MobaPassiveSkillLifecycleService(CreateConfigDatabase(), trace);
        var first = new PassiveSkillRuntime { PassiveSkillId = 101, Level = 1 };
        var second = new PassiveSkillRuntime { PassiveSkillId = 102, Level = 1 };
        var actor = CreateActor(first, second);

        service.SyncActorPassives(actor, frame: 1);
        var firstOwnerKey = actor.ongoingTriggerPlans.Active.Single(entry => entry.TriggerIds.Contains(1001)).OwnerKey;
        var secondOwnerKey = actor.ongoingTriggerPlans.Active.Single(entry => entry.TriggerIds.Contains(1002)).OwnerKey;

        actor.ReplaceSkillLoadout(Array.Empty<ActiveSkillRuntime>(), new[] { second });
        service.SyncActorPassives(actor, frame: 2);

        Assert.False(service.IsPassiveOwnerKey(firstOwnerKey));
        Assert.True(service.IsPassiveOwnerKey(secondOwnerKey));
        var remaining = Assert.Single(actor.ongoingTriggerPlans.Active);
        Assert.Equal(secondOwnerKey, remaining.OwnerKey);
        Assert.Equal(new[] { 1002 }, remaining.TriggerIds);
    }

    private static ActorEntity CreateActor(params PassiveSkillRuntime[] passiveSkills)
    {
        var context = new ActorContext();
        var actor = context.CreateEntity();
        actor.AddActorId(1);
        actor.AddSkillLoadout(Array.Empty<ActiveSkillRuntime>(), passiveSkills);
        return actor;
    }

    private static MobaConfigDatabase CreateConfigDatabase()
    {
        var configs = new MobaConfigDatabase();
        var result = configs.ReloadFromDtoArrays(
            new Dictionary<Type, Array>
            {
                [typeof(PassiveSkillDTO)] = new[]
                {
                    new PassiveSkillDTO { Id = 101, Name = "passive_a", CooldownMs = 1000, TriggerIds = new[] { 1001 } },
                    new PassiveSkillDTO { Id = 102, Name = "passive_b", CooldownMs = 500, TriggerIds = new[] { 1002 } },
                },
            },
            strict: false);

        Assert.True(result.Succeeded, result.Error);
        return configs;
    }

    private sealed class TestFrameTime : IFrameTime
    {
        public FrameIndex Frame => new FrameIndex(0);
        public float DeltaTime => 0f;
        public float Time => TimeSeconds;
        public float TimeSeconds { get; set; }
        public float FrameToTime(FrameIndex frame) => 0f;
        public FrameIndex TimeToFrame(float time) => new FrameIndex(0);
    }
}
