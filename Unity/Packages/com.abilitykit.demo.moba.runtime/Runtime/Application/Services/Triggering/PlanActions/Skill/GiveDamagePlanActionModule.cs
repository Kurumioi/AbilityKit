using System.Collections.Generic;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Services.Search;
using AbilityKit.Ability.World.DI;
using AbilityKit.Core.Common.Log;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan;
using CritType = AbilityKit.Demo.Moba.CritType;
using DamageReasonKind = AbilityKit.Demo.Moba.DamageReasonKind;
using DamageFormulaKind = AbilityKit.Demo.Moba.DamageFormulaKind;
using AbilityKit.Demo.Moba.Systems;


namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    /// <summary>
    /// 閫犳垚浼ゅ鐨凱lan Action妯″潡
    /// 浣跨敤鏂扮殑鍏峰悕鍙傛暟 Schema API
    /// </summary>
    [PlanActionModule(order: 11)]
    public sealed class GiveDamagePlanActionModule : MobaPlanActionModuleBase<GiveDamageArgs, GiveDamagePlanActionModule>
    {
        protected override IActionSchema<GiveDamageArgs, IWorldResolver> Schema => GiveDamageSchema.Instance;

        protected override void Execute(object triggerArgs, GiveDamageArgs args, ExecCtx<IWorldResolver> ctx)
        {
            if (!ctx.Context.TryResolve<MobaCombatEffectService>(out var combat) || combat == null)
            {
                Log.Warning("[Plan] give_damage cannot resolve MobaCombatEffectService.");
                return;
            }

            var coreInput = MobaPlanActionInputResolver.Resolve(triggerArgs, ctx);
            var effectInput = new MobaEffectActionInput(in coreInput);
            if (!effectInput.HasCasterActor)
            {
                Log.Warning($"[Plan] give_damage missing caster. target={effectInput.TargetActorId}, damage={args.DamageValue:0.###}, reasonParam={args.ReasonParam}");
                return;
            }

            var attackerActorId = effectInput.CasterActorId;
            if (args.QueryTemplateId > 0)
            {
                if (!ctx.Context.TryResolve<SearchTargetService>(out var search) || search == null)
                {
                    Log.Warning($"[Plan] give_damage cannot resolve SearchTargetService. queryTemplateId={args.QueryTemplateId}");
                    return;
                }

                var targets = new List<int>(8);
                var aimPosition = coreInput.AimPosition;
                if (!search.TrySearchActorIds(args.QueryTemplateId, attackerActorId, in aimPosition, effectInput.TargetActorId, targets))
                {
                    return;
                }

                for (int i = 0; i < targets.Count; i++)
                {
                    ExecuteDamage(combat, args, effectInput, attackerActorId, targets[i]);
                }
                return;
            }

            if (!effectInput.HasTargetActor)
            {
                Log.Warning($"[Plan] give_damage missing target. attacker={attackerActorId}, damage={args.DamageValue:0.###}, reasonParam={args.ReasonParam}");
                return;
            }

            ExecuteDamage(combat, args, effectInput, attackerActorId, effectInput.TargetActorId);
        }

        private static void ExecuteDamage(MobaCombatEffectService combat, GiveDamageArgs args, MobaEffectActionInput input, int attackerActorId, int targetActorId)
        {
            if (targetActorId <= 0)
            {
                Log.Warning($"[Plan] give_damage invalid target. attacker={attackerActorId}, target={targetActorId}, damage={args.DamageValue:0.###}, reasonParam={args.ReasonParam}");
                return;
            }

            var origin = input.BuildOrigin(attackerActorId, targetActorId, MobaTraceKind.EffectExecution, 0);
            var attack = new AttackInfo
            {
                AttackerActorId = attackerActorId,
                TargetActorId = targetActorId,
                DamageType = args.DamageType,
                CritType = CritType.None,
                ReasonKind = DamageReasonKind.Skill,
                ReasonParam = args.ReasonParam,
                FormulaKind = (int)DamageFormulaKind.Standard,
            };
            attack.SetOrigin(in origin);
            attack.BaseDamage.BaseValue = args.DamageValue;

            var result = combat.DealDamage(attack);
            if (result == null)
            {
                Log.Warning($"[Plan] give_damage pipeline returned null. attacker={attackerActorId} target={targetActorId} damage={args.DamageValue:0.###} reasonParam={args.ReasonParam}");
            }
        }
    }
}
