using System;
using AbilityKit.Ability.World.DI;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Triggering.Eventing;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan;

namespace AbilityKit.Demo.Moba.Predicates
{
    public static class MobaPlanPredicateFunctions
    {
        public static readonly FunctionId HasBuffFunctionId = new FunctionId(StableStringId.Get("predicate:has_buff"));

        public static void Register(FunctionRegistry functions)
        {
            if (functions == null) return;

            functions.Register<Predicate2<object, IWorldResolver>>(HasBuffFunctionId, HasBuff, isDeterministic: true);
        }

        private static bool HasBuff(object triggerArgs, NamedArgsDict args, ExecCtx<IWorldResolver> ctx)
        {
            var buffId = ReadInt(args, "_0");
            if (buffId <= 0 || ctx.Context == null) return false;

            var checkStack = ReadInt(args, "_1") != 0;
            var actors = default(MobaActorLookupService);
            if (!CombatPredicateRuntime.TryResolveTargetActorId(triggerArgs, ctx.Context, out var targetActorId)
                || !CombatPredicateRuntime.TryGetActor(ctx.Context, ref actors, targetActorId, out var actor)
                || actor == null
                || !actor.hasBuffs
                || actor.buffs.Active == null)
            {
                return false;
            }

            var active = actor.buffs.Active;
            for (int i = 0; i < active.Count; i++)
            {
                var runtime = active[i];
                if (runtime == null || runtime.BuffId != buffId) continue;
                return !checkStack || runtime.StackCount > 0;
            }

            return false;
        }

        private static int ReadInt(NamedArgsDict args, string key)
        {
            if (args == null || !args.TryGetValue(key, out var value)) return 0;
            return (int)Math.Round(value.Ref.ConstValue);
        }
    }
}
