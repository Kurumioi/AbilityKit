using System.Collections.Generic;
using AbilityKit.Ability.Host;
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

        private static readonly HeroSkillSlotContract Skill2 = new HeroSkillSlotContract(2, 10050201, 10050201, 10050201);
        private static readonly HeroSkillSlotContract Skill3 = new HeroSkillSlotContract(3, 10050301, 10050301, 10050301);

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
}
