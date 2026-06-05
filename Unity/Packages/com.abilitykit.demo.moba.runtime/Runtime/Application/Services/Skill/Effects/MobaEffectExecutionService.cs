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
        private MobaTriggerPlanExecutor _planExecutor;
 
        /// <summary>
        /// 溯源注册表（可选，用于链路追踪）。未注册时只保留核心 lineage，不创建 trace tree。
        /// </summary>
        [WorldInject(required: false)]
        public MobaTraceRegistry Trace { get; private set; }

        /// <summary>
        /// 当前正在执行的可选 trace 栈（用于嵌套效果和 Action 父子关系追踪）
        /// </summary>
        private readonly Stack<EffectExecutionTraceScope> _traceScopes = new Stack<EffectExecutionTraceScope>();
        private readonly Stack<MobaCombatExecutionContext> _executionContexts = new Stack<MobaCombatExecutionContext>();

        /// <summary>
        /// 获取当前正在追踪的 Action 链路
        /// </summary>
        public IReadOnlyList<long> CurrentActionChain => _traceScopes.Count > 0 ? _traceScopes.Peek().ActionContextIds : Array.Empty<long>();

        public long CurrentEffectContextId => _traceScopes.Count > 0 ? _traceScopes.Peek().EffectContextId : 0;

        public bool TryGetCurrentExecutionContext(out MobaCombatExecutionContext context)
        {
            context = default;
            if (_executionContexts.Count == 0) return false;

            context = _executionContexts.Peek();
            return context.IsValid;
        }

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

            RegisterPlanActionModules("InitializePlanActions");
        }

        private void RegisterPlanActionModules(string caller)
        {
            if (_planActions == null)
            {
                Log.Warning($"[MobaEffectExecutionService] {caller}: PlanActionModule register skipped. _planActions is null");
                return;
            }

            if (_services == null)
            {
                Log.Warning($"[MobaEffectExecutionService] {caller}: PlanActionModule register skipped. _services is null");
                return;
            }

            try
            {
                if (!_services.TryResolve<AbilityKit.Demo.Moba.Services.Triggering.PlanActions.PlanActionModuleRegistry>(out var registry) || registry == null)
                {
                    Log.Warning($"[MobaEffectExecutionService] {caller}: PlanActionModuleRegistry not resolved");
                    return;
                }

                if (registry.Modules == null)
                {
                    Log.Warning($"[MobaEffectExecutionService] {caller}: PlanActionModuleRegistry.Modules is null");
                    return;
                }

                var modules = registry.Modules;
                for (int i = 0; i < modules.Length; i++)
                {
                    var m = modules[i];
                    if (m == null)
                    {
                        Log.Warning($"[MobaEffectExecutionService] {caller}: PlanActionModule null. index={i}");
                        continue;
                    }

                    try
                    {
                        m.Register(_planActions, _services);
                    }
                    catch (Exception ex)
                    {
                        Log.Exception(ex, $"[MobaEffectExecutionService] {caller}: PlanActionModule register failed. module={m.GetType().Name}");
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
            var executionContext = CreateCombatExecutionContext(payload, 0, 0);
            return CreateConditionContext(in executionContext);
        }

        private MobaTriggerConditionContext CreateConditionContext(in MobaCombatExecutionContext executionContext)
        {
            var frame = executionContext.Frame != 0 ? executionContext.Frame : CurrentBudgetFrame;
            var snapshot = executionContext.ExecutionSnapshot.WithFrame(frame);
            var payload = executionContext.Payload;
            var lineageInput = executionContext.LineageInput;
            if (_payloadResolvers != null && _payloadResolvers.TryCreateContext(payload, in lineageInput, in snapshot, _skillRuntimes, frame, out var context))
            {
                return context;
            }

            var normalizedContext = MobaCombatExecutionContextFactory.WithSnapshot(in executionContext, in snapshot, frame);
            return MobaTriggerConditionContext.Create(in normalizedContext, _skillRuntimes, frame);
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

        private MobaCombatExecutionContext CreateCombatExecutionContext(object payload, int triggerId, int configId)
        {
            var lineageInput = MobaEffectLineageInputResolver.Resolve(payload);
            var executionSnapshot = CreateExecutionSnapshot(payload, in lineageInput, triggerId, configId);
            return MobaCombatExecutionContextFactory.Create(payload, in lineageInput, in executionSnapshot, executionSnapshot.Frame != 0 ? executionSnapshot.Frame : CurrentBudgetFrame);
        }

        private bool TryEnterExecutionBudget(int triggerId, in MobaCombatExecutionContext executionContext, out MobaTriggerExecutionBudgetToken token, out MobaTriggerConditionContext conditionContext)
        {
            conditionContext = CreateConditionContext(in executionContext);
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

        private MobaEffectExecutionSession BeginExecutionSession(
            int effectConfigId,
            int triggerId,
            in MobaCombatExecutionContext executionContext,
            in MobaEffectLineageInput lineageInput,
            in TriggerPlan<object> plan,
            in MobaTriggerExecutionBudgetToken budgetToken)
        {
            EffectExecutionTraceScope traceScope = null;
            _executionContexts.Push(executionContext);
            try
            {
                traceScope = BeginEffectTraceScope(effectConfigId, triggerId, in lineageInput);
                if (plan.Actions != null && plan.Actions.Length > 0)
                {
                    CreateActionChildNodes(in plan, lineageInput.SourceActorId, lineageInput.TargetActorId);
                }

                return new MobaEffectExecutionSession(this, traceScope, budgetToken);
            }
            catch
            {
                if (traceScope != null)
                {
                    EndCurrentTrace((int)TraceLifecycleReason.Failed);
                }

                if (_executionContexts.Count > 0)
                {
                    _executionContexts.Pop();
                }

                _executionBudget.Exit(in budgetToken);
                throw;
            }
        }

        private sealed class MobaEffectExecutionSession : IDisposable
        {
            private readonly MobaEffectExecutionService _owner;
            private readonly MobaTriggerExecutionBudgetToken _budgetToken;
            private EffectExecutionTraceScope _traceScope;
            private bool _disposed;

            public MobaEffectExecutionSession(
                MobaEffectExecutionService owner,
                EffectExecutionTraceScope traceScope,
                in MobaTriggerExecutionBudgetToken budgetToken)
            {
                _owner = owner;
                _traceScope = traceScope;
                _budgetToken = budgetToken;
            }

            public void Complete(bool executed)
            {
                if (_traceScope == null) return;

                _owner.EndCurrentTrace(ToTraceEndReason(executed));
                _traceScope = null;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;

                if (_traceScope != null)
                {
                    _owner.EndCurrentTrace((int)TraceLifecycleReason.Failed);
                    _traceScope = null;
                }

                if (_owner._executionContexts.Count > 0)
                {
                    _owner._executionContexts.Pop();
                }

                _owner._executionBudget.Exit(in _budgetToken);
            }
        }

        private MobaTriggerPlanExecutor PlanExecutor
        {
            get
            {
                if (_planExecutor == null)
                {
                    _planExecutor = new MobaTriggerPlanExecutor(
                        _services,
                        _planDb,
                        _planEventBus,
                        _planFunctions,
                        _planActions);
                }

                return _planExecutor;
            }
        }

        private bool TryGetPlanByTriggerId(int triggerId, out TriggerPlan<object> plan)
        {
            return PlanExecutor.TryGetPlan(triggerId, out plan);
        }

        private bool TryExecutePlanByTriggerId(int triggerId, object args)
        {
            return PlanExecutor.Execute(triggerId, args);
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

            var executionContext = CreateCombatExecutionContext(wrappedContext, effectId, effectId);
            var lineageInput = executionContext.LineageInput;

            var hasPlan = TryGetPlanByTriggerId(effectId, out var plan);
            if (!hasPlan)
            {
                Log.Warning($"[MobaEffectExecutionService] Missing trigger plan. effectId={effectId}, source={lineageInput.SourceActorId}, target={lineageInput.TargetActorId}, kind={lineageInput.ContextKind}");
            }

            if (!TryEnterExecutionBudget(effectId, in executionContext, out var budgetToken, out var conditionContext)) return;

            using (var session = BeginExecutionSession(effectId, effectId, in executionContext, in lineageInput, in plan, in budgetToken))
            {
                var conditionsPassed = EvaluateTriggerConditions(effectId, in conditionContext);
                var planExecuted = conditionsPassed && TryExecutePlanByTriggerId(effectId, wrappedContext);
                session.Complete(planExecuted);
            }
        }

        /// <summary>
        /// 通过 triggerId 直接执行触发计划
        /// 用于 Projectile hit、Area enter/exit、Buff interval 等场景
        /// </summary>
        public void ExecuteTriggerId(int triggerId, object payload)
        {
            if (triggerId <= 0) return;

            var executionContext = CreateCombatExecutionContext(payload, triggerId, triggerId);
            var lineageInput = executionContext.LineageInput;

            var hasPlan = TryGetPlanByTriggerId(triggerId, out var plan);
            if (!hasPlan)
            {
                Log.Warning($"[MobaEffectExecutionService] Missing trigger plan. triggerId={triggerId}, source={lineageInput.SourceActorId}, target={lineageInput.TargetActorId}, kind={lineageInput.ContextKind}");
            }

            if (!TryEnterExecutionBudget(triggerId, in executionContext, out var budgetToken, out var conditionContext)) return;

            using (var session = BeginExecutionSession(triggerId, triggerId, in executionContext, in lineageInput, in plan, in budgetToken))
            {
                var conditionsPassed = EvaluateTriggerConditions(triggerId, in conditionContext);
                var planExecuted = conditionsPassed && TryExecutePlanByTriggerId(triggerId, payload);
                session.Complete(planExecuted);
            }
        }

        public void Dispose()
        {
        }
    }
}
