using System;

namespace AbilityKit.Game.Flow
{
    /// <summary>
    /// 从 <c>GameFlowDomain</c> 提取的 Flow 条件上下文构建器（Step 4.7d）。
    /// 负责构建 <c>MobaFlowConditionContext</c> 并求值转移条件。
    /// 纯逻辑，零 Unity 依赖，可独立测试。
    /// </summary>
    internal sealed class FlowConditionContextBuilder
    {
        /// <summary>
        /// FlowConditionContextBuilder 需要的回调集合，由 Domain 提供。
        /// </summary>
        internal sealed class Callbacks
        {
            /// <summary>获取 _battleRequested 标志。</summary>
            public Func<bool> GetBattleRequested { get; set; }
        }

        private readonly Callbacks _callbacks;
        private readonly BattleWorldScopeHost _battleWorldScope;
        private readonly MobaFlowConditionResolver _conditionResolver;

        // 准入 gate 的兜底来源：scope 尚未建立时（Boot/Lobby 阶段求值）用它，四项全 true。
        private static readonly IFlowGateProvider DefaultGates = new DefaultFlowGateProvider();

        internal FlowConditionContextBuilder(
            Callbacks callbacks,
            BattleWorldScopeHost battleWorldScope,
            MobaFlowConditionResolver conditionResolver)
        {
            _callbacks = callbacks;
            _battleWorldScope = battleWorldScope;
            _conditionResolver = conditionResolver;
        }

        internal bool EvaluateRootTransitionCondition(string conditionId)
        {
            var ctx = BuildFlowConditionContext();
            return _conditionResolver.Evaluate(conditionId, in ctx);
        }

        internal MobaFlowConditionContext BuildFlowConditionContext()
        {
            // 四个准入 gate 从 per-battle scope 的 IFlowGateProvider 取（Step 3）。
            // 转移求值可能发生在 scope 尚未建立时（如 Boot/Lobby 阶段轮询），此时取回落空。
            // 用 DefaultFlowGateProvider 兜底——四项全 true，与迁移前四个硬编码 return true 行为等价。
            var gates = _battleWorldScope.TryResolve<IFlowGateProvider>(out var provider)
                ? provider
                : DefaultGates;

            return new MobaFlowConditionContext(
                battleRequested: _callbacks.GetBattleRequested(),
                authenticated: gates.IsAuthenticated,
                roomReady: gates.IsRoomReady,
                connectivityReady: gates.IsConnectivityReady,
                assetsReady: gates.IsAssetsReady);
        }
    }
}
