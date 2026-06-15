using System;
using System.Collections.Generic;
using AbilityKit.Ability.World.DI;
using AbilityKit.Core.Logging;
using AbilityKit.Demo.Moba.Attributes;
using AbilityKit.Demo.Moba.Gameplay.Triggering;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Triggering.Payload;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Behavior;
using AbilityKit.Triggering.Runtime.Behavior.Predicates;
using AbilityKit.Triggering.Runtime.Plan;

namespace AbilityKit.Demo.Moba.Predicates
{
    /// <summary>
    /// 检查目标是否有指定 BUFF 的条件
    /// </summary>
    public sealed class HasBuffPredicate : AutoPredicate
    {
        /// <summary>
        /// BUFF ID
        /// </summary>
        public int BuffId { get; private set; }

        /// <summary>
        /// 是否检查层数大于 0
        /// </summary>
        public bool CheckStack { get; private set; }

        protected override string PredicateType => "has_buff";
        protected override int Order => 10;

        private IWorldResolver _services;
        private MobaActorLookupService _actors;
        private static bool _invalidConfigLogged;
        private static bool _missingTargetLogged;

        public override void ParseFrom(Dictionary<string, ActionArgValue> namedArgs, ExecCtx<IWorldResolver> ctx)
        {
            BuffId = AutoPredicateExtensions.ResolveInt(this, namedArgs, "buff_id", 0);
            CheckStack = AutoPredicateExtensions.ResolveInt(this, namedArgs, "check_stack", 0) > 0;
            _services = ctx.Context;
            CombatPredicateRuntime.TryResolve(_services, out _actors);
        }

        public override bool Evaluate(IBehaviorContext context)
        {
            if (BuffId <= 0)
            {
                CombatPredicateRuntime.LogOnce(ref _invalidConfigLogged, $"[HasBuffPredicate] Invalid buff_id. buffId={BuffId}");
                return false;
            }

            if (!CombatPredicateRuntime.TryResolveTargetActorId(context?.Args, _services, out var targetActorId)
                || !CombatPredicateRuntime.TryGetActor(_services, ref _actors, targetActorId, out var actor))
            {
                CombatPredicateRuntime.LogOnce(ref _missingTargetLogged, $"[HasBuffPredicate] Cannot resolve target actor. buffId={BuffId}, checkStack={CheckStack}, argsType={CombatPredicateRuntime.FormatArgsType(context?.Args)}");
                return false;
            }

            if (!actor.hasBuffs || actor.buffs.Active == null) return false;

            var active = actor.buffs.Active;
            for (int i = 0; i < active.Count; i++)
            {
                var runtime = active[i];
                if (runtime == null || runtime.BuffId != BuffId) continue;
                return !CheckStack || runtime.StackCount > 0;
            }

            return false;
        }
    }

    /// <summary>
    /// 检查目标生命值百分比的条件
    /// </summary>
    public sealed class HealthPercentPredicate : AutoPredicate
    {
        /// <summary>
        /// 生命值百分比阈值
        /// </summary>
        public float Threshold { get; private set; }

        /// <summary>
        /// 比较类型: 0=小于, 1=大于
        /// </summary>
        public int CompareType { get; private set; }

        protected override string PredicateType => "health_percent";
        protected override int Order => 10;

        private IWorldResolver _services;
        private IPayloadAccessorRegistry _payloads;
        private MobaActorLookupService _actors;
        private static bool _missingHealthLogged;
        private static bool _invalidCompareLogged;

        public override void ParseFrom(Dictionary<string, ActionArgValue> namedArgs, ExecCtx<IWorldResolver> ctx)
        {
            Threshold = AutoPredicateExtensions.ResolveFloat(this, namedArgs, "threshold", 50f);
            CompareType = AutoPredicateExtensions.ResolveInt(this, namedArgs, "compare_type", 0);
            _services = ctx.Context;
            CombatPredicateRuntime.TryResolve(_services, out _payloads);
            CombatPredicateRuntime.TryResolve(_services, out _actors);
        }

        public override bool Evaluate(IBehaviorContext context)
        {
            if (!CombatPredicateRuntime.TryResolveHealth(context?.Args, _services, _payloads, ref _actors, out var hp, out var maxHp) || maxHp <= 0f)
            {
                CombatPredicateRuntime.LogOnce(ref _missingHealthLogged, $"[HealthPercentPredicate] Cannot resolve target health. threshold={Threshold}, compareType={CompareType}, argsType={CombatPredicateRuntime.FormatArgsType(context?.Args)}");
                return false;
            }

            var percent = hp / maxHp * 100f;
            switch (CompareType)
            {
                case 0: return percent < Threshold;
                case 1: return percent > Threshold;
                default:
                    CombatPredicateRuntime.LogOnce(ref _invalidCompareLogged, $"[HealthPercentPredicate] Unsupported compare_type. compareType={CompareType}, threshold={Threshold}");
                    return false;
            }
        }
    }

    internal static class CombatPredicateRuntime
    {
        private static readonly int BattleTargetActorIdId = MobaBattlePayloadFields.FieldId(MobaBattlePayloadFields.TargetActorId);
        private static readonly int BattleTargetHpId = MobaBattlePayloadFields.FieldId(MobaBattlePayloadFields.TargetHp);
        private static readonly int BattleTargetMaxHpId = MobaBattlePayloadFields.FieldId(MobaBattlePayloadFields.TargetMaxHp);
        private static readonly int SkillTargetActorIdId = SkillRulePayloadFields.FieldId(SkillRulePayloadFields.TargetActorId);

        public static bool TryResolve<T>(IWorldResolver services, out T service) where T : class
        {
            service = null;
            return services != null && services.TryResolve(out service) && service != null;
        }

        public static bool TryGetActor(IWorldResolver services, ref MobaActorLookupService actors, int actorId, out global::ActorEntity actor)
        {
            actor = null;
            if (actorId <= 0) return false;
            if (actors == null) TryResolve(services, out actors);
            return actors != null && actors.TryGetActorEntity(actorId, out actor) && actor != null;
        }

        public static bool TryResolveTargetActorId(object args, IWorldResolver services, out int actorId)
        {
            actorId = 0;
            if (args == null) return false;

            if (args is MobaTriggerConditionContext conditionContext)
            {
                actorId = conditionContext.TargetActorId;
                if (actorId > 0) return true;
            }

            if (args is IMobaActorContextProvider actorContext && actorContext.TryGetTargetActorId(out actorId) && actorId > 0)
            {
                return true;
            }

            if (args is SkillPipelineContext skillContext)
            {
                actorId = skillContext.TargetActorId;
                if (actorId > 0) return true;
            }

            if (TryResolve(services, out IPayloadAccessorRegistry payloads))
            {
                object payload = args;
                if (payloads.TryGetInt(in payload, BattleTargetActorIdId, out actorId) && actorId > 0) return true;
                if (payloads.TryGetInt(in payload, SkillTargetActorIdId, out actorId) && actorId > 0) return true;
            }

            return false;
        }

        public static bool TryResolveHealth(object args, IWorldResolver services, IPayloadAccessorRegistry payloads, ref MobaActorLookupService actors, out float hp, out float maxHp)
        {
            hp = 0f;
            maxHp = 0f;
            if (args == null) return false;

            if (args is MobaTriggerConditionContext conditionContext)
            {
                if (TryResolveHealth(conditionContext.Payload, services, payloads, ref actors, out hp, out maxHp)) return true;
            }

            if (args is DamageResult damageResult && damageResult.TargetMaxHp > 0f)
            {
                hp = damageResult.TargetHp;
                maxHp = damageResult.TargetMaxHp;
                return true;
            }

            if (payloads == null) TryResolve(services, out payloads);
            if (payloads != null)
            {
                object payload = args;
                if (payloads.TryGetDouble(in payload, BattleTargetHpId, out var hpValue)
                    && payloads.TryGetDouble(in payload, BattleTargetMaxHpId, out var maxHpValue)
                    && maxHpValue > 0d)
                {
                    hp = (float)hpValue;
                    maxHp = (float)maxHpValue;
                    return true;
                }
            }

            if (!TryResolveTargetActorId(args, services, out var targetActorId)
                || !TryGetActor(services, ref actors, targetActorId, out var actor)
                || actor == null)
            {
                return false;
            }

            var attrs = actor.GetMobaAttrs();
            hp = attrs.Hp;
            maxHp = attrs.MaxHp;
            return maxHp > 0f;
        }

        public static string FormatArgsType(object args)
        {
            return args != null ? args.GetType().Name : "<null>";
        }

        public static void LogOnce(ref bool flag, string message)
        {
            if (flag) return;
            flag = true;
            Log.Warning(message);
        }
    }
}
