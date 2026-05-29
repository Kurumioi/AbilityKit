using System;
using System.Collections.Generic;
using AbilityKit.Core.Generic;
using AbilityKit.Demo.Moba;
using AbilityKit.Core.Common.Log;
using AbilityKit.Effect;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Core.Common.Event;
using AbilityKit.Triggering.Eventing;
using AbilityKit.Triggering.Registry;
using AbilityKit.Triggering.Runtime.Plan;
using AbilityKit.Triggering.Runtime.Plan.Json;
using AbilityKit.Pipeline;
using AbilityKit.Trace;

namespace AbilityKit.Demo.Moba.Services
{
    using AbilityKit.Ability;
    [WorldService(typeof(MobaEffectExecutionService))]
    public sealed class MobaEffectExecutionService : IService
    {
        private readonly IWorldResolver _services;
        private readonly TriggerPlanJsonDatabase _planDb;
        private readonly AbilityKit.Triggering.Runtime.TriggerRunner<IWorldResolver> _planRunner;
        private readonly AbilityKit.Triggering.Eventing.IEventBus _planEventBus;
        private readonly FunctionRegistry _planFunctions;
        private readonly ActionRegistry _planActions;

        /// <summary>
        /// 从 NamedArgsDict 中提取双精度浮点参数
        /// </summary>
        private static double ExtractArg(NamedArgsDict namedArgs, string key)
        {
            if (namedArgs == null) return 0;
            if (namedArgs.TryGetValue(key, out var value))
            {
                return value.Ref.ConstValue;
            }
            return 0;
        }

        /// <summary>
        /// 溯源注册表（可选，用于链路追踪）
        /// </summary>
        public MobaTraceRegistry Trace { get; }

        /// <summary>
        /// 当前正在执行的溯源链路（用于父子关系追踪）
        /// </summary>
        private long _currentTraceRootId;
        private readonly List<long> _currentActionContextIds = new List<long>();

        public MobaEffectExecutionService(
            IWorldResolver services,
            TriggerPlanJsonDatabase planDb,
            AbilityKit.Triggering.Runtime.TriggerRunner<IWorldResolver> planRunner,
            AbilityKit.Triggering.Eventing.IEventBus planEventBus,
            FunctionRegistry planFunctions,
            ActionRegistry planActions)
        {
            _services = services;
            _planDb = planDb;
            _planRunner = planRunner;
            _planEventBus = planEventBus;
            _planFunctions = planFunctions;
            _planActions = planActions;

            // 初始化溯源注册表（默认启用）
            Trace = new MobaTraceRegistry();
        }

        public MobaEffectExecutionService(
            IWorldResolver services,
            TriggerPlanJsonDatabase planDb,
            AbilityKit.Triggering.Runtime.TriggerRunner<IWorldResolver> planRunner,
            AbilityKit.Triggering.Eventing.IEventBus planEventBus,
            FunctionRegistry planFunctions,
            ActionRegistry planActions,
            MobaTraceRegistry traceRegistry)
        {
            _services = services;
            _planDb = planDb;
            _planRunner = planRunner;
            _planEventBus = planEventBus;
            _planFunctions = planFunctions;
            _planActions = planActions;
            Trace = traceRegistry ?? new MobaTraceRegistry();
        }

        /// <summary>
        /// 获取当前正在追踪的 Action 链路
        /// </summary>
        public IReadOnlyList<long> CurrentActionChain => _currentActionContextIds;

        /// <summary>
        /// 提取溯源信息
        /// </summary>
        private (int sourceActorId, int targetActorId) ExtractTraceInfo(IEffectContext ctx)
        {
            return (ctx?.SourceActorId ?? 0, ctx?.TargetActorId ?? 0);
        }

        /// <summary>
        /// 提取溯源信息（从 object payload）
        /// </summary>
        private (int sourceActorId, int targetActorId) ExtractTraceInfoFromPayload(object payload)
        {
            if (payload is IEffectContext effectCtx)
            {
                return (effectCtx.SourceActorId, effectCtx.TargetActorId);
            }
            return (0, 0);
        }

        /// <summary>
        /// 创建效果执行溯源根节点并记录 Action 子节点
        /// </summary>
        private TraceRootScope CreateEffectTraceRoot(int effectId, int triggerId, int sourceActorId, int targetActorId, EffectContextKind contextKind)
        {
            if (Trace == null) return default;

            var rootScope = Trace.CreateEffectRoot(
                effectConfigId: effectId,
                triggerPlanId: triggerId,
                sourceActorId: sourceActorId,
                targetActorId: targetActorId,
                contextKind: contextKind);

            _currentTraceRootId = rootScope.RootId;
            return rootScope;
        }

        /// <summary>
        /// 为 Plan 中的所有 Action 创建子节点（父子关系）
        /// </summary>
        private void CreateActionChildNodes(in TriggerPlan<object> plan, int sourceActorId, int targetActorId)
        {
            if (Trace == null || _currentTraceRootId == 0) return;
            if (plan.Actions == null || plan.Actions.Length == 0) return;

            _currentActionContextIds.Clear();
            foreach (var actionCall in plan.Actions)
            {
                var actionId = (int)actionCall.Id.Value;
                if (actionId == 0) continue;

                var childScope = Trace.CreateActionChild(
                    parentRootId: _currentTraceRootId,
                    actionId: actionId,
                    sourceActorId: sourceActorId,
                    targetActorId: targetActorId);

                _currentActionContextIds.Add(childScope.ContextId);
            }
        }

        /// <summary>
        /// 结束当前溯源链路
        /// </summary>
        private void EndCurrentTrace(int reason)
        {
            if (Trace == null || _currentTraceRootId == 0) return;

            // 结束所有子节点
            foreach (var childId in _currentActionContextIds)
            {
                Trace.End(childId, reason);
            }
            _currentActionContextIds.Clear();

            // 结束根节点
            Trace.EndRoot(_currentTraceRootId, reason);
            _currentTraceRootId = 0;
        }

        /// <summary>
        /// 初始化 Plan Actions 注册
        /// 由 InstallPlanTriggering 在 World 启动时统一调用
        /// </summary>
        public void InitializePlanActions()
        {
            if (_planDb == null || _planActions == null)
            {
                Log.Warning("[MobaEffectExecutionService] InitializePlanActions: skipped. _planDb or _planActions is null");
                return;
            }

            Log.Info("[MobaEffectExecutionService] InitializePlanActions: starting...");

            // Register debug_log action
            try
            {
                var debugLogId = new ActionId(StableStringId.Get("action:debug_log"));
                _planActions.Register<Action0<object, IWorldResolver>>(
                    debugLogId,
                    static (args, ctx) =>
                    {
                        var ctxType = ctx.Context != null ? ctx.Context.GetType().Name : "<null>";
                        var argsType = args != null ? args.GetType().Name : "<null>";
                        Log.Info($"[Plan] debug_log executed. argsType={argsType}, ctxType={ctxType}");
                    },
                    isDeterministic: true);

                _planActions.Register<Action2<object, IWorldResolver>>(
                    debugLogId,
                    static (args, namedArgs, ctx) =>
                    {
                        var a0 = ExtractArg(namedArgs, "_0");
                        var a1 = ExtractArg(namedArgs, "_1");
                        var msgId = (int)a0;
                        var dump = a1 >= 0.5;
                        var msg = string.Empty;
                        if (ctx.Context != null && ctx.Context.TryResolve<TriggerPlanJsonDatabase>(out var db) && db != null)
                        {
                            if (!db.TryGetString(msgId, out msg)) msg = string.Empty;
                        }

                        Log.Info($"[Plan] debug_log: {msg}");
                        if (dump)
                        {
                            var ctxType = ctx.Context != null ? ctx.Context.GetType().Name : "<null>";
                            var argsType = args != null ? args.GetType().Name : "<null>";
                            Log.Info($"[Plan] debug_log dump. argsType={argsType}, ctxType={ctxType}");
                        }
                    },
                    isDeterministic: true);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "[MobaEffectExecutionService] InitializePlanActions: register debug_log action failed");
            }

            // Compatibility guard: stubs are registered before real modules so older positional
            // actions can survive migration. New actions must use PlanActionModuleRegistry.
            try
            {
                RegisterStubActionsFromPlans(_planDb, _planActions);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "[MobaEffectExecutionService] InitializePlanActions: RegisterStubActionsFromPlans failed");
            }

            // Register real action modules (override stubs)
            try
            {
                if (_services != null
                    && _services.TryResolve<AbilityKit.Demo.Moba.Systems.PlanActionModuleRegistry>(out var registry)
                    && registry != null
                    && registry.Modules != null)
                {
                    var modules = registry.Modules;
                    for (int i = 0; i < modules.Length; i++)
                    {
                        var m = modules[i];
                        if (m == null) continue;
                        try { m.Register(_planActions, _services); }
                        catch (Exception ex) { Log.Exception(ex, $"[MobaEffectExecutionService] InitializePlanActions: PlanActionModule register failed. module={m.GetType().Name}"); }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "[MobaEffectExecutionService] InitializePlanActions: register PlanActionModules failed");
            }

            Log.Info("[MobaEffectExecutionService] InitializePlanActions: completed");
        }

        // Compatibility repair for worlds initialized before all action modules are ready.
        // Keep this path defensive; do not use it as the normal extension point.
        private void TryRepairMissingActions()
        {
            if (_planDb == null || _planActions == null) return;

            try
            {
                RegisterStubActionsFromPlans(_planDb, _planActions);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "[MobaEffectExecutionService] TryRepairMissingActions: RegisterStubActionsFromPlans failed");
            }

            try
            {
                if (_services != null
                    && _services.TryResolve<AbilityKit.Demo.Moba.Systems.PlanActionModuleRegistry>(out var registry)
                    && registry != null
                    && registry.Modules != null)
                {
                    var modules = registry.Modules;
                    for (int i = 0; i < modules.Length; i++)
                    {
                        var m = modules[i];
                        if (m == null) continue;
                        try { m.Register(_planActions, _services); }
                        catch (Exception ex) { Log.Exception(ex, $"[MobaEffectExecutionService] TryRepairMissingActions: PlanActionModule register failed. module={m.GetType().Name}"); }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "[MobaEffectExecutionService] TryRepairMissingActions: register PlanActionModules failed");
            }
        }

        // Compatibility repair for a single legacy plan loaded without complete registrations.
        private void TryRepairMissingActions(in AbilityKit.Triggering.Runtime.Plan.TriggerPlan<object> plan)
        {
            if (_planActions == null) return;

            try
            {
                RegisterStubActionsFromPlan(in plan, _planActions);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "[MobaEffectExecutionService] TryRepairMissingActions(plan): RegisterStubActionsFromPlan failed");
            }

            try
            {
                if (_services != null
                    && _services.TryResolve<AbilityKit.Demo.Moba.Systems.PlanActionModuleRegistry>(out var registry)
                    && registry != null
                    && registry.Modules != null)
                {
                    var modules = registry.Modules;
                    for (int i = 0; i < modules.Length; i++)
                    {
                        var m = modules[i];
                        if (m == null) continue;
                        try { m.Register(_planActions, _services); }
                        catch (Exception ex) { Log.Exception(ex, $"[MobaEffectExecutionService] TryRepairMissingActions(plan): PlanActionModule register failed. module={m.GetType().Name}"); }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "[MobaEffectExecutionService] TryRepairMissingActions(plan): register PlanActionModules failed");
            }
        }

        private static void RegisterStubActionsFromPlan(in AbilityKit.Triggering.Runtime.Plan.TriggerPlan<object> plan, ActionRegistry actions)
        {
            if (actions == null) return;

            var calls = plan.Actions;
            if (calls == null || calls.Length == 0) return;

            for (int i = 0; i < calls.Length; i++)
            {
                var call = calls[i];
                var actionId = call.Id;
                if (actionId.Value == 0) continue;

                var hasNamedArgs = call.HasNamedArgs;
                if (hasNamedArgs)
                {
                    // 具名参数模式的 Action 不注册 stub
                    // 因为 PlanActionModule 会注册正确类型的 NamedAction<TArgs> 委托
                    // 注册 stub 会导致类型不匹配
                    continue;
                }

                // 注册传统 Action stub（向后兼容）
                var arity = call.Arity;
                switch (arity)
                {
                    case 0:
                        actions.Register<Action0<object, IWorldResolver>>(actionId, static (args, ctx) => { }, true);
                        break;
                    case 1:
                        actions.Register<Action1<object, IWorldResolver>>(actionId, static (args, namedArgs, ctx) => { }, true);
                        break;
                    case 2:
                        actions.Register<Action2<object, IWorldResolver>>(actionId, static (args, namedArgs, ctx) => { }, true);
                        break;
                }
            }
        }

        private bool TryExecutePlanByTriggerId(int triggerId, object args)
        {
            if (triggerId <= 0) return false;
            if (_planDb == null) return false;
            if (!_planDb.TryGetPlanByTriggerId(triggerId, out var plan))
            {
                return false;
            }

            if (_planEventBus == null || _planFunctions == null || _planActions == null)
            {
                Log.Warning($"[MobaEffectExecutionService] Plan runtime deps missing; skip plan exec. triggerId={triggerId}");
                return false;
            }

            var ctrl = new AbilityKit.Triggering.Runtime.ExecutionControl();
            ctrl.Reset();

            var execCtx = new AbilityKit.Triggering.Runtime.ExecCtx<IWorldResolver>(
                context: _services,
                eventBus: _planEventBus,
                functions: _planFunctions,
                actions: _planActions,
                blackboards: null,
                payloads: null,
                idNames: null,
                numericDomains: null,
                numericFunctions: null,
                policy: default,
                control: ctrl);

            bool ExecuteOnce()
            {
                var planned = new PlannedTrigger<object, IWorldResolver>(plan);
                var ok = planned.Evaluate(args, execCtx);
                if (ctrl.StopPropagation || ctrl.Cancel) return ok;
                if (!ok) return true;
                planned.Execute(args, execCtx);
                return true;
            }

            try
            {
                return ExecuteOnce();
            }
            catch (InvalidOperationException)
            {
                // Common cause: actions not registered yet due to init timing.
                // Attempt one-time repair and retry.
                try
                {
                    TryRepairMissingActions(in plan);
                    return ExecuteOnce();
                }
                catch (Exception ex2)
                {
                    Log.Exception(ex2, $"[MobaEffectExecutionService] Plan execution failed. triggerId={triggerId}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex, $"[MobaEffectExecutionService] Plan execution failed. triggerId={triggerId}");
                return false;
            }
        }

        private static void RegisterStubActionsFromPlans(TriggerPlanJsonDatabase db, ActionRegistry actions)
        {
            if (db == null || actions == null) return;

            var arityById = new System.Collections.Generic.Dictionary<int, byte>();
            var records = db.Records;
            if (records == null) return;

            for (int i = 0; i < records.Count; i++)
            {
                var plan = records[i].Plan;
                var calls = plan.Actions;
                if (calls == null) continue;

                for (int j = 0; j < calls.Length; j++)
                {
                    var call = calls[j];
                    var id = call.Id.Value;
                    if (id == 0) continue;

                    if (arityById.TryGetValue(id, out var existing))
                    {
                        if (existing != call.Arity) arityById[id] = byte.MaxValue;
                    }
                    else
                    {
                        arityById[id] = call.Arity;
                    }
                }
            }

            foreach (var kv in arityById)
            {
                var actionId = new ActionId(kv.Key);
                var arity = kv.Value;
                if (arity == byte.MaxValue) continue;

                switch (arity)
                {
                    case 0:
                        actions.Register<Action0<object, IWorldResolver>>(actionId, static (args, ctx) => { }, true);
                        break;
                    case 1:
                        actions.Register<Action1<object, IWorldResolver>>(actionId, static (args, namedArgs, ctx) => { }, true);
                        break;
                    case 2:
                        actions.Register<Action2<object, IWorldResolver>>(actionId, static (args, namedArgs, ctx) => { }, true);
                        break;
                }
            }
        }

        public void Execute(int effectId, IAbilityPipelineContext context, EffectExecuteMode mode = EffectExecuteMode.InternalOnly)
        {
            if (effectId <= 0) return;
            if (context == null) return;

            var wrappedContext = EffectContextWrapper.Wrap(context);
            if (wrappedContext == null) return;

            if (mode == EffectExecuteMode.PublishEventOnly || mode == EffectExecuteMode.InternalThenPublishEvent)
            {
                Log.Warning($"[MobaEffectExecutionService] EffectExecuteMode.{mode} is not supported (legacy publish removed). effectId={effectId}");
            }

            // 提取溯源信息
            var effectCtx = (IEffectContext)wrappedContext;
            var (sourceActorId, targetActorId) = ExtractTraceInfo(effectCtx);
            var contextKind = effectCtx.Kind;

            // 尝试获取 TriggerPlan 以便创建 Action 子节点
            TriggerPlan<object> plan = default;
            if (_planDb != null)
            {
                _planDb.TryGetPlanByTriggerId(effectId, out plan);
            }

            // 创建效果执行溯源根节点
            var rootScope = CreateEffectTraceRoot(effectId, effectId, sourceActorId, targetActorId, contextKind);

            // 为 Plan 中的所有 Action 创建子节点（父子关系）
            if (plan.Actions != null && plan.Actions.Length > 0)
            {
                CreateActionChildNodes(in plan, sourceActorId, targetActorId);
            }

            // 执行计划
            bool executed = TryExecutePlanByTriggerId(effectId, wrappedContext);

            // 结束溯源链路
            EndCurrentTrace(executed ? 0 : -1);
        }

        /// <summary>
        /// 通过 triggerId 直接执行触发计划
        /// 用于 Projectile hit、Area enter/exit、Buff interval 等场景
        /// </summary>
        public void ExecuteTriggerId(int triggerId, object payload)
        {
            if (triggerId <= 0) return;

            // 提取溯源信息
            var (sourceActorId, targetActorId) = ExtractTraceInfoFromPayload(payload);

            // 尝试获取 TriggerPlan 以便创建 Action 子节点
            TriggerPlan<object> plan = default;
            if (_planDb != null)
            {
                _planDb.TryGetPlanByTriggerId(triggerId, out plan);
            }

            // 创建效果执行溯源根节点
            var rootScope = CreateEffectTraceRoot(0, triggerId, sourceActorId, targetActorId, EffectContextKind.Unknown);

            // 为 Plan 中的所有 Action 创建子节点（父子关系）
            if (plan.Actions != null && plan.Actions.Length > 0)
            {
                CreateActionChildNodes(in plan, sourceActorId, targetActorId);
            }

            // 执行计划
            bool executed = TryExecutePlanByTriggerId(triggerId, payload);

            // 结束溯源链路
            EndCurrentTrace(executed ? 0 : -1);
        }

        public void Dispose()
        {
        }
    }
}
