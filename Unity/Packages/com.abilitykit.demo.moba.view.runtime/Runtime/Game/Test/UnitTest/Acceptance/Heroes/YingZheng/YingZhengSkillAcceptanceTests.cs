using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Services.Buffs;
using AbilityKit.Demo.Moba.Systems;
using AbilityKit.Protocol.Moba;
using AbilityKit.Trace;
using NUnit.Framework;

namespace AbilityKit.Game.Test.UnitTest
{
    public sealed class YingZhengSkillAcceptanceTests : MobaAcceptanceTestBase
    {
        private static readonly HeroSkillContract YingZheng = new HeroSkillContract(
            "YingZheng",
            heroId: 1006,
            attributeTemplateId: 1006,
            skillIds: new[] { 10060101, 10060201, 10060301 });

        private static readonly HeroSkillSlotContract Skill2 = new HeroSkillSlotContract(2, 10060201, 10060201, 10060201);
        private static readonly HeroSkillSlotContract Skill3 = new HeroSkillSlotContract(3, 10060301, 10060301, 10060301);

        [Test]
        public void BasicAttack_ShouldUseThreeHitPiercingMagicSword()
        {
            using (var harness = HeroSkillHeadlessContract.CreateHarness(YingZheng, "ying_zheng_basic_attack_pierce_contract_world"))
            {
                harness.AssertSkillUsesCastFlow(10060001, 10060001);
                harness.AssertCastFlowContainsTimelineEffect(10060001, 10060001);
                HeroSkillHeadlessContract.AssertTriggerActions(
                    harness,
                    10060001,
                    (int)TriggeringConstants.ShootProjectileId.Value);
                HeroSkillHeadlessContract.AssertTriggerActions(
                    harness,
                    10060011,
                    (int)TriggeringConstants.GiveDamageId.Value);
                harness.AssertProjectileConfigExists(31060001, 30060001);
                Assert.IsTrue(harness.Config.TryGetProjectile(30060001, out var projectile), "Ying Zheng basic attack projectile config should exist.");
                Assert.AreEqual(3, projectile.HitsRemaining, "Ying Zheng basic attack should pierce up to three targets.");

                harness.EnterGameAndWarmup(reason: "ying zheng basic attack pierce contract");
                harness.AssertSlotSkill(slot: 4, expectedSkillId: 10060001);
            }
        }

        [Test]
        public void Skill10060201_ShouldCleanseSlowAndApplyGuardMoveSpeed()
        {
            using (var harness = HeroSkillHeadlessContract.CreateHarness(YingZheng, "ying_zheng_skill_2_guard_cleanse_contract_world"))
            {
                HeroSkillHeadlessContract.AssertTriggerActions(
                    harness,
                    10060201,
                    (int)TriggeringConstants.RemoveBuffId.Value,
                    (int)TriggeringConstants.AddBuffId.Value,
                    (int)TriggeringConstants.AddShieldId.Value,
                    (int)TriggeringConstants.DebugLogId.Value);

                harness.EnterGameAndWarmup(reason: "ying zheng skill 2 guard cleanse contract");
                var actorId = harness.AssertPlayerActorBound();
                var baseMoveSpeed = harness.GetActorMoveSpeed(actorId);
                var buffs = harness.World.Services.Resolve<MobaBuffService>();
                Assert.IsTrue(
                    buffs.ApplyBuffImmediate(actorId, 10060101, actorId, durationOverrideMs: 0),
                    "Ying Zheng slow setup buff should apply before guard cleanse.");
                Assert.IsTrue(harness.HasActorBuff(actorId, 10060101), "Ying Zheng skill 1 slow should be active before the guard cleanse.");
                Assert.Less(harness.GetActorMoveSpeed(actorId), baseMoveSpeed, "The tagged slow setup should reduce movement speed before skill 2.");

                var effectTrace = HeroSkillHeadlessContract.CastSlotAndAssertEffect(harness, Skill2, "ying zheng skill 2 guard cleanse contract");
                harness.AssertActionExecutedUnderEffect(effectTrace.RootId, (int)TriggeringConstants.RemoveBuffId.Value, TriggeringConstants.Actions.RemoveBuff);
                harness.AssertActionExecutedUnderEffect(effectTrace.RootId, (int)TriggeringConstants.AddBuffId.Value, TriggeringConstants.Actions.AddBuff);
                harness.AssertActionExecutedUnderEffect(effectTrace.RootId, (int)TriggeringConstants.AddShieldId.Value, TriggeringConstants.Actions.AddShield);
                harness.Tick(1);

                Assert.IsFalse(harness.HasActorBuff(actorId, 10060101), "Ying Zheng skill 2 should remove active Buffs tagged Debuff.Slow.");
                HeroSkillHeadlessContract.AssertFreshBuff(
                    harness,
                    actorId,
                    10060201,
                    minRemainingSeconds: 2.5f,
                    message: "Ying Zheng skill 2 should apply its three-second guard move-speed Buff.");
                Assert.Greater(harness.GetActorMoveSpeed(actorId), baseMoveSpeed, "Ying Zheng skill 2 guard Buff should grant movement speed after cleansing slow.");
            }
        }

        [Test]
        public void Skill10060301_ShouldUseTimedFiveSwordFanLauncher()
        {
            using (var harness = HeroSkillHeadlessContract.CreateHarness(YingZheng, "ying_zheng_skill_3_fan_launcher_contract_world"))
            {
                HeroSkillHeadlessContract.AssertTriggerActions(
                    harness,
                    10060301,
                    (int)TriggeringConstants.AddBuffId.Value,
                    (int)TriggeringConstants.ShootProjectileId.Value,
                    (int)TriggeringConstants.DebugLogId.Value);
                harness.AssertProjectileConfigExists(31060301, 30060301);
                Assert.IsTrue(harness.Config.TryGetProjectileLauncher(31060301, out var launcher), "Ying Zheng ultimate launcher config should exist.");
                Assert.AreEqual(2500, launcher.DurationMs, "Ying Zheng ultimate should sustain sword emission for 2.5 seconds.");
                Assert.AreEqual(250, launcher.IntervalMs, "Ying Zheng ultimate should emit sword waves every 250ms.");
                Assert.AreEqual(5, launcher.CountPerShot, "Ying Zheng ultimate should emit five swords per wave.");
                Assert.AreEqual(12f, launcher.FanAngleDeg, 0.001f, "Ying Zheng ultimate should use the configured narrow fan angle.");

                var effectTrace = HeroSkillHeadlessContract.CastSlotAndAssertEffect(harness, Skill3, "ying zheng skill 3 fan launcher contract");
                var actorId = harness.AssertPlayerActorBound();
                harness.AssertActionExecutedUnderEffect(effectTrace.RootId, (int)TriggeringConstants.ShootProjectileId.Value, TriggeringConstants.Actions.ShootProjectile);
                harness.AssertProjectileLaunchedUnderEffect(effectTrace.RootId, 31060301, 30060301);
                HeroSkillHeadlessContract.AssertFreshBuff(
                    harness,
                    actorId,
                    10060301,
                    minRemainingSeconds: 2.0f,
                    message: "Ying Zheng ultimate should apply its configured sustained sword state.");
            }
        }
    }
}
