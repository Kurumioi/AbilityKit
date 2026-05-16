using System;
using System.Collections.Generic;
using AbilityKit.Ability.World.DI;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan;
using AbilityKit.Triggering.Registry;
using AbilityKit.Core.Common.Log;
using CritType = AbilityKit.Demo.Moba.CritType;
using DamageReasonKind = AbilityKit.Demo.Moba.DamageReasonKind;
using DamageFormulaKind = AbilityKit.Demo.Moba.DamageFormulaKind;
using EffectSourceKind = AbilityKit.Demo.Moba.EffectSourceKind;

namespace AbilityKit.Demo.Moba.Actions.Combat
{
    /// <summary>
    /// 造成伤害的 Action
    /// 继承 AutoPlanAction 基类，自动完成注册
    /// </summary>
    public sealed class GiveDamageAction : AutoPlanAction
    {
        /// <summary>
        /// 伤害值
        /// </summary>
        public float DamageValue { get; private set; }

        /// <summary>
        /// 伤害类型
        /// </summary>
        public DamageType DamageType { get; private set; }

        /// <summary>
        /// 原因参数
        /// </summary>
        public int ReasonParam { get; private set; }

        protected override string ActionId => "give_damage";
        protected override int Order => 10;

        public override void ParseFrom(Dictionary<string, ActionArgValue> namedArgs, ExecCtx<IWorldResolver> ctx)
        {
            DamageValue = AutoPlanActionExtensions.ResolveFloat(this, namedArgs, "damage_value", 0);
            DamageType = (DamageType)AutoPlanActionExtensions.ResolveInt(this, namedArgs, "damage_type", 0);
            ReasonParam = AutoPlanActionExtensions.ResolveInt(this, namedArgs, "reason_param", 0);
        }

        public override void Execute(object triggerArgs, ExecCtx<IWorldResolver> ctx)
        {
            if (!ctx.Context.TryResolve<DamagePipelineService>(out var pipeline) || pipeline == null)
            {
                Log.Warning("[GiveDamageAction] cannot resolve DamagePipelineService");
                return;
            }

            int attackerActorId = 0;
            int targetActorId = 0;

            Systems.PlanContextValueResolver.TryGetCasterActorId(triggerArgs, out attackerActorId);
            Systems.PlanContextValueResolver.TryGetTargetActorId(triggerArgs, out targetActorId);

            if (attackerActorId <= 0 || targetActorId <= 0)
                return;

            var attack = new AttackInfo
            {
                AttackerActorId = attackerActorId,
                TargetActorId = targetActorId,
                DamageType = DamageType,
                CritType = CritType.None,
                ReasonKind = DamageReasonKind.Skill,
                ReasonParam = ReasonParam,
                FormulaKind = (int)DamageFormulaKind.Standard,
                OriginSource = attackerActorId,
                OriginTarget = targetActorId,
                OriginKind = EffectSourceKind.Effect,
                OriginConfigId = 0,
                OriginContextId = 0,
            };
            attack.BaseDamage.BaseValue = DamageValue;

            pipeline.Execute(attack);
        }
    }
}
