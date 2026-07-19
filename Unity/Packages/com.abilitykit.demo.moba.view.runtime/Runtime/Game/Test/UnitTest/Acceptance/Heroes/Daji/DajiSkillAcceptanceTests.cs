using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Combat.Collision;
using AbilityKit.Combat.Projectile;
using AbilityKit.Core.Mathematics;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Systems;
using AbilityKit.Protocol.Moba;
using AbilityKit.Protocol.Moba.StateSync;
using AbilityKit.Trace;
using NUnit.Framework;

namespace AbilityKit.Game.Test.UnitTest
{
    public sealed class DajiSkillAcceptanceTests : MobaAcceptanceTestBase
    {
        private static readonly HeroSkillContract Daji = new HeroSkillContract(
            "Daji",
            heroId: 1005,
            attributeTemplateId: 1005,
            skillIds: new[] { 10050101, 10050201, 10050301 });

        private static readonly HeroSkillSlotContract Skill1 = new HeroSkillSlotContract(1, 10050101, 10050101, 10050101);
        private static readonly HeroSkillSlotContract Skill2 = new HeroSkillSlotContract(2, 10050201, 10050201, 10050201);
        private static readonly HeroSkillSlotContract Skill3 = new HeroSkillSlotContract(3, 10050301, 10050301, 10050301);

        [Test]
        public void Skill10050101_ShouldLaunchRectangularWaveAndDamageOffsetTarget()
        {
            using (var harness = HeroSkillHeadlessContract.CreateHarness(Daji, "daji_skill_1_rectangular_wave_contract_world"))
            {
                HeroSkillHeadlessContract.AssertTriggerActions(
                    harness,
                    10050101,
                    (int)TriggeringConstants.ShootProjectileId.Value,
                    (int)TriggeringConstants.DebugLogId.Value);
                HeroSkillHeadlessContract.AssertTriggerActions(
                    harness,
                    10050111,
                    (int)TriggeringConstants.GiveDamageId.Value,
                    (int)TriggeringConstants.AddBuffId.Value);
                harness.AssertProjectileConfigExists(31050101, 30050101);
                Assert.IsTrue(harness.Config.TryGetProjectile(30050101, out var projectile), "Daji skill 1 projectile config should exist.");
                Assert.AreEqual(2f, projectile.CollisionWidth, 0.001f, "Daji skill 1 should use the configured rectangular width.");
                Assert.AreEqual(1.5f, projectile.CollisionHeight, 0.001f, "Daji skill 1 should use the configured rectangular height.");
                Assert.AreEqual(0.8f, projectile.CollisionLength, 0.001f, "Daji skill 1 should use the configured rectangular length.");

                harness.EnterGameAndWarmup(reason: "daji skill 1 rectangular wave contract");
                var actorId = harness.AssertPlayerActorBound();
                var targetActorId = HeroSkillHeadlessContract.SpawnEnemyHero(harness, x: 3f, z: 0.8f);
                var hpBefore = harness.GetActorHp(targetActorId);
                var skills = harness.World.Services.Resolve<SkillCastCoordinator>();
                var cast = skills.TryCastBySlot(actorId, Skill1.Slot, aimPos: default, aimDir: Vec3.Right, targetActorId: 0);
                Assert.IsTrue(cast.Success, "Daji skill 1 should cast along the selected direction. failReason=" + cast.FailReason);

                var effectTrace = harness.TickUntilTraceNode(
                    MobaTraceKind.EffectExecution,
                    Skill1.EffectId,
                    maxTicks: harness.CalculateWaitTicksForSkillEffect(Skill1.SkillId, Skill1.EffectId, safetyFrames: 5) + 30,
                    message: "Daji skill 1 should execute its configured effect.");
                harness.AssertProjectileLaunchedUnderEffect(effectTrace.RootId, 31050101, 30050101);
                TickUntilProjectileSpawn(harness, 30050101, maxTicks: 30);
                TickUntilActorHpLessThan(
                    harness,
                    targetActorId,
                    hpBefore,
                    maxTicks: 60,
                    message: "Daji skill 1 rectangular wave should hit a target offset from the center ray but inside its configured width.");
            }
        }

        [Test]
        public void Skill10050201_ShouldLaunchHomingCharmAndApplyControlOnHit()
        {
            using (var harness = HeroSkillHeadlessContract.CreateHarness(Daji, "daji_skill_2_homing_charm_contract_world"))
            {
                HeroSkillHeadlessContract.AssertTriggerActions(
                    harness,
                    10050201,
                    (int)TriggeringConstants.ShootProjectileId.Value,
                    (int)TriggeringConstants.DebugLogId.Value);
                HeroSkillHeadlessContract.AssertTriggerActions(
                    harness,
                    10050211,
                    (int)TriggeringConstants.GiveDamageId.Value,
                    (int)TriggeringConstants.AddBuffId.Value);
                harness.AssertProjectileConfigExists(31050201, 30050201);

                harness.EnterGameAndWarmup(reason: "daji skill 2 homing charm contract");
                var actorId = harness.AssertPlayerActorBound();
                var targetActorId = HeroSkillHeadlessContract.SpawnEnemyHero(harness, x: 3f);
                var skills = harness.World.Services.Resolve<SkillCastCoordinator>();
                var cast = skills.TryCastBySlot(actorId, Skill2.Slot, aimPos: default, aimDir: new Vec3(1f, 0f, 0f), targetActorId: targetActorId);
                Assert.IsTrue(cast.Success, "Daji skill 2 should cast toward the selected enemy. failReason=" + cast.FailReason);

                var effectTrace = harness.TickUntilTraceNode(
                    MobaTraceKind.EffectExecution,
                    Skill2.EffectId,
                    maxTicks: harness.CalculateWaitTicksForSkillEffect(Skill2.SkillId, Skill2.EffectId, safetyFrames: 5) + 30,
                    message: "Daji skill 2 should execute its configured effect.");
                harness.AssertProjectileLaunchedUnderEffect(effectTrace.RootId, 31050201, 30050201);
                TickUntilProjectileSpawn(harness, 30050201, maxTicks: 30);
                TickUntilActorBuff(
                    harness,
                    targetActorId,
                    10050201,
                    maxTicks: 60,
                    message: "Daji charm projectile should hit the selected target within the configured projectile lifetime.");
                HeroSkillHeadlessContract.AssertFreshBuff(
                    harness,
                    targetActorId,
                    10050201,
                    minRemainingSeconds: 1.0f,
                    message: "Daji charm projectile should apply the configured control buff after hitting its selected target.");
            }
        }

        [Test]
        public void Skill10050301_ShouldApplyFoxfireStateAndCompileRepeatHitDecay()
        {
            using (var harness = HeroSkillHeadlessContract.CreateHarness(Daji, "daji_skill_3_foxfire_decay_contract_world"))
            {
                HeroSkillHeadlessContract.AssertTriggerActions(
                    harness,
                    10050301,
                    (int)TriggeringConstants.AddBuffId.Value,
                    (int)TriggeringConstants.DebugLogId.Value);
                HeroSkillHeadlessContract.AssertTriggerActions(
                    harness,
                    10050311,
                    (int)TriggeringConstants.ShootProjectileId.Value,
                    (int)TriggeringConstants.DebugLogId.Value);
                HeroSkillHeadlessContract.AssertTriggerActions(
                    harness,
                    10050314,
                    (int)TriggeringConstants.GetActionId(TriggeringConstants.Actions.AdjustDamageNumber).Value);
                harness.AssertProjectileConfigExists(31050301, 30050301);

                var effectTrace = HeroSkillHeadlessContract.CastSlotAndAssertEffect(harness, Skill3, "daji skill 3 foxfire state contract");
                var actorId = harness.AssertPlayerActorBound();
                HeroSkillHeadlessContract.SpawnEnemyHero(harness, x: 3f);
                harness.AssertActionExecutedUnderEffect(effectTrace.RootId, (int)TriggeringConstants.AddBuffId.Value, TriggeringConstants.Actions.AddBuff);
                HeroSkillHeadlessContract.AssertFreshBuff(
                    harness,
                    actorId,
                    10050301,
                    minRemainingSeconds: 1.2f,
                    message: "Daji ultimate should enter the configured 1.6 second foxfire state.");
                TickUntilProjectileSpawn(harness, 30050301, maxTicks: 60);
            }
        }

        private static void TickUntilActorHpLessThan(MobaSkillConfigTestHarness harness, int actorId, float hp, int maxTicks, string message)
        {
            for (var i = 0; i <= maxTicks; i++)
            {
                if (harness.GetActorHp(actorId) < hp) return;
                if (i < maxTicks) harness.Tick(1);
            }

            Assert.Fail(message);
        }

        private static void TickUntilActorBuff(MobaSkillConfigTestHarness harness, int actorId, int buffId, int maxTicks, string message)
        {
            for (var i = 0; i <= maxTicks; i++)
            {
                if (harness.HasActorBuff(actorId, buffId)) return;
                if (i < maxTicks) harness.Tick(1);
            }

            Assert.Fail(message);
        }

        private static void TickUntilProjectileSpawn(MobaSkillConfigTestHarness harness, int templateId, int maxTicks)
        {
            var provider = harness.World.Services.Resolve<IWorldStateSnapshotBatchProvider>();
            var snapshots = new List<WorldStateSnapshot>(16);
            for (var i = 0; i <= maxTicks; i++)
            {
                snapshots.Clear();
                provider.CollectSnapshots(harness.FrameTime.Frame, snapshots, 32);
                for (var snapshotIndex = 0; snapshotIndex < snapshots.Count; snapshotIndex++)
                {
                    if (snapshots[snapshotIndex].OpCode != MobaOpCodes.Snapshot.ProjectileEvent) continue;
                    var entries = MobaProjectileEventSnapshotCodec.Deserialize(snapshots[snapshotIndex].Payload);
                    for (var entryIndex = 0; entryIndex < entries.Length; entryIndex++)
                    {
                        if (entries[entryIndex].Kind == (int)ProjectileEventKind.Spawn
                            && entries[entryIndex].TemplateId == templateId)
                        {
                            return;
                        }
                    }
                }

                if (i < maxTicks) harness.Tick(1);
            }

            Assert.Fail("Projectile spawn snapshot missing for template " + templateId + " within " + maxTicks + " ticks.");
        }
    }

    public sealed class DajiRectangularProjectileTests
    {
        private const int CollisionLayerId = 0;
        private const int CollisionLayerMask = 1 << CollisionLayerId;

        [Test]
        public void RectangularSweep_ShouldHitOffsetTargetThatPointRayMisses()
        {
            var pointHits = TickSingleProjectile(collisionHalfExtents: Vec3.Zero, out _, out _);
            var boxHits = TickSingleProjectile(collisionHalfExtents: new Vec3(0.5f, 0.5f, 0.2f), out _, out _);

            Assert.AreEqual(0, pointHits.Count, "A point projectile should miss a target outside its center ray.");
            Assert.AreEqual(1, boxHits.Count, "A rectangular projectile should hit a target inside its width.");
        }

        [Test]
        public void RectangularSweep_ShouldSkipIgnoredColliderAndHitTargetBehindIt()
        {
            var collision = new NaiveCollisionWorld();
            var ignored = AddSphere(collision, new Vec3(2f, 0f, 0.4f), 0.1f);
            var target = AddSphere(collision, new Vec3(4f, 0f, 0.4f), 0.1f);
            var filter = new FixedCollisionResponseFilter(ProjectileCollisionResponse.Hit);
            filter.Set(ignored, ProjectileCollisionResponse.Ignore);
            var world = new ProjectileWorld(collision);
            SpawnTestProjectile(world, filter, new Vec3(0.5f, 0.5f, 0.2f));
            var hits = new List<ProjectileHitEvent>();
            var exits = new List<ProjectileExitEvent>();

            world.Tick(1, 1f, hits, exits, tickEvents: null);

            Assert.AreEqual(1, hits.Count, "Ignoring an overlapping friendly collider should not exhaust rectangular sweep attempts.");
            Assert.AreEqual(target, hits[0].HitCollider, "The rectangular projectile should continue to the valid target behind the ignored collider.");
            Assert.AreEqual(0, exits.Count, "A piercing projectile should remain active after the valid hit.");
        }

        [Test]
        public void RectangularSweep_BlockerShouldExitPiercingProjectileWithoutHitEvent()
        {
            var collision = new NaiveCollisionWorld();
            var blocker = AddSphere(collision, new Vec3(2f, 0f, 0f), 0.2f);
            AddSphere(collision, new Vec3(4f, 0f, 0f), 0.2f);
            var filter = new FixedCollisionResponseFilter(ProjectileCollisionResponse.Hit);
            filter.Set(blocker, ProjectileCollisionResponse.Block);
            var world = new ProjectileWorld(collision);
            SpawnTestProjectile(world, filter, new Vec3(0.5f, 0.5f, 0.2f));
            var hits = new List<ProjectileHitEvent>();
            var exits = new List<ProjectileExitEvent>();

            world.Tick(1, 1f, hits, exits, tickEvents: null);

            Assert.AreEqual(0, hits.Count, "A hard blocker should not emit a unit hit event.");
            Assert.AreEqual(1, exits.Count, "A hard blocker should immediately terminate the projectile.");
            Assert.AreEqual(ProjectileExitReason.Hit, exits[0].Reason);
            Assert.AreEqual(0, world.ActiveCount);
        }

        [Test]
        public void ManualDespawn_ShouldQueueExitWithoutHitEvent()
        {
            var service = new ProjectileService(new CollisionService());
            var projectileId = service.Spawn(new ProjectileSpawnParams(
                ownerId: 1,
                templateId: 30050101,
                launcherActorId: 2,
                rootActorId: 1,
                spawnFrame: 7,
                position: Vec3.Zero,
                direction: new Vec3(1f, 0f, 0f),
                speed: 10f,
                returnAfterFrames: 0,
                returnSpeed: 0f,
                returnStopDistance: 0f,
                lifetimeFrames: 10,
                maxDistance: 20f,
                collisionLayerMask: CollisionLayerMask,
                ignoreCollider: default,
                hitFilter: null));
            var hits = new List<ProjectileHitEvent>();
            var exits = new List<ProjectileExitEvent>();

            Assert.IsTrue(service.Despawn(projectileId, 42, ProjectileExitReason.Manual));
            service.DrainHitEvents(hits);
            service.DrainExitEvents(exits);

            Assert.AreEqual(0, service.ActiveCount);
            Assert.AreEqual(0, hits.Count, "Area-driven projectile clearing must not emit a projectile hit event.");
            Assert.AreEqual(1, exits.Count);
            Assert.AreEqual(projectileId, exits[0].Projectile);
            Assert.AreEqual(ProjectileExitReason.Manual, exits[0].Reason);
            Assert.AreEqual(42, exits[0].Frame);
        }

        [Test]
        public void Rollback_ShouldRetainRectangularCollisionHalfExtents()
        {
            var collision = new NaiveCollisionWorld();
            var target = AddSphere(collision, new Vec3(4f, 0f, 0.4f), 0.1f);
            var world = new ProjectileWorld(collision);
            SpawnTestProjectile(world, hitFilter: null, collisionHalfExtents: new Vec3(0.5f, 0.5f, 0.2f));
            var payload = world.ExportRollback(new FrameIndex(0));
            world.ImportRollback(new FrameIndex(0), payload);
            var hits = new List<ProjectileHitEvent>();

            world.Tick(1, 1f, hits, exitEvents: null, tickEvents: null);

            Assert.AreEqual(1, hits.Count, "Rollback restore should preserve rectangular collision geometry.");
            Assert.AreEqual(target, hits[0].HitCollider);
        }

        private static List<ProjectileHitEvent> TickSingleProjectile(in Vec3 collisionHalfExtents, out List<ProjectileExitEvent> exits, out ProjectileWorld world)
        {
            var collision = new NaiveCollisionWorld();
            AddSphere(collision, new Vec3(4f, 0f, 0.4f), 0.1f);
            world = new ProjectileWorld(collision);
            SpawnTestProjectile(world, hitFilter: null, collisionHalfExtents: collisionHalfExtents);
            var hits = new List<ProjectileHitEvent>();
            exits = new List<ProjectileExitEvent>();
            world.Tick(1, 1f, hits, exits, tickEvents: null);
            return hits;
        }

        private static void SpawnTestProjectile(ProjectileWorld world, IProjectileHitFilter hitFilter, in Vec3 collisionHalfExtents)
        {
            world.Spawn(new ProjectileSpawnParams(
                ownerId: 1,
                templateId: 30050101,
                launcherActorId: 1,
                rootActorId: 1,
                spawnFrame: 0,
                position: Vec3.Zero,
                direction: Vec3.Right,
                speed: 10f,
                returnAfterFrames: 0,
                returnSpeed: 0f,
                returnStopDistance: 0f,
                lifetimeFrames: 30,
                maxDistance: 20f,
                collisionLayerMask: CollisionLayerMask,
                ignoreCollider: default,
                hitsRemaining: 6,
                hitPolicyKind: ProjectileHitPolicyKind.Pierce,
                hitPolicyParam: 6,
                hitFilter: hitFilter,
                collisionHalfExtents: collisionHalfExtents));
        }

        private static ColliderId AddSphere(NaiveCollisionWorld collision, in Vec3 position, float radius)
        {
            var transform = new Transform3(position, Quat.Identity, Vec3.One);
            var shape = ColliderShape.CreateSphere(new Sphere(Vec3.Zero, radius));
            return collision.Add(transform, shape, CollisionLayerId);
        }

        private sealed class FixedCollisionResponseFilter : IProjectileHitFilter, IProjectileCollisionResponseResolver
        {
            private readonly Dictionary<int, ProjectileCollisionResponse> _responses = new Dictionary<int, ProjectileCollisionResponse>();
            private readonly ProjectileCollisionResponse _defaultResponse;

            public FixedCollisionResponseFilter(ProjectileCollisionResponse defaultResponse)
            {
                _defaultResponse = defaultResponse;
            }

            public void Set(ColliderId collider, ProjectileCollisionResponse response)
            {
                _responses[collider.Value] = response;
            }

            public bool ShouldHit(int ownerId, ColliderId collider, int frame)
            {
                return ResolveCollision(ownerId, collider, frame) == ProjectileCollisionResponse.Hit;
            }

            public ProjectileCollisionResponse ResolveCollision(int ownerId, ColliderId collider, int frame)
            {
                return _responses.TryGetValue(collider.Value, out var response) ? response : _defaultResponse;
            }
        }
    }
}
