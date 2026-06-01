using System;
using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
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
        [WorldInject] private IWorldResolver _services;
        [WorldInject] private TriggerPlanJsonDatabase _planDb;
        [WorldInject] private AbilityKit.Triggering.Runtime.TriggerRunner<IWorldResolver> _planRunner;
        [WorldInject] private AbilityKit.Triggering.Eventing.IEventBus _planEventBus;
        [WorldInject] private FunctionRegistry _planFunctions;
        [WorldInject] private ActionRegistry _planActions;
        [WorldInject(required: false)] private IFrameTime _frameTime;
        [WorldInject(required: false)] private MobaSkillCastRuntimeService _skillRuntimes;
        [WorldInject(required: false)] private MobaTriggerPayloadResolverRegistry _payloadResolvers;
        [WorldInject(required: false)] private MobaTriggerConditionRegistry _triggerConditions;

        private readonly MobaTriggerExecutionBudget _executionBudget = new MobaTriggerExecutionBudget();
        private int _fallbackBudgetFrame;
 
        /// <summary>
        /// 溯源注册表（可选，用于链路追踪）。未注册时只保留核心 lineage，不创建 trace tree。
        /// </summary>
        [WorldInject(required: false)]
        public MobaTraceRegistry Trace { get; private set; }

        /// <summary>
        /// 当前正在执行的可选 trace 栈（用于嵌套效果和 Action 父子关系追踪）
        /// </summary>
        private readonly Stack<EffectExecutionTraceScope> _traceScopes = new Stack<EffectExecutionTraceScope>();

        /// <summary>
        /// 获取当前正在追踪的 Action 链路
        /// </summary>
        public IReadOnlyList<long> CurrentActionChain => _traceScopes.Count > 0 ? _traceScopes.Peek().ActionContextIds : Array.Empty<long>();

        public long CurrentEffectContextId => _traceScopes.Count > 0 ? _traceScopes.Peek().EffectContextId : 0;

        public bool TryGetCurrentTraceScope(out MobaEffectTraceScopeSnapshot snapshot)
        {
            snapshot = default;
            if (_traceScopes.Count == 0) return false;

            var scope = _traceScopes.Peek();
            if (scope.EffectContextId == 0) return false;

            snapshot = new MobaEffectTraceScopeSnapshot(
                scope.EffectContextId,
                scope.EffectConfigId,
                scope.TriggerId,
                scope.SourceActorId,
                scope.TargetActorId,
                scope.IsRoot);
            return true;
        }

        /// <summary>
        /// 创建可选效果执行 trace 节点。存在父上下文时挂为子节点，否则创建根节点。
        /// </summary>
        private EffectExecutionTraceScope BeginEffectTraceScope(int effectConfigId, int triggerId, in MobaEffectLineageInput lineageInput)
        {
            if (Trace == null) return null;

            var configId = effectConfigId > 0 ? effectConfigId : triggerId;
            var parentContextId = lineageInput.ParentContextId;
            var scope = new EffectExecutionTraceScope
            {
                EffectConfigId = configId,
                TriggerId = triggerId,
                SourceActorId = lineageInput.SourceActorId,
                TargetActorId = lineageInput.TargetActorId,
            };

            if (parentContextId != 0)
            {
                scope.EffectContextId = Trace.CreateChildContext(
                    parentContextId,
                    MobaTraceKind.EffectExecution,
                    configId,
                    lineageInput.SourceActorId,
                    lineageInput.TargetActorId,
                    TraceEndpoint.Config("Effect", configId),
                    TraceEndpoint.Actor(lineageInput.TargetActorId));
                scope.IsRoot = false;
            }
            else
            {
                var rootScope = Trace.CreateEffectRoot(
                    effectConfigId: configId,
                    triggerPlanId: triggerId,
                    sourceActorId: lineageInput.SourceActorId,
                    targetActorId: lineageInput.TargetActorId,
                    contextKind: lineageInput.ContextKind);

                scope.EffectContextId = rootScope.RootId;
                scope.IsRoot = true;
            }

            if (scope.EffectContextId == 0) return null;
            _traceScopes.Push(scope);
            return scope;
        }

        /// <summary>
        /// 为 Plan 中的所有 Action 创建子节点（父子关系）
        /// </summary>
        private void CreateActionChildNodes(in TriggerPlan<object> plan, int sourceActorId, int targetActorId)
        {
            if (Trace == null || _traceScopes.Count == 0) return;
            if (plan.Actions == null || plan.Actions.Length == 0) return;

            var currentScope = _traceScopes.Peek();
            currentScope.ActionContextIds.Clear();
            foreach (var actionCall in plan.Actions)
            {
                var actionId = (int)actionCall.Id.Value;
                if (actionId == 0) continue;

                var childScope = Trace.CreateActionChild(
                    parentRootId: currentScope.EffectContextId,
                    actionId: actionId,
                    sourceActorId: sourceActorId,
                    targetActorId: targetActorId);

                currentScope.ActionContextIds.Add(childScope.ContextId);
            }
        }

        /// <summary>
        /// 结束当前溯源链路
        /// </summary>
        private void EndCurrentTrace(int reason)
        {
            if (Trace == null || _traceScopes.Count == 0) return;

            var scope = _traceScopes.Pop();
            foreach (var childId in scope.ActionContextIds)
            {
                Trace.End(childId, reason);
            }
            scope.ActionContextIds.Clear();

            if (scope.IsRoot)
            {
                Trace.EndRoot(scope.EffectContextId, reason);
            }
            else
            {
                Trace.End(scope.EffectContextId, reason);
            }
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
            RegisterPlanActionModules("InitializePlanActions");
            Log.Info("[MobaEffectExecutionService] InitializePlanActions: completed");
        }

        private void TryRepairMissingActions(in AbilityKit.Triggering.Runtime.Plan.TriggerPlan<object> plan)
        {
            RegisterPlanActionModules("TryRepairMissingActions(plan)");
        }

        private void RegisterPlanActionModules(string caller)
        {
            if (_planActions == null) return;

            try
            {
                if (_services != null
                    && _services.TryResolve<AbilityKit.Demo.Moba.Services.Triggering.PlanActions.PlanActionModuleRegistry>(out var registry)
                    && registry != null
                    && registry.Modules != null)
                {
                    var modules = registry.Modules;
                    for (int i = 0; i < modules.Length; i++)
                    {
                        var m = modules[i];
                        if (m == null) continue;
                        try { m.Register(_planActions, _services); }
                        catch (Exception ex) { Log.Exception(ex, $"[MobaEffectExecutionService] {caller}: PlanActionModule register failed. module={m.GetType().Name}"); }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex, $"[MobaEffectExecutionService] {caller}: register PlanActionModules failed");
            }
        }

        private int CurrentBudgetFrame
        {
            get
            {
                if (_frameTime != null) return _frameTime.Frame.Value;
                if (_executionBudget.CurrentDepth == 0) _fallbackBudgetFrame++;
                return _fallbackBudgetFrame;
            }
        }

        public MobaTriggerConditionContext CreateConditionContext(object payload)
        {
            var lineageInput = MobaEffectLineageInputResolver.Resolve(payload);
            var executionSnapshot = CreateExecutionSnapshot(payload, in lineageInput, 0, 0);
            return CreateConditionContext(payload, in lineageInput, in executionSnapshot);
        }

        private MobaTriggerConditionContext CreateConditionContext(object payload, in MobaEffectLineageInput lineageInput, in MobaTriggerExecutionSnapshot executionSnapshot)
        {
            var frame = executionSnapshot.Frame != 0 ? executionSnapshot.Frame : CurrentBudgetFrame;
            var snapshot = executionSnapshot.WithFrame(frame);
            if (_payloadResolvers != null && _payloadResolvers.TryCreateContext(payload, in lineageInput, in snapshot, _skillRuntimes, frame, out var context))
            {
                return context;
            }

            return MobaTriggerConditionContext.Create(payload, in lineageInput, in snapshot, _skillRuntimes, frame);
        }

        private MobaTriggerExecutionSnapshot CreateExecutionSnapshot(object payload, in MobaEffectLineageInput lineageInput, int triggerId, int configId)
        {
            return MobaTriggerExecutionSnapshotBuilder.Create()
                .FromLineage(in lineageInput)
                .FromPayload(payload)
                .WithTrigger(triggerId, configId != 0 ? configId : lineageInput.OriginConfigId)
                .WithFrameIfMissing(CurrentBudgetFrame)
                .Build();
        }

        private bool TryEnterExecutionBudget(int triggerId, object payload, in MobaEffectLineageInput lineageInput, in MobaTriggerExecutionSnapshot executionSnapshot, out MobaTriggerExecutionBudgetToken token, out MobaTriggerConditionContext conditionContext)
        {
            conditionContext = CreateConditionContext(payload, in lineageInput, in executionSnapshot);
            var request = conditionContext.ToExecutionRequest(triggerId);
            if (_executionBudget.TryEnter(in request, out token, out var block)) return true;

            Log.Warning($"[MobaEffectExecutionService] Trigger execution blocked. reason={block.Reason}, triggerId={triggerId}, frame={request.Frame}, depth={block.CurrentDepth}, frameCount={block.CurrentFrameCount}, rootCount={block.CurrentRootCount}, sameTriggerCount={block.CurrentSameTriggerCount}, rootContextId={request.RootContextId}, parentContextId={request.ParentContextId}, sourceActorId={request.SourceActorId}, targetActorId={request.TargetActorId}");
            return false;
        }

        private bool EvaluateTriggerConditions(int triggerId, in MobaTriggerConditionContext conditionContext)
        {
            if (_triggerConditions == null || !_triggerConditions.HasConditions(triggerId)) return true;

            var result = _triggerConditions.Evaluate(triggerId, in conditionContext);
            if (result.Passed) return true;

            Log.Warning($"[MobaEffectExecutionService] Trigger condition failed. triggerId={triggerId}, reason={result.Reason}, failureKey={result.FailureKey}, rootContextId={conditionContext.RootContextId}, sourceActorId={conditionContext.SourceActorId}, targetActorId={conditionContext.TargetActorId}");
            return false;
        }

        private static int ToTraceEndReason(bool executed)
        {
            return executed ? (int)TraceLifecycleReason.Completed : (int)TraceLifecycleReason.Failed;
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

            var effectCtx = (IEffectContext)wrappedContext;
            var lineageInput = MobaEffectLineageInputResolver.Resolve(effectCtx);

            TriggerPlan<object> plan = default;
            if (_planDb != null)
            {
                _planDb.TryGetPlanByTriggerId(effectId, out plan);
            }

            var executionSnapshot = CreateExecutionSnapshot(wrappedContext, in lineageInput, effectId, effectId);
            if (!TryEnterExecutionBudget(effectId, wrappedContext, in lineageInput, in executionSnapshot, out var budgetToken, out var conditionContext)) return;

            EffectExecutionTraceScope traceScope = null;
            try
            {
                traceScope = BeginEffectTraceScope(effectId, effectId, in lineageInput);
                if (plan.Actions != null && plan.Actions.Length > 0)
                {
                    CreateActionChildNodes(in plan, lineageInput.SourceActorId, lineageInput.TargetActorId);
                }
 
                bool executed = EvaluateTriggerConditions(effectId, in conditionContext) && TryExecutePlanByTriggerId(effectId, wrappedContext);
  
                if (traceScope != null)
                {
                    EndCurrentTrace(ToTraceEndReason(executed));
                    traceScope = null;
                }
            }
            finally
            {
                if (traceScope != null)
                {
                    EndCurrentTrace((int)TraceLifecycleReason.Failed);
                }

                _executionBudget.Exit(in budgetToken);
            }
        }

        /// <summary>
        /// 通过 triggerId 直接执行触发计划
        /// 用于 Projectile hit、Area enter/exit、Buff interval 等场景
        /// </summary>
        public void ExecuteTriggerId(int triggerId, object payload)
        {
            if (triggerId <= 0) return;

            var lineageInput = MobaEffectLineageInputResolver.Resolve(payload);

            TriggerPlan<object> plan = default;
            if (_planDb != null)
            {
                _planDb.TryGetPlanByTriggerId(triggerId, out plan);
            }

            var executionSnapshot = CreateExecutionSnapshot(payload, in lineageInput, triggerId, triggerId);
            if (!TryEnterExecutionBudget(triggerId, payload, in lineageInput, in executionSnapshot, out var budgetToken, out var conditionContext)) return;

            EffectExecutionTraceScope traceScope = null;
            try
            {
                traceScope = BeginEffectTraceScope(triggerId, triggerId, in lineageInput);
                if (plan.Actions != null && plan.Actions.Length > 0)
                {
                    CreateActionChildNodes(in plan, lineageInput.SourceActorId, lineageInput.TargetActorId);
                }
 
                bool executed = EvaluateTriggerConditions(triggerId, in conditionContext) && TryExecutePlanByTriggerId(triggerId, payload);
  
                if (traceScope != null)
                {
                    EndCurrentTrace(ToTraceEndReason(executed));
                    traceScope = null;
                }
            }
            finally
            {
                if (traceScope != null)
                {
                    EndCurrentTrace((int)TraceLifecycleReason.Failed);
                }

                _executionBudget.Exit(in budgetToken);
            }
        }

        public void Dispose()
        {
        }
    }
}
