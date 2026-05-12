using System;
using System.Collections.Generic;
using AbilityKit.Modifiers;
using AbilityKit.Samples.Abstractions;

namespace AbilityKit.Samples.Logic.Samples.Modifiers
{
    /// <summary>
    /// AttributeModifiers - 属性修改器
    /// 展示如何使用修改器系统统一管理 RPG 属性
    /// </summary>
    [Sample]
    public sealed class AttributeModifiers : SampleBase
    {
        public override string Title => "属性修改器 (RPG Attributes)";
        public override string Description => "使用修改器系统管理 RPG 角色属性";
        public override SampleCategory Category => SampleCategory.Modifiers;

        protected override void OnRun()
        {
            Log("=== RPG 属性修改器系统 ===");
            Output.Divider();

            var character = new CharacterAttributes(Log);
            Log("创建角色: 战士 Lv.1");
            character.PrintAttributes();

            Output.Divider();

            Log("【1】应用装备加成 (Add):");
            Log("  +200 生命, +50 攻击, +20 防御");

            character.AddModifier(ModifierData.Add(ModifierKey.MaxHealth, 200f));
            character.AddModifier(ModifierData.Add(ModifierKey.AttackPower, 50f));
            character.AddModifier(ModifierData.Add(ModifierKey.Defense, 20f));

            character.PrintAttributes();

            Output.Divider();

            Log("【2】应用 BUFF 加成 (PercentAdd):");
            Log("  +10% 生命, +20% 攻击");

            character.AddModifier(ModifierData.PercentAdd(ModifierKey.MaxHealth, 0.1f));
            character.AddModifier(ModifierData.PercentAdd(ModifierKey.AttackPower, 0.2f));

            character.PrintAttributes();

            Output.Divider();

            Log("【3】应用套装效果 (Mul):");
            Log("  ×1.5 攻击 (套装两件套)");

            character.AddModifier(ModifierData.Mul(ModifierKey.AttackPower, 1.5f));

            character.PrintAttributes();

            Output.Divider();

            Log("【4】应用称号覆盖 (Override):");
            Log("  =1000 生命上限 (传说称号)");

            character.AddModifier(ModifierData.Override(ModifierKey.MaxHealth, 1000f));

            character.PrintAttributes();

            Output.Divider();

            Log("【5】移除所有修改器 (还原原始值):");

            var baseHealth = character.GetBaseValue(ModifierKey.MaxHealth);
            var baseAttack = character.GetBaseValue(ModifierKey.AttackPower);
            var baseDefense = character.GetBaseValue(ModifierKey.Defense);

            character.RemoveAllModifiers();
            character.PrintAttributes();

            Log($"  原始值: HP={baseHealth}, ATK={baseAttack}, DEF={baseDefense}");

            Output.Divider();

            Log("【6】时间衰减 BUFF 示例:");

            var timeDecayMod = ModifierData.AddWithTimeDecay(
                ModifierKey.AttackPower,
                initialValue: 100f,
                duration: 5f,
                DecayType.Linear
            );

            Log("  获得 BUFF: +100 攻击, 持续 5 秒 (线性衰减)");

            character.AddModifier(timeDecayMod);
            var context = new SampleModifierContext();
            Log($"  初始: ATK = {character.GetFinalValue(ModifierKey.AttackPower, context)}");

            for (int i = 1; i <= 5; i++)
            {
                context = new SampleModifierContext { ElapsedTime = i };
                var currentAtk = character.GetFinalValue(ModifierKey.AttackPower, context);
                Log($"  t={i}s: ATK = {currentAtk} (BUFF 剩余 {5 - i}s)");
            }

            Output.Divider();

            Log("【7】属性百分比变化统计:");

            Log($"  生命变化: {baseHealth} → {character.GetBaseValue(ModifierKey.MaxHealth)}");
            Log($"  攻击变化: {baseAttack} → {character.GetBaseValue(ModifierKey.AttackPower)}");
            Log($"  防御变化: {baseDefense} → {character.GetBaseValue(ModifierKey.Defense)}");
        }
    }

    /// <summary>
    /// 角色属性类 - 演示如何使用修改器系统
    /// </summary>
    internal sealed class CharacterAttributes
    {
        private readonly Action<string> _log;
        private readonly Dictionary<ModifierKey, float> _baseValues = new();
        private readonly List<ModifierData> _modifiers = new();
        private readonly ModifierCalculator _calculator = new();

        private static readonly ModifierKey AttackSpeedKey = ModifierKey.Create(1, 4);

        public CharacterAttributes(Action<string> log)
        {
            _log = log;
            _baseValues[ModifierKey.MaxHealth] = 1000f;
            _baseValues[ModifierKey.AttackPower] = 100f;
            _baseValues[ModifierKey.Defense] = 50f;
            _baseValues[ModifierKey.MoveSpeed] = 300f;
            _baseValues[AttackSpeedKey] = 1.0f;
        }

        public void AddModifier(ModifierData modifier)
        {
            _modifiers.Add(modifier);
        }

        public void RemoveModifier(ModifierData modifier)
        {
            _modifiers.Remove(modifier);
        }

        public void RemoveAllModifiers()
        {
            _modifiers.Clear();
        }

        public float GetBaseValue(ModifierKey key)
        {
            return _baseValues.TryGetValue(key, out var value) ? value : 0f;
        }

        public float GetFinalValue(ModifierKey key, IModifierContext context)
        {
            if (!_baseValues.TryGetValue(key, out var baseValue))
                return 0f;

            var modifiersForKey = _modifiers
                .Where(m => m.Key == key)
                .ToArray();

            if (modifiersForKey.Length == 0)
                return baseValue;

            var result = _calculator.Calculate(modifiersForKey, baseValue, level: context.Level);
            return result.FinalValue;
        }

        public void PrintAttributes()
        {
            var context = new SampleModifierContext();
            _log($"  HP: {_baseValues[ModifierKey.MaxHealth]:F0} (最终: {GetFinalValue(ModifierKey.MaxHealth, context):F0})");
            _log($"  ATK: {_baseValues[ModifierKey.AttackPower]:F0} (最终: {GetFinalValue(ModifierKey.AttackPower, context):F0})");
            _log($"  DEF: {_baseValues[ModifierKey.Defense]:F0} (最终: {GetFinalValue(ModifierKey.Defense, context):F0})");
            _log($"  SPD: {_baseValues[ModifierKey.MoveSpeed]:F0}");
        }
    }
}
