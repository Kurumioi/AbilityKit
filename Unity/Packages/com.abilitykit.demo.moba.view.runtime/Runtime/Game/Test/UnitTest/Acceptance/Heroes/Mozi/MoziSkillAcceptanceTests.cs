using System.Collections.Generic;
using System.Text;
using AbilityKit.Ability.Host;
using AbilityKit.Combat.Projectile;
using AbilityKit.Core.Mathematics;
using AbilityKit.Demo.Moba;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Services.Area;
using AbilityKit.Demo.Moba.Systems;
using AbilityKit.Game.Flow.Battle.ViewEvents;
using AbilityKit.Protocol.Moba;
using AbilityKit.Protocol.Moba.StateSync;
using UnityEngine;
using AbilityKit.Trace;
using AbilityKit.Triggering.Runtime.Config.Plans;
using NUnit.Framework;

namespace AbilityKit.Game.Test.UnitTest
{
    public sealed class MoziSkillAcceptanceTests : MobaAcceptanceTestBase
    {
        private static readonly HeroSkillContract Mozi = new HeroSkillContract(
            "Mozi",
            heroId: 1004,
            attributeTemplateId: 1004,
            skillIds: new[] { 10040101, 10040201, 10040301 });

        private static readonly HeroSkillSlotContract Skill1 = new HeroSkillSlotContract(1, 10040101, 10040101, 10040101);
        private static readonly HeroSkillSlotContract Skill2 = new HeroSkillSlotContract(2, 10040201, 10040201, 10040201);
        private static readonly HeroSkillSlotContract Skill3 = new HeroSkillSlotContract(3, 10040301, 10040301, 10040301);

        [Test]
        public void Skill10040101_ShouldApplyBuffModifierAndReplaceNextBasicAttackSkillId()
        {
            using (var harness = HeroSkillHeadlessContract.CreateHarness(Mozi, "mozi_skill_1_buff_modifier_enhanced_basic_contract_world"))
            {
                HeroSkillHeadlessContract.AssertTriggerActions(
                    harness,
                    10040101,
                    (int)TriggeringConstants.DashId.Value,
                    (int)TriggeringConstants.AddBuffId.Value,
                    (int)TriggeringConstants.AddShieldId.Value,
                    (int)TriggeringConstants.DebugLogId.Value);
                HeroSkillHeadlessContract.AssertTriggerActions(
                    harness,
                    10040112,
                    (int)TriggeringConstants.GiveDamageId.Value,
                    (int)TriggeringConstants.PullId.Value,
                    (int)TriggeringConstants.RemoveBuffId.Value,
                    (int)TriggeringConstants.DebugLogId.Value);
                harness.AssertSkillUsesCastFlow(10040112, 10040112);
                harness.AssertCastFlowContainsTimelineEffect(10040112, 10040112);

                var effectTrace = HeroSkillHeadlessContract.CastSlotAndAssertEffect(harness, Skill1, "mozi skill 1 buff modifier enhanced basic attack contract");
                var actorId = harness.AssertPlayerActorBound();
                var targetActorId = HeroSkillHeadlessContract.SpawnEnemyHero(harness, x: 3f);
                harness.AssertActionExecutedUnderEffect(effectTrace.RootId, (int)TriggeringConstants.DashId.Value, TriggeringConstants.Actions.Dash);
                harness.AssertActionExecutedUnderEffect(effectTrace.RootId, (int)TriggeringConstants.AddBuffId.Value, TriggeringConstants.Actions.AddBuff);
                HeroSkillHeadlessContract.AssertFreshBuff(harness, actorId, 10040101, 4.0f, "Mozi skill 1 should apply the enhanced basic attack modifier buff using BuffDTO duration.");
                TickUntilSkillStops(harness, actorId, Skill1.Slot, maxTicks: 120);

                const int basicAttackSlot = 4;
                const int normalBasicAttackSkillId = 10040011;
                var modifiers = harness.World.Services.Resolve<MobaSkillParamModifierService>();
                harness.AssertSlotSkill(basicAttackSlot, normalBasicAttackSkillId);
                Assert.AreEqual(10040112, modifiers.Skill.ResolveSkillId(actorId, normalBasicAttackSkillId), "Mozi skill 1 buff should override normal basic attack skill id to the enhanced basic attack.");

                var skills = harness.World.Services.Resolve<SkillCastCoordinator>();
                var cast = skills.TryCastSkill(actorId, normalBasicAttackSkillId, slot: basicAttackSlot, aimPos: default, aimDir: new Vec3(1f, 0f, 0f), targetActorId: targetActorId);
                Assert.IsTrue(cast.Success, $"Mozi enhanced basic attack should cast through ResolveSkillId. failReason={cast.FailReason}");
                harness.TickUntilTraceNode(MobaTraceKind.SkillCast, 10040112, maxTicks: 10, message: "Mozi normal basic attack should be replaced by skill 10040112 while the skill 1 buff is active.");
                var enhancedTrace = harness.TickUntilTraceNode(MobaTraceKind.EffectExecution, 10040112, maxTicks: harness.CalculateWaitTicksForSkillEffect(10040112, 10040112, safetyFrames: 5) + 30, message: "Mozi enhanced basic attack should execute effect 10040112 after skill id replacement.");
                harness.AssertActionExecutedUnderEffect(enhancedTrace.RootId, (int)TriggeringConstants.GiveDamageId.Value, TriggeringConstants.Actions.GiveDamage);
                harness.AssertActionExecutedUnderEffect(enhancedTrace.RootId, (int)TriggeringConstants.PullId.Value, TriggeringConstants.Actions.Pull);
                harness.AssertActionExecutedUnderEffect(enhancedTrace.RootId, (int)TriggeringConstants.RemoveBuffId.Value, TriggeringConstants.Actions.RemoveBuff);
                Assert.GreaterOrEqual(CountTraceNodesInRoot(harness, enhancedTrace.RootId, MobaTraceKind.DamageApply, 10040112), 1, "Mozi enhanced melee basic attack should apply direct damage instead of launching the skill 2 cannon projectile.");

                harness.Tick(1);
                Assert.IsFalse(harness.HasActorBuff(actorId, 10040101), "Mozi enhanced basic attack should consume the skill 1 modifier buff.");
                Assert.AreEqual(normalBasicAttackSkillId, modifiers.Skill.ResolveSkillId(actorId, normalBasicAttackSkillId), "Mozi normal basic attack skill id should recover after the modifier buff is removed.");
            }
        }

        [Test]
        public void Skill10040201_ShouldLaunchCannonAndSpawnEndpointCrater()
        {
            using (var harness = HeroSkillHeadlessContract.CreateHarness(Mozi, "mozi_skill_2_endpoint_crater_contract_world"))
            {
                AssertMoziSkill2ControlChain(harness);

                var effectTrace = HeroSkillHeadlessContract.CastSlotAndAssertEffect(harness, Skill2, "mozi skill 2 endpoint crater contract");
                harness.AssertActionExecutedUnderEffect(effectTrace.RootId, (int)TriggeringConstants.ShootProjectileId.Value, TriggeringConstants.Actions.ShootProjectile);
                harness.AssertProjectileLaunchedUnderEffect(effectTrace.RootId, 31040201, 30040201);
                var spawn = TickUntilProjectileSpawnSnapshot(harness, 30040201, maxTicks: 30);
                AssertMoziSkill2ProjectileMovesAndResolvesVfx(harness, spawn);
                var projectileActorId = spawn.ProjectileActorId;
                var exit = TickUntilProjectileExitSnapshot(harness, spawn.ProjectileId, maxTicks: 80);
                Assert.AreEqual(projectileActorId, exit.ProjectileActorId, $"Mozi skill 2 endpoint exit should reference the same scene projectile actor as spawn. spawnActorId={projectileActorId}, exitActorId={exit.ProjectileActorId}, projectileId={spawn.ProjectileId}");
                Assert.AreEqual((int)ProjectileExitReason.MaxDistance, exit.ExitReason, $"Mozi skill 2 endpoint crater contract should end by max distance. actualReason={exit.ExitReason}");
                AssertMoziSkill2ProjectileActorDespawned(harness, projectileActorId);
                TickUntilCraterAreaSpawn(harness, effectTrace.RootId, maxTicks: 10, message: "Mozi skill 2 projectile should spawn the crater area when it reaches its endpoint.");
            }
        }

        [Test]
        public void Skill10040201_ShouldSpawnHitCraterAndDamageEveryHalfSecond()
        {
            using (var harness = HeroSkillHeadlessContract.CreateHarness(Mozi, "mozi_skill_2_hit_crater_interval_contract_world"))
            {
                AssertMoziSkill2ControlChain(harness);

                harness.EnterGameAndWarmup(reason: "mozi skill 2 hit crater interval contract");
                harness.AssertSlotSkill(Skill2.Slot, Skill2.SkillId);
                var actorId = harness.AssertPlayerActorBound();
                var targetActorId = HeroSkillHeadlessContract.SpawnEnemyHero(harness, x: 3f);

                var skills = harness.World.Services.Resolve<SkillCastCoordinator>();
                var cast = skills.TryCastBySlot(actorId, Skill2.Slot, aimPos: default, aimDir: new Vec3(1f, 0f, 0f), targetActorId: 0);
                Assert.IsTrue(cast.Success, $"Mozi skill 2 should cast toward the enemy and allow the cannon to hit early. failReason={cast.FailReason}");

                var effectTrace = harness.TickUntilTraceNode(MobaTraceKind.EffectExecution, Skill2.EffectId, maxTicks: 20, message: "Mozi skill 2 should execute its cannon effect.");
                harness.AssertProjectileLaunchedUnderEffect(effectTrace.RootId, 31040201, 30040201);
                var spawn = TickUntilProjectileSpawnSnapshot(harness, 30040201, maxTicks: 30);
                var projectileActorId = spawn.ProjectileActorId;
                var exit = TickUntilProjectileExitSnapshot(harness, spawn.ProjectileId, maxTicks: 30);
                Assert.AreEqual(projectileActorId, exit.ProjectileActorId, $"Mozi skill 2 hit exit should reference the same scene projectile actor as spawn. spawnActorId={projectileActorId}, exitActorId={exit.ProjectileActorId}, projectileId={spawn.ProjectileId}");
                Assert.AreEqual((int)ProjectileExitReason.Hit, exit.ExitReason, $"Mozi skill 2 hit crater contract should end by hit. actualReason={exit.ExitReason}");
                AssertMoziSkill2ProjectileActorDespawned(harness, projectileActorId);
                TickUntilCraterAreaSpawn(harness, effectTrace.RootId, maxTicks: 10, message: "Mozi skill 2 projectile hit should spawn the crater area at the hit position.");
                AssertMoziSkill2HitCraterPosition(harness, effectTrace.RootId, targetActorId, exit);

                harness.TickMilliseconds(550);
                var damageCount = CountTraceNodesInRoot(harness, effectTrace.RootId, MobaTraceKind.DamageApply, 10040201);
                Assert.GreaterOrEqual(damageCount, 2, $"Mozi skill 2 crater should deal initial damage and at least one 0.5s interval damage tick. actual={damageCount}");
            }
        }

        [Test]
        public void Skill10040301_ShouldSpawnBarrierApplyChannelBuffAndCompileControlTicks()
        {
            using (var harness = HeroSkillHeadlessContract.CreateHarness(Mozi, "mozi_skill_3_barrier_contract_world"))
            {
                HeroSkillHeadlessContract.AssertTriggerActions(
                    harness,
                    10040301,
                    (int)TriggeringConstants.SpawnAreaId.Value,
                    (int)TriggeringConstants.AddBuffId.Value,
                    (int)TriggeringConstants.AddShieldId.Value,
                    (int)TriggeringConstants.DebugLogId.Value);
                HeroSkillHeadlessContract.AssertTriggerActions(
                    harness,
                    10040311,
                    (int)TriggeringConstants.GiveDamageId.Value,
                    (int)TriggeringConstants.AddBuffId.Value,
                    (int)TriggeringConstants.DebugLogId.Value);
                HeroSkillHeadlessContract.AssertTriggerActions(
                    harness,
                    10040321,
                    (int)TriggeringConstants.AddBuffId.Value,
                    (int)TriggeringConstants.DebugLogId.Value);

                var effectTrace = HeroSkillHeadlessContract.CastSlotAndAssertEffect(harness, Skill3, "mozi skill 3 barrier contract");
                var actorId = harness.AssertPlayerActorBound();
                harness.AssertActionExecutedUnderEffect(effectTrace.RootId, (int)TriggeringConstants.SpawnAreaId.Value, TriggeringConstants.Actions.SpawnArea);
                harness.AssertActionExecutedUnderEffect(effectTrace.RootId, (int)TriggeringConstants.AddBuffId.Value, TriggeringConstants.Actions.AddBuff);
                harness.AssertAreaSpawnedUnderEffect(effectTrace.RootId, 40040301);
                HeroSkillHeadlessContract.AssertFreshBuff(harness, actorId, 10040003, 3.0f, "Mozi skill 3 should apply the channel/barrier state buff.");
            }
        }

        [Test]
        public void Skill10040000_PassiveCastCompleteShouldApplyShieldInsideFormalEffectScope()
        {
            using (var harness = HeroSkillHeadlessContract.CreateHarness(Mozi, "mozi_passive_cast_complete_effect_scope_contract_world"))
            {
                Assert.IsTrue(harness.TriggerPlans.TryGetRecordByTriggerId(10040000, out var castPassive), "Mozi cast-complete passive trigger should exist.");
                Assert.AreEqual(TriggerPlanScope.OwnerBound, castPassive.Scope, "Mozi cast-complete passive must not be registered globally.");
                Assert.IsTrue(harness.TriggerPlans.TryGetRecordByTriggerId(10040001, out var attackCounter), "Mozi basic-attack counter trigger should exist.");
                Assert.AreEqual(TriggerPlanScope.OwnerBound, attackCounter.Scope, "Mozi basic-attack counter must remain bound to its passive owner.");

                HeroSkillHeadlessContract.AssertTriggerActions(
                    harness,
                    10040000,
                    (int)TriggeringConstants.AddBuffId.Value,
                    (int)TriggeringConstants.AddShieldId.Value,
                    (int)TriggeringConstants.DebugLogId.Value);

                harness.EnterGameAndWarmup(reason: "mozi passive cast-complete formal effect scope contract");
                harness.AssertSlotSkill(Skill1.Slot, Skill1.SkillId);
                var actorId = harness.AssertPlayerActorBound();
                Assert.IsFalse(harness.HasActorBuff(actorId, 10040000), "Mozi passive shield buff should not exist before casting.");

                var skills = harness.World.Services.Resolve<SkillCastCoordinator>();
                var cast = skills.TryCastBySlot(actorId, Skill1.Slot, aimPos: default, aimDir: new Vec3(1f, 0f, 0f), targetActorId: 0);
                Assert.IsTrue(cast.Success, $"Mozi skill 1 should cast successfully. failReason={cast.FailReason}");

                harness.TickUntilTraceNode(MobaTraceKind.SkillCast, Skill1.SkillId, maxTicks: 10, message: "Mozi skill 1 should publish its cast trace.");
                var passiveTrace = harness.TickUntilTraceNode(MobaTraceKind.EffectExecution, 10040000, maxTicks: 120, message: "Mozi cast-complete passive should execute through the formal owner-bound effect gateway.");
                harness.AssertActionExecutedUnderEffect(passiveTrace.RootId, (int)TriggeringConstants.AddBuffId.Value, TriggeringConstants.Actions.AddBuff);
                harness.AssertActionExecutedUnderEffect(passiveTrace.RootId, (int)TriggeringConstants.AddShieldId.Value, TriggeringConstants.Actions.AddShield);
                Assert.IsTrue(harness.HasActorBuff(actorId, 10040000), "Mozi cast-complete passive should apply its shield buff without an unscoped plan-action exception.");
            }
        }

        [Test]
        public void Skill10040000_PassiveFourthBasicAttackShouldDealMeleeDamageKnockbackAndRefreshShieldBuff()
        {
            using (var harness = HeroSkillHeadlessContract.CreateHarness(Mozi, "mozi_passive_fourth_basic_contract_world"))
            {
                HeroSkillHeadlessContract.AssertTriggerActions(
                    harness,
                    10040001,
                    (int)TriggeringConstants.AdvanceGameplayCounterId.Value);
                HeroSkillHeadlessContract.AssertTriggerActions(
                    harness,
                    10040002,
                    (int)TriggeringConstants.GiveDamageId.Value,
                    (int)TriggeringConstants.PullId.Value,
                    (int)TriggeringConstants.AddBuffId.Value,
                    (int)TriggeringConstants.DebugLogId.Value);

                harness.EnterGameAndWarmup(reason: "mozi passive fourth basic attack contract");
                var actorId = harness.AssertPlayerActorBound();
                var targetActorId = HeroSkillHeadlessContract.SpawnEnemyHero(harness, x: 3f);

                for (var i = 0; i < 4; i++)
                {
                    var damage = HeroSkillHeadlessContract.ExecuteBasicAttackDamage(harness, actorId, targetActorId, baseDamage: 2f);
                    Assert.IsNotNull(damage, "Mozi passive setup should apply real basic attack damage.");
                    Assert.Greater(damage.Value, 0f, "Mozi passive setup damage should be positive.");
                    harness.Tick(1);
                }

                var passiveTrace = harness.TickUntilTraceNode(MobaTraceKind.EffectExecution, 10040002, maxTicks: 10, message: "Mozi passive fourth basic attack should execute the enhanced melee hit trigger.");
                harness.AssertActionExecutedUnderEffect(passiveTrace.RootId, (int)TriggeringConstants.GiveDamageId.Value, TriggeringConstants.Actions.GiveDamage);
                harness.AssertActionExecutedUnderEffect(passiveTrace.RootId, (int)TriggeringConstants.PullId.Value, TriggeringConstants.Actions.Pull);
                harness.AssertActionExecutedUnderEffect(passiveTrace.RootId, (int)TriggeringConstants.AddBuffId.Value, TriggeringConstants.Actions.AddBuff);
                Assert.GreaterOrEqual(CountTraceNodesInRoot(harness, passiveTrace.RootId, MobaTraceKind.DamageApply, 10040002), 1, "Mozi passive fourth basic attack should apply melee damage instead of launching the skill 2 cannon projectile.");
                HeroSkillHeadlessContract.AssertFreshBuff(harness, actorId, 10040000, 2.0f, "Mozi passive fourth basic attack should refresh the passive shield buff.");
            }
        }
        private static void AssertMoziSkill2ControlChain(MobaSkillConfigTestHarness harness)
        {
            HeroSkillHeadlessContract.AssertTriggerActions(
                harness,
                10040201,
                (int)TriggeringConstants.ShootProjectileId.Value,
                (int)TriggeringConstants.DebugLogId.Value);
            HeroSkillHeadlessContract.AssertTriggerActions(
                harness,
                10040211,
                (int)TriggeringConstants.SpawnAreaId.Value,
                (int)TriggeringConstants.DebugLogId.Value);
            HeroSkillHeadlessContract.AssertTriggerActions(
                harness,
                10040212,
                (int)TriggeringConstants.GiveDamageId.Value,
                (int)TriggeringConstants.AddBuffId.Value,
                (int)TriggeringConstants.DebugLogId.Value);
            harness.AssertProjectileConfigExists(31040201, 30040201);
            Assert.IsTrue(harness.Config.TryGetProjectile(30040201, out var projectile), "Mozi skill 2 projectile config should exist.");
            CollectionAssert.Contains(projectile.OnExitTriggerIds, 10040211, "Mozi skill 2 projectile should create the crater from its exit trigger so both hit and endpoint exits are covered.");
            Assert.IsTrue(harness.Config.TryGetAoe(40040201, out var crater), "Mozi skill 2 crater area config should exist.");
            Assert.AreEqual(90004004, crater.VfxId, "Mozi skill 2 crater should use the configured crater VFX prefab.");
            Assert.AreEqual(500, crater.IntervalMs, "Mozi skill 2 crater should tick every 0.5 seconds.");
            CollectionAssert.Contains(crater.OnIntervalTriggerIds, 10040212, "Mozi skill 2 crater should apply interval damage through trigger 10040212.");
        }

        private static void AssertMoziSkill2ProjectileActorDespawned(MobaSkillConfigTestHarness harness, int projectileActorId)
        {
            Assert.Greater(projectileActorId, 0, "Mozi skill 2 projectile spawn should expose the scene projectile actor id before cleanup can be verified.");
            harness.Tick(1);
            var exists = harness.TryGetActorEntity(projectileActorId, out var entity);
            Assert.IsFalse(exists, $"Mozi skill 2 projectile actor should despawn after projectile exit so the scene cannon does not remain. actorId={projectileActorId}, hasActorId={entity != null && entity.hasActorId}, hasTransform={entity != null && entity.hasTransform}, hasDespawnRequest={entity != null && entity.hasActorDespawnRequest}, isFlyingProjectileTag={entity != null && entity.isFlyingProjectileTag}");
        }

        private static MobaProjectileEventSnapshotEntry TickUntilProjectileSpawnSnapshot(MobaSkillConfigTestHarness harness, int templateId, int maxTicks)
        {
            for (var i = 0; i <= maxTicks; i++)
            {
                if (TryCollectProjectileSpawnSnapshot(harness, templateId, out var entry))
                {
                    return entry;
                }

                if (i < maxTicks) harness.Tick(1);
            }

            Assert.Fail($"Projectile spawn snapshot missing for template {templateId} within {maxTicks} ticks.");
            return default;
        }

        private static bool TryCollectProjectileSpawnSnapshot(MobaSkillConfigTestHarness harness, int templateId, out MobaProjectileEventSnapshotEntry entry)
        {
            entry = default;
            var provider = harness.World.Services.Resolve<IWorldStateSnapshotBatchProvider>();
            var snapshots = new List<WorldStateSnapshot>(16);
            provider.CollectSnapshots(harness.FrameTime.Frame, snapshots, 32);

            for (var i = 0; i < snapshots.Count; i++)
            {
                if (snapshots[i].OpCode != MobaOpCodes.Snapshot.ProjectileEvent) continue;

                var entries = MobaProjectileEventSnapshotCodec.Deserialize(snapshots[i].Payload);
                for (var j = 0; j < entries.Length; j++)
                {
                    if (entries[j].Kind != (int)ProjectileEventKind.Spawn) continue;
                    if (entries[j].TemplateId != templateId) continue;

                    entry = entries[j];
                    return true;
                }
            }

            return false;
        }

        private static MobaProjectileEventSnapshotEntry TickUntilProjectileExitSnapshot(MobaSkillConfigTestHarness harness, int projectileId, int maxTicks)
        {
            for (var i = 0; i <= maxTicks; i++)
            {
                if (TryCollectProjectileExitSnapshot(harness, projectileId, out var entry))
                {
                    return entry;
                }

                if (i < maxTicks) harness.Tick(1);
            }

            Assert.Fail($"Projectile exit snapshot missing for projectile {projectileId} within {maxTicks} ticks.");
            return default;
        }

        private static bool TryCollectProjectileExitSnapshot(MobaSkillConfigTestHarness harness, int projectileId, out MobaProjectileEventSnapshotEntry entry)
        {
            entry = default;
            var provider = harness.World.Services.Resolve<IWorldStateSnapshotBatchProvider>();
            var snapshots = new List<WorldStateSnapshot>(16);
            provider.CollectSnapshots(harness.FrameTime.Frame, snapshots, 32);

            for (var i = 0; i < snapshots.Count; i++)
            {
                if (snapshots[i].OpCode != MobaOpCodes.Snapshot.ProjectileEvent) continue;

                var entries = MobaProjectileEventSnapshotCodec.Deserialize(snapshots[i].Payload);
                for (var j = 0; j < entries.Length; j++)
                {
                    if (entries[j].Kind != (int)ProjectileEventKind.Exit) continue;
                    if (entries[j].ProjectileId != projectileId) continue;

                    entry = entries[j];
                    return true;
                }
            }

            return false;
        }

        private static void AssertMoziSkill2ProjectileMovesAndResolvesVfx(MobaSkillConfigTestHarness harness, MobaProjectileEventSnapshotEntry spawn)
        {
            Assert.AreEqual((int)ProjectileEventKind.Spawn, spawn.Kind, "Mozi skill 2 should emit a projectile spawn snapshot for the cannon.");
            Assert.Greater(spawn.LauncherActorId, 0, "Mozi skill 2 cannon spawn snapshot should keep the launcher actor for follow/position resolution.");
            Assert.Greater(spawn.ProjectileActorId, 0, "Mozi skill 2 cannon spawn snapshot should expose the moving projectile actor for scene VFX follow binding.");
            Assert.AreEqual(1f, spawn.ForwardX, 0.0001f, "Mozi skill 2 cannon spawn snapshot should carry launch forward for VFX orientation.");
            Assert.AreEqual(0f, spawn.ForwardY, 0.0001f, "Mozi skill 2 cannon spawn snapshot should stay on the XZ plane.");
            Assert.AreEqual(0f, spawn.ForwardZ, 0.0001f, "Mozi skill 2 cannon spawn snapshot should follow the skill aim direction.");

            var projectileActor = harness.AssertActorEntity(spawn.ProjectileActorId);
            Assert.IsTrue(projectileActor.hasTransform, "Mozi skill 2 cannon projectile actor should have a transform for snapshot-driven movement.");
            var initialPosition = projectileActor.transform.Value.Position;

            var moved = TickUntilActorPositionXGreaterThan(harness, spawn.ProjectileActorId, initialPosition.X + 0.05f, maxTicks: 10);
            Assert.Greater(moved.X, initialPosition.X + 0.05f, $"Mozi skill 2 cannon projectile actor should move after projectile ticks. initialX={initialPosition.X:F3}, movedX={moved.X:F3}");
            Assert.IsTrue(TryCollectActorTransformSnapshot(harness, spawn.ProjectileActorId, out var transformEntry), "Mozi skill 2 moving projectile actor should be included in actor transform snapshots for the client view.");
            Assert.Greater(transformEntry.X, initialPosition.X + 0.05f, $"Mozi skill 2 projectile transform snapshot should carry the moved projectile position. initialX={initialPosition.X:F3}, snapshotX={transformEntry.X:F3}");

            var resolver = new BattleProjectileVfxResolver();
            Assert.AreEqual(90004002, resolver.ResolveSnapshotVfxId(spawn.TemplateId, spawn.Kind), "Mozi skill 2 projectile spawn snapshot should resolve to the configured cannon VFX instead of a placeholder.");
        }

        private static bool TryCollectActorTransformSnapshot(MobaSkillConfigTestHarness harness, int actorId, out MobaActorTransformSnapshotEntry entry)
        {
            entry = default;
            var provider = harness.World.Services.Resolve<IWorldStateSnapshotBatchProvider>();
            var snapshots = new List<WorldStateSnapshot>(16);
            provider.CollectSnapshots(harness.FrameTime.Frame, snapshots, 32);

            for (var i = 0; i < snapshots.Count; i++)
            {
                if (snapshots[i].OpCode != MobaOpCodes.Snapshot.ActorTransform) continue;

                var entries = MobaActorTransformSnapshotCodec.Deserialize(snapshots[i].Payload);
                for (var j = 0; j < entries.Length; j++)
                {
                    if (entries[j].ActorId != actorId) continue;

                    entry = entries[j];
                    return true;
                }
            }

            return false;
        }

        private static Vec3 TickUntilActorPositionXGreaterThan(MobaSkillConfigTestHarness harness, int actorId, float minX, int maxTicks)
        {
            for (var i = 0; i <= maxTicks; i++)
            {
                var actor = harness.AssertActorEntity(actorId);
                Assert.IsTrue(actor.hasTransform, "Actor should keep a transform while waiting for projectile movement.");
                var position = actor.transform.Value.Position;
                if (position.X > minX)
                {
                    return position;
                }

                if (i < maxTicks) harness.Tick(1);
            }

            var finalActor = harness.AssertActorEntity(actorId);
            return finalActor.transform.Value.Position;
        }

        private static TraceSnapshot<MobaTraceMetadata> TickUntilCraterAreaSpawn(MobaSkillConfigTestHarness harness, long rootId, int maxTicks, string message)
        {
            for (var i = 0; i <= maxTicks; i++)
            {
                foreach (var node in harness.Trace.GetNodesByRoot(rootId))
                {
                    if (node.Kind == (int)MobaTraceKind.AreaSpawn && node.Metadata != null && node.Metadata.ConfigId == 40040201)
                    {
                        return node;
                    }
                }

                if (i < maxTicks) harness.Tick(1);
            }

            Assert.Fail($"{message}\n{DescribeMoziSkill2CraterTraceState(harness, rootId)}");
            return default;
        }

        private static void AssertMoziSkill2HitCraterPosition(
            MobaSkillConfigTestHarness harness,
            long rootId,
            int targetActorId,
            MobaProjectileEventSnapshotEntry exit)
        {
            var areas = harness.World.Services.Resolve<MobaAreaRuntimeService>();
            var craterAreas = new List<MobaAreaRuntimeInfo>(2);
            Assert.IsTrue(
                areas.TryGetAreas(craterAreas, templateId: 40040201),
                "Mozi skill 2 hit exit should leave an active crater area.");

            MobaAreaRuntimeInfo crater = default;
            var found = false;
            for (var i = 0; i < craterAreas.Count; i++)
            {
                if (craterAreas[i].RootContextId != rootId) continue;
                crater = craterAreas[i];
                found = true;
                break;
            }

            Assert.IsTrue(found, $"Mozi skill 2 hit crater should belong to the cast trace root. root={rootId}, candidates={craterAreas.Count}");
            Assert.AreEqual(exit.X, crater.Center.X, 0.001f, "Mozi skill 2 crater X should match the projectile hit exit point.");
            Assert.AreEqual(exit.Z, crater.Center.Z, 0.001f, "Mozi skill 2 crater Z should match the projectile hit exit point.");

            var target = harness.AssertActorEntity(targetActorId);
            Assert.IsTrue(target.hasTransform, "Mozi skill 2 hit target should retain its logical transform while the crater is active.");
            var targetPosition = target.transform.Value.Position;
            var dx = targetPosition.X - crater.Center.X;
            var dz = targetPosition.Z - crater.Center.Z;
            Assert.LessOrEqual(
                dx * dx + dz * dz,
                crater.Radius * crater.Radius + 0.001f,
                $"Mozi skill 2 hit target should be inside the crater. target=({targetPosition.X:0.###},{targetPosition.Z:0.###}), center=({crater.Center.X:0.###},{crater.Center.Z:0.###}), radius={crater.Radius:0.###}");
        }

        private static int CountTraceNodesInRoot(MobaSkillConfigTestHarness harness, long rootId, MobaTraceKind kind, int configId)
        {
            var count = 0;
            foreach (var node in harness.Trace.GetNodesByRoot(rootId))
            {
                if (node.Kind == (int)kind && node.Metadata != null && node.Metadata.ConfigId == configId)
                {
                    count++;
                }
            }

            return count;
        }

        private static string DescribeMoziSkill2CraterTraceState(MobaSkillConfigTestHarness harness, long expectedRootId)
        {
            var sb = new StringBuilder(512);
            sb.Append("expectedRoot=").Append(expectedRootId);
            AppendTraceNodes(sb, harness, MobaTraceKind.EffectExecution, 10040211, "craterTriggerEffects");
            AppendTraceNodes(sb, harness, MobaTraceKind.AreaSpawn, 40040201, "craterAreaSpawns");
            AppendTraceNodes(sb, harness, MobaTraceKind.ProjectileLaunch, 30040201, "projectileLaunches");
            AppendTraceNodes(sb, harness, MobaTraceKind.EffectAction, (int)TriggeringConstants.SpawnAreaId.Value, "spawnAreaActions");
            return sb.ToString();
        }

        private static void AppendTraceNodes(StringBuilder sb, MobaSkillConfigTestHarness harness, MobaTraceKind kind, int configId, string label)
        {
            sb.Append("; ").Append(label).Append("=");
            var count = 0;
            foreach (var node in harness.Trace.GetNodesByKind((int)kind))
            {
                if (node.Metadata == null || node.Metadata.ConfigId != configId) continue;
                if (count > 0) sb.Append('|');
                sb.Append("ctx:").Append(node.ContextId)
                    .Append(",root:").Append(node.RootId)
                    .Append(",parent:").Append(node.ParentId)
                    .Append(",kind:").Append(node.Kind)
                    .Append(",config:").Append(node.Metadata.ConfigId)
                    .Append(",src:").Append(node.Metadata.SourceActorId)
                    .Append(",target:").Append(node.Metadata.TargetActorId);
                count++;
            }

            if (count == 0) sb.Append("<none>");
        }

        private static void TickUntilSkillStops(MobaSkillConfigTestHarness harness, int actorId, int slot, int maxTicks)
        {
            for (var i = 0; i < maxTicks; i++)
            {
                if (!harness.TryGetRunningSkillSnapshot(actorId, slot, out _)) return;
                harness.Tick(1);
            }

            Assert.Fail($"Skill slot {slot} should stop within {maxTicks} ticks. {harness.DescribeSkillRuntimeState(actorId, slot)}");
        }
    }
}
