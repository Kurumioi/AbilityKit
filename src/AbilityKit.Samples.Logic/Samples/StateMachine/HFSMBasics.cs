using System;
using UnityHFSM;
using AbilityKit.Samples.Abstractions;

namespace AbilityKit.Samples.Logic.Samples.StateMachine
{
    /// <summary>
    /// HFSMBasics - HFSM 基础示例
    /// 演示 UnityHFSM 的核心概念和使用方法
    /// </summary>
    [Sample]
    public sealed class HFSMBasics : SampleBase
    {
        public override string Title => "HFSM Basics";
        public override string Description => "UnityHFSM 核心概念与基础用法";
        public override SampleCategory Category => SampleCategory.StateMachine;

        protected override void OnRun()
        {
            Log("=== UnityHFSM 基础示例 ===");
            Output.Divider();

            // 1. 核心概念
            Log("【1】核心概念");
            Output.Bullet("StateMachine - 状态机容器，管理所有状态和转换");
            Output.Bullet("State - 状态，执行具体逻辑");
            Output.Bullet("Transition - 转换，定义状态之间的切换规则");
            Output.Bullet("Hierarchy - 层次，支持嵌套状态机");
            Log("");

            // 2. 状态生命周期
            Log("【2】状态生命周期");
            Output.Bullet("OnEnter() - 进入状态时调用");
            Output.Bullet("OnLogic() - 每帧逻辑更新");
            Output.Bullet("OnExit() - 离开状态时调用");
            Output.Bullet("OnExitRequest() - 请求退出时调用（当 needsExitTime = true）");
            Log("");

            // 3. 创建基本状态机
            Log("【3】创建基本状态机");
            var fsm = new StateMachine<string, string, string>();

            // 使用 Lambda 表达式创建状态
            fsm.AddState("Idle", new State(
                onEnter: s => Log("[Idle] 进入空闲状态"),
                onLogic: s => Log("[Idle] 等待中..."),
                onExit: s => Log("[Idle] 离开空闲状态")
            ));

            fsm.AddState("Move", new State(
                onEnter: s => Log("[Move] 进入移动状态"),
                onLogic: s => Log("[Move] 移动中..."),
                onExit: s => Log("[Move] 停止移动")
            ));

            Log("已添加 Idle 和 Move 状态");
            Log("");

            // 4. 添加转换
            Log("【4】添加转换");
            fsm.AddTransition(new Transition<string>(
                from: "Idle",
                to: "Move",
                condition: t => true, // 条件永远为真
                onTransition: t => Log("[转换] Idle -> Move")
            ));

            fsm.AddTransition(new Transition<string>(
                from: "Move",
                to: "Idle",
                condition: t => true,
                onTransition: t => Log("[转换] Move -> Idle")
            ));

            Log("已添加双向转换");
            Log("");

            // 5. 设置初始状态并初始化
            Log("【5】设置初始状态并初始化");
            fsm.SetStartState("Idle");
            fsm.Init();
            Log($"初始状态: {fsm.ActiveStateName}");
            Log("");

            // 6. 运行状态机
            Log("【6】运行状态机");
            Log("--- 第 1 帧 ---");
            fsm.OnLogic();
            Log("");

            Log("--- 第 2 帧（触发转换）---");
            fsm.OnLogic();
            Log($"当前状态: {fsm.ActiveStateName}");
            Log("");

            // 7. 带条件的转换
            Log("【7】带条件的转换");
            var conditionalFsm = CreateConditionalFsm();
            Log("已创建带条件的状态机");
            Log("");

            Log("初始状态:");
            conditionalFsm.OnLogic();
            Log("");

            Log("--- 触发转换 ---");
            conditionalFsm.OnLogic();
            Log($"当前状态: {conditionalFsm.ActiveStateName}");
            Log("");

            // 8. 延迟转换
            Log("【8】延迟转换（TransitionAfter）");
            var delayedFsm = CreateDelayedFsm();
            Log("已创建带延迟转换的状态机");
            Log("");

            Log("初始状态:");
            delayedFsm.OnLogic();
            Log("");

            Log("--- 帧 2 ---");
            delayedFsm.OnLogic();
            Log("");

            Log("--- 帧 3 (应该转换) ---");
            delayedFsm.OnLogic();
            Log($"当前状态: {delayedFsm.ActiveStateName}");
            Log("");

            Output.Divider();

            Log("【9】总结");
            Output.Bullet("StateMachine<TStateId, TEvent> - 可指定状态ID和事件类型");
            Output.Bullet("State - 带生命周期回调的状态类");
            Output.Bullet("Transition - 条件触发的状态转换");
            Output.Bullet("TransitionAfter - 延迟触发的状态转换");
        }

        /// <summary>
        /// 创建带条件的状态机
        /// </summary>
        private StateMachine<string, string, string> CreateConditionalFsm()
        {
            var fsm = new StateMachine<string, string, string>();

            fsm.AddState("Idle", new State(
                onEnter: s => Log("[Idle] 进入空闲"),
                onLogic: s => Log("[Idle] 等待...")
            ));

            fsm.AddState("Move", new State(
                onEnter: s => Log("[Move] 开始移动"),
                onLogic: s => Log("[Move] 移动中...")
            ));

            // 带条件的转换 - 条件永远为 true，所以会立即转换
            fsm.AddTransition(new Transition<string>(
                from: "Idle",
                to: "Move",
                condition: t => true,
                onTransition: t => Log("[条件转换] Idle -> Move")
            ));

            fsm.SetStartState("Idle");
            fsm.Init();

            return fsm;
        }

        /// <summary>
        /// 创建带延迟转换的状态机
        /// </summary>
        private StateMachine<string, string, string> CreateDelayedFsm()
        {
            var fsm = new StateMachine<string, string, string>();

            fsm.AddState("Preparing", new State(
                onEnter: s => Log("[Preparing] 准备中..."),
                onLogic: s => Log("[Preparing] 准备倒计时...")
            ));

            fsm.AddState("Ready", new State(
                onEnter: s => Log("[Ready] 准备完成！"),
                onLogic: s => Log("[Ready] 就绪...")
            ));

            // 延迟 2 秒后自动转换
            fsm.AddTransition(new TransitionAfter<string>(
                from: "Preparing",
                to: "Ready",
                delay: 2.0f,
                onTransition: t => Log("[延迟转换] Preparing -> Ready (延迟 2 秒)")
            ));

            fsm.SetStartState("Preparing");
            fsm.Init();

            return fsm;
        }
    }
}
