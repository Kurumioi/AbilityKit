using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba;
using AbilityKit.Demo.Moba.Services;
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
            if (!ctx.Context.TryResolve<DamagePipelineService>(out var pipeline) || pipeline == null)
                return;

            var input = MobaPlanActionInputResolver.Resolve(triggerArgs, ctx);
            if (!input.HasCasterActor) return;
            if (!input.HasTargetActor) return;

            var attackerActorId = input.CasterActorId;
            var targetActorId = input.TargetActorId;
            var executionContext = input.ExecutionContext;
            var traceScope = input.TraceScope;
            var origin = MobaActionOriginBuilder.Build(
                in executionContext,
                in traceScope,
                attackerActorId,
                targetActorId,
                MobaTraceKind.EffectExecution,
                0);

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

            var result = pipeline.Execute(attack);
            if (result == null)
            {
                Log.Warning($"[Plan] give_damage pipeline returned null. attacker={attackerActorId} target={targetActorId} damage={args.DamageValue:0.###} reasonParam={args.ReasonParam}");
            }
        }
    }
}
