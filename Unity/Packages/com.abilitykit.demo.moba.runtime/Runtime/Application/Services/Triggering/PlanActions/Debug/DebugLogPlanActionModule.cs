using AbilityKit.Core.Common.Log;
using AbilityKit.Ability.World.DI;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime;
using AbilityKit.Triggering.Runtime.Plan;
using AbilityKit.Triggering.Runtime.Plan.Json;
using AbilityKit.Demo.Moba.Systems;


namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    /// <summary>
    /// 调试日志Plan Action模块
    /// 使用强类型参�?Schema API
    /// </summary>
    [PlanActionModule(order: 0)]
    public sealed class DebugLogPlanActionModule : MobaPlanActionModuleBase<DebugLogArgs, DebugLogPlanActionModule>
    {
        protected override IActionSchema<DebugLogArgs, IWorldResolver> Schema => DebugLogSchema.Instance;

        protected override void Execute(object triggerArgs, DebugLogArgs args, ExecCtx<IWorldResolver> ctx)
        {
            var msg = string.Empty;

            if (args.MsgId > 0 && ctx.Context != null && ctx.Context.TryResolve<TriggerPlanJsonDatabase>(out var db) && db != null)
            {
                db.TryGetString(args.MsgId, out msg);
            }

            Log.Info($"[Plan] debug_log: {msg}");

            if (args.Dump)
            {
                var argsType = triggerArgs != null ? triggerArgs.GetType().Name : "<null>";
                var ctxType = ctx.Context != null ? ctx.Context.GetType().Name : "<null>";
                Log.Info($"[Plan] debug_log dump. argsType={argsType}, ctxType={ctxType}");
            }
        }
    }
}
