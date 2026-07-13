using AbilityKit.Ability.Host;
using AbilityKit.Game.Battle.View.Lib.Skill;
using AbilityKit.Game.Flow;
using AbilityKit.Protocol.Moba;
using NUnit.Framework;

namespace AbilityKit.Game.Test.UnitTest
{
    public sealed class BattleHudSkillButtonTemplateResolverTests
    {
        [Test]
        public void ConfiguredLoadout_ResolvesTemplateDrivenSlotsAndAppendedBasicAttack()
        {
            var resolver = new BattleHudSkillButtonTemplateResolver();
            var loadout = new MobaPlayerLoadout(
                playerId: new PlayerId("mozi_player"),
                teamId: 1,
                heroId: 1004,
                attributeTemplateId: 1004,
                level: 1,
                basicAttackSkillId: 10040011,
                skillIds: new[] { 10040101, 10040201, 10040301 },
                spawnIndex: 0);

            Assert.AreEqual(4, resolver.ResolveSkillButtonCount(loadout));

            AssertResolvedSlot(
                resolver,
                loadout,
                slot: 1,
                expectedSkillId: 10040101,
                expectedPreviewShape: BattleHudSkillPreviewShape.DirectionLine,
                expectedIndicatorShape: SkillAimIndicatorShape.DirectionLine);
            AssertResolvedSlot(
                resolver,
                loadout,
                slot: 2,
                expectedSkillId: 10040201,
                expectedPreviewShape: BattleHudSkillPreviewShape.DirectionLine,
                expectedIndicatorShape: SkillAimIndicatorShape.DirectionLine);
            AssertResolvedSlot(
                resolver,
                loadout,
                slot: 3,
                expectedSkillId: 10040301,
                expectedPreviewShape: BattleHudSkillPreviewShape.None,
                expectedIndicatorShape: SkillAimIndicatorShape.Hidden);

            AssertResolvedSlot(
                resolver,
                loadout,
                slot: 4,
                expectedSkillId: 10040011,
                expectedPreviewShape: BattleHudSkillPreviewShape.None,
                expectedIndicatorShape: SkillAimIndicatorShape.Hidden);

            Assert.IsFalse(resolver.TryResolveSkill(loadout, 5, out _, out _, out _));
        }

        private static void AssertResolvedSlot(
            BattleHudSkillButtonTemplateResolver resolver,
            in MobaPlayerLoadout loadout,
            int slot,
            int expectedSkillId,
            BattleHudSkillPreviewShape expectedPreviewShape,
            SkillAimIndicatorShape expectedIndicatorShape)
        {
            Assert.IsTrue(resolver.TryResolveSkill(loadout, slot, out var skill, out _, out var spec), $"slot {slot} should resolve.");
            Assert.IsNotNull(skill, $"slot {slot} should resolve a concrete skill config.");
            Assert.AreEqual(expectedSkillId, skill.Id);
            Assert.AreEqual(expectedSkillId, spec.SkillId);
            Assert.AreEqual(expectedPreviewShape, spec.PreviewShape);
            Assert.AreEqual(expectedIndicatorShape, spec.IndicatorShape);
        }
    }
}
