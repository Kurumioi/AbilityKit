using System;
using System.Collections.Generic;
using AbilityKit.Ability.Behavior.BTree;
using BTCoreAction = BTCore.Runtime.Actions.Action;
using BTCoreCondition = BTCore.Runtime.Conditions.Condition;
using NodeState = BTCore.Runtime.NodeState;

namespace AbilityKit.Ability.Behavior
{
    /// <summary>
    /// BTCore 节点执行结果到 AbilityKit 决策结果的转换器
    /// </summary>
    public static class BTreeResultConverter
    {
        public static DecisionKind ToDecisionKind(NodeState nodeState)
        {
            return nodeState switch
            {
                NodeState.Success => DecisionKind.Complete,
                NodeState.Failure => DecisionKind.Interrupt,
                NodeState.Running => DecisionKind.Continue,
                _ => DecisionKind.Continue
            };
        }

        public static NodeState ToNodeState(DecisionKind kind)
        {
            return kind switch
            {
                DecisionKind.Complete => NodeState.Success,
                DecisionKind.Interrupt => NodeState.Failure,
                DecisionKind.ChangeState => NodeState.Running,
                DecisionKind.Continue => NodeState.Running,
                _ => NodeState.Failure
            };
        }
    }

    /// <summary>
    /// BTCore Action 节点的 AbilityKit 适配器
    /// </summary>
    public abstract class BTreeActionAdapter : BTCoreAction, IBehaviorDecision
    {
        public string DecisionType => GetType().Name;
        public string CurrentState { get; private set; } = "Running";

        protected IBehaviorContext Context { get; private set; }
        protected IWorldQuery WorldQuery { get; private set; }

        protected virtual void OnInit(IBehaviorContext context, IWorldQuery worldQuery) { }

        protected sealed override void OnStart()
        {
            base.OnStart();
            OnStartAdapter();
        }

        protected virtual void OnStartAdapter() { }

        protected sealed override NodeState OnUpdate()
        {
            if (Context == null)
                return NodeState.Failure;

            return OnUpdateAdapter();
        }

        protected virtual NodeState OnUpdateAdapter()
        {
            return NodeState.Success;
        }

        protected sealed override void OnStop()
        {
            OnStopAdapter();
            base.OnStop();
        }

        protected virtual void OnStopAdapter() { }

        public DecisionResult Decide(IBehaviorContext context, IWorldQuery world)
        {
            Context = context;
            WorldQuery = world;
            OnInit(context, world);

            var nodeState = OnUpdate();

            if (nodeState == NodeState.Running)
            {
                CurrentState = "Running";
                return DecisionResult.Continue(CurrentState);
            }

            if (nodeState == NodeState.Success)
            {
                CurrentState = "Success";
                return DecisionResult.Complete();
            }

            CurrentState = "Failure";
            return DecisionResult.Interrupt("BTreeActionFailed");
        }
    }

    /// <summary>
    /// BTCore Condition 节点的 AbilityKit 适配器
    /// </summary>
    public abstract class BTreeConditionAdapter : BTCoreCondition, IBehaviorDecision
    {
        public string DecisionType => GetType().Name;
        public string CurrentState { get; private set; } = "Condition";

        protected IBehaviorContext Context { get; private set; }
        protected IWorldQuery WorldQuery { get; private set; }

        protected sealed override void OnStart()
        {
            base.OnStart();
        }

        protected sealed override bool Validate()
        {
            if (Context == null)
                return false;
            return OnValidate();
        }

        protected virtual bool OnValidate()
        {
            return true;
        }

        public DecisionResult Decide(IBehaviorContext context, IWorldQuery world)
        {
            Context = context;
            WorldQuery = world;

            var result = Validate();
            CurrentState = result ? "Success" : "Failure";
            return result ? DecisionResult.Complete() : DecisionResult.Interrupt("ConditionFailed");
        }
    }

    /// <summary>
    /// BTCore 行为树执行器适配器
    /// </summary>
    public sealed class BTreeExecutorAdapter : IBehaviorExecutor
    {
        private readonly BTreeBlackboardBridge _blackboardBridge;

        public BTreeExecutorAdapter(BTreeBlackboardBridge blackboardBridge)
        {
            _blackboardBridge = blackboardBridge;
        }

        public void Execute(DecisionResult decision, IBehaviorContext context, IBehaviorOutput output)
        {
            _blackboardBridge.SyncStateToBlackboard(context.State);

            switch (decision.Kind)
            {
                case DecisionKind.Complete:
                    output.RequestComplete();
                    break;

                case DecisionKind.Interrupt:
                    output.RequestInterrupt(decision.InterruptReason ?? "Decision");
                    break;

                case DecisionKind.ChangeState:
                case DecisionKind.Continue:
                    var targetPos = decision.GetMoveTarget();
                    var targetEntity = decision.GetMoveTargetEntity();
                    var speed = decision.GetMoveSpeed(0f);

                    if (targetPos.HasValue || targetEntity.HasValue)
                    {
                        output.SetMovement(targetPos, targetEntity, speed);
                    }
                    break;
            }
        }
    }
}
