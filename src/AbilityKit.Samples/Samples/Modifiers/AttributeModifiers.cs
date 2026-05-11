using System;
using AbilityKit.Samples.Infrastructure;
using AbilityKit.Samples.Common.Math;

namespace AbilityKit.Samples.Samples.Modifiers
{
    /// <summary>
    /// AttributeModifiers - ?????
    /// </summary>
    public sealed class AttributeModifiers : SampleBase
    {
        public override string Title => "Attribute Modifiers";
        public override string Description => "?? RPG ????????";
        public override SampleCategory Category => SampleCategory.Modifiers;

        protected override void OnRun()
        {
            Log("RPG ??????");
            Output.Divider();

            float baseHealth = 1000f;
            float baseAttack = 100f;

            Log($"???? HP={baseHealth}, ATK={baseAttack}");

            Output.Divider();
            Log("?????");

            float hp = baseHealth;
            float atk = baseAttack;

            // ??????
            Log("  1. [??] +200 HP, +20 ATK (Additive)");
            hp += 200f;
            atk += 20f;

            Log("  2. [??] x1.5 HP (PercentMult)");
            hp *= 1.5f;

            Log("  3. [BUFF] +10% HP, +5% ATK (PercentAdd)");
            hp *= 1.1f;
            atk *= 1.05f;

            Log("  4. [??] +100 HP (Additive)");
            hp += 100f;

            Output.Divider();
            Log("????:");

            float originalHp = baseHealth + 200f;
            float step1 = originalHp * 1.5f;
            float step2 = step1 * 1.1f;
            float finalHp = step2 + 100f;

            Log($"  HP: {baseHealth} + 200 = {originalHp} * 1.5 = {step1:F0} * 1.1 = {step2:F0} + 100 = {finalHp:F0}");
            Log($"  ATK: {baseAttack} + 20 = {atk - 20f:F0} * 1.05 = {atk:F0}");

            Output.Divider();
            Log($"????");
            Log($"  HP={finalHp:F0} ({MathUtil.PercentChange(baseHealth, finalHp):+F0}%)");
            Log($"  ATK={atk:F0} ({MathUtil.PercentChange(baseAttack, atk):+F0}%)");
        }
    }
}
