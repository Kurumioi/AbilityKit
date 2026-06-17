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
        public readonly MobaActionTargetRequest TargetRequest;

        public GiveDamageArgs(float damageValue, int reasonParam, DamageType damageType = DamageType.Physical, MobaActionTargetRequest targetRequest = default)
        {
            DamageValue = damageValue;
            ReasonParam = reasonParam;
            DamageType = damageType;
            TargetRequest = targetRequest;
        }

        public static GiveDamageArgs Default => new GiveDamageArgs(0f, 0, DamageType.Physical, MobaActionTargetRequest.ContextTarget());
    }
}