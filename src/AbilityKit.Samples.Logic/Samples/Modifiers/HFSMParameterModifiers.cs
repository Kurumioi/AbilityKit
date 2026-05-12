using System;
using System.Collections.Generic;
using AbilityKit.Modifiers;
using AbilityKit.Samples.Abstractions;

namespace AbilityKit.Samples.Logic.Samples.Modifiers
{
    /// <summary>
    /// HFSMParameterModifiers - 分层状态机参数修改器
    /// 展示如何用修改器系统影响 HFSM 状态参数
    /// </summary>
    [Sample]
    public sealed class HFSMParameterModifiers : SampleBase
    {
        public override string Title => "HFSM 参数修改器 (HFSM Parameters)";
        public override string Description => "用修改器控制分层状态机的运行时参数";
        public override SampleCategory Category => SampleCategory.Modifiers;

        protected override void OnRun()
        {
            Log("=== HFSM 参数修改器示例 ===");
            Output.Divider();

            Log("【核心概念】");
            Log("  修改器可以影响 HFSM 的运行时参数:");
            Output.Bullet("状态转换条件阈值");
            Output.Bullet("状态持续时间");
            Output.Bullet("状态执行速度");
            Output.Bullet("子状态机参数");

            Output.Divider();

            Log("【1】HFSM 参数键定义:");
            Log("  使用自定义 Category 用于状态参数");

            var stateParams = new[]
            {
                ("StateInvincible", ModifierKey.Create(100, 1, 0), "无敌状态"),
                ("StateSilence", ModifierKey.Create(100, 2, 0), "沉默状态"),
                ("StateImmune", ModifierKey.Create(100, 3, 0), "免疫状态"),
                ("TransitionThreshold", ModifierKey.Create(101, 1, 0), "转换阈值"),
                ("StateDuration", ModifierKey.Create(101, 2, 0), "状态持续时间"),
                ("StateSpeed", ModifierKey.Create(101, 3, 0), "状态速度倍率"),
            };

            foreach (var (name, key, desc) in stateParams)
            {
                Log($"  {name}: Category={key.Category}, Sub={key.SubCategory}, Custom={key.CustomId}");
            }

            Output.Divider();

            Log("【2】技能效果参数修改示例:");
            Log("  场景: 战士施放多个技能, 影响 HFSM 参数");

            var hero = new HFSMHero(Log);
            hero.PrintStatus();

            Output.Divider();

            Log("  施放【战神之力】:");
            Log("    效果: AttackPower ×2, TransitionThreshold -50%");
            hero.ApplyStateModifier(ModifierKey.Create(101, 1, 0), ModifierData.Mul(ModifierKey.AttackPower, 2f));
            hero.ApplyStateModifier(ModifierKey.Create(101, 1, 0), ModifierData.PercentAdd(ModifierKey.Create(101, 1, 0), -0.5f));
            hero.PrintStatus();

            Output.Divider();

            Log("  施放【时间扭曲】:");
            Log("    效果: StateSpeed ×1.5, TransitionThreshold +30%");
            hero.ApplyStateModifier(ModifierKey.Create(101, 3, 0), ModifierData.Mul(ModifierKey.Create(101, 3, 0), 1.5f));
            hero.ApplyStateModifier(ModifierKey.Create(101, 1, 0), ModifierData.PercentAdd(ModifierKey.Create(101, 1, 0), 0.3f));
            hero.PrintStatus();

            Output.Divider();

            Log("【3】状态机转换条件修改:");

            float baseThreshold = 100f;
            Log($"  基础转换阈值: {baseThreshold}");

            var thresholdModifiers = new List<ModifierData>
            {
                ModifierData.PercentAdd(ModifierKey.Create(101, 1, 0), -0.5f),
                ModifierData.Add(ModifierKey.Create(101, 1, 0), 20f)
            };

            var calculator = new ModifierCalculator();
            var result = calculator.Calculate(thresholdModifiers.ToArray(), baseThreshold);

            Log($"  应用修改器:");
            Log($"    -50% 阈值: {baseThreshold} × 0.5 = {baseThreshold * 0.5f}");
            Log($"    +20 阈值: +20");
            Log($"  最终阈值: {result.FinalValue}");

            Output.Divider();

            Log("【4】持续时间修改:");

            float baseDuration = 5f;
            Log($"  基础状态持续时间: {baseDuration}s");

            var durationModifiers = new List<ModifierData>
            {
                ModifierData.Add(ModifierKey.Create(101, 2, 0), 2f),
                ModifierData.Mul(ModifierKey.Create(101, 2, 0), 1.5f)
            };

            var durationResult = calculator.Calculate(durationModifiers.ToArray(), baseDuration);
            Log($"  应用修改器:");
            Log($"    +2s 持续时间");
            Log($"    ×1.5 持续时间");
            Log($"  最终持续时间: {durationResult.FinalValue:F1}s");

            Output.Divider();

            Log("【5】状态机速度倍率修改:");

            float baseSpeed = 1.0f;
            Log($"  基础速度倍率: {baseSpeed}×");

            var speedModifiers = new List<ModifierData>
            {
                ModifierData.Add(ModifierKey.Create(101, 3, 0), 0.5f),
                ModifierData.PercentAdd(ModifierKey.Create(101, 3, 0), 0.2f)
            };

            var speedResult = calculator.Calculate(speedModifiers.ToArray(), baseSpeed);
            Log($"  应用修改器:");
            Log($"    +0.5 速度");
            Log($"    +20% 速度");
            Log($"  最终速度倍率: {speedResult.FinalValue:F2}×");

            Output.Divider();

            Log("【6】组合效果演示:");
            Log("  技能: 终极形态");
            Log("    - 攻击力 ×3");
            Log("    - 防御力 ×0.5");
            Log("    - 移动速度 ×2");
            Log("    - 攻击速度 ×1.5");
            Log("    - 状态持续时间 ×1.5");
            Log("    - 无敌状态激活");

            Log("");
            Log("  效果计算:");

            float baseAtk = 100f;
            float baseDef = 50f;
            float baseSpd = 300f;
            float baseAtkSpd = 1.0f;
            float baseStateDur = 5f;

            Log($"    ATK: {baseAtk} × 3 = {baseAtk * 3}");
            Log($"    DEF: {baseDef} × 0.5 = {baseDef * 0.5f}");
            Log($"    SPD: {baseSpd} × 2 = {baseSpd * 2}");
            Log($"    ATK_SPD: {baseAtkSpd} × 1.5 = {baseAtkSpd * 1.5f}");
            Log($"    状态持续: {baseStateDur} × 1.5 = {baseStateDur * 1.5f}");
            Log($"    无敌: 激活");

            Output.Divider();

            Log("【总结】HFSM 参数修改的优势:");
            Output.Bullet("运行时动态调整状态机行为");
            Output.Bullet("技能效果可以叠加和组合");
            Output.Bullet("便于实现状态驱动的游戏逻辑");
            Output.Bullet("修改器移除后参数自动还原");
        }
    }

    /// <summary>
    /// 带有修改器系统的 HFSM 英雄
    /// </summary>
    internal sealed class HFSMHero
    {
        private readonly Action<string> _log;
        private float _attackPower = 100f;
        private float _defense = 50f;
        private float _moveSpeed = 300f;
        private float _attackSpeed = 1.0f;

        private float _transitionThreshold = 100f;
        private float _stateDuration = 5f;
        private float _stateSpeed = 1.0f;

        private bool _isInvincible = false;
        private bool _isSilenced = false;

        private readonly Dictionary<ModifierKey, ModifierData> _stateModifiers = new();
        private readonly ModifierCalculator _calculator = new();

        public HFSMHero(Action<string> log)
        {
            _log = log;
        }

        public void ApplyStateModifier(ModifierKey key, ModifierData modifier)
        {
            _stateModifiers[key] = modifier;
        }

        public void RemoveStateModifier(ModifierKey key)
        {
            _stateModifiers.Remove(key);
        }

        public void PrintStatus()
        {
            _log($"    攻击: {_attackPower}, 防御: {_defense}");
            _log($"    移动: {_moveSpeed}, 攻速: {_attackSpeed}");
            _log($"    转换阈值: {_transitionThreshold:F0}, 状态持续: {_stateDuration:F1}s");
            _log($"    状态速度: {_stateSpeed:F2}×");
            _log($"    无敌: {(_isInvincible ? "是" : "否")}, 沉默: {(_isSilenced ? "是" : "否")}");
        }
    }
}
