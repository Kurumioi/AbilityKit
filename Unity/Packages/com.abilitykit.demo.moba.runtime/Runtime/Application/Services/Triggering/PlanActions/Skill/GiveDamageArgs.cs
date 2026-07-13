using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba;

namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    /// <summary>
    /// give_damage Action 的强类型参数。
    /// </summary>
    public readonly struct GiveDamageArgs
    {
        /// <summary>
        /// 伤害数值。
        /// </summary>
        public readonly float DamageValue;

        /// <summary>
        /// 伤害原因参数，对应 DamageReasonKind。
        /// </summary>
        public readonly int ReasonParam;

        /// <summary>
        /// 伤害类型，如物理或法术。
        /// </summary>
        public readonly DamageType DamageType;

        /// <summary>
        /// 来源角色攻击属性加成。物理伤害读取物攻，法术伤害读取法攻。
        /// </summary>
        public readonly float SourceAttackRatio;

        public readonly MobaActionTargetRequest TargetRequest;

        /// <summary>
        /// 伤害来源类型。默认由技能配置造成技能伤害，普攻配置可显式标记为 BasicAttack。
        /// </summary>
        public readonly DamageReasonKind ReasonKind;

        public GiveDamageArgs(
            float damageValue,
            int reasonParam,
            DamageType damageType = DamageType.Physical,
            MobaActionTargetRequest targetRequest = default,
            float sourceAttackRatio = 0f,
            DamageReasonKind reasonKind = DamageReasonKind.Skill)
        {
            DamageValue = damageValue;
            ReasonParam = reasonParam;
            DamageType = damageType;
            SourceAttackRatio = sourceAttackRatio;
            TargetRequest = targetRequest;
            ReasonKind = reasonKind;
        }

        public static GiveDamageArgs Default => new GiveDamageArgs(0f, 0, DamageType.Physical, MobaActionTargetRequest.ContextTarget());
    }
}