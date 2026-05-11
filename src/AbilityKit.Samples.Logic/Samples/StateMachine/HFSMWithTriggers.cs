using System;
using UnityHFSM;
using AbilityKit.Samples.Abstractions;

namespace AbilityKit.Samples.Logic.Samples.StateMachine
{
    /// <summary>
    /// HFSMWithTriggers - 演示使用触发器进行状态转换
    /// </summary>
    [Sample]
    public sealed class HFSMWithTriggers : SampleBase
    {
        public override string Title => "HFSM with Triggers";
        public override string Description => "使用触发器进行状态转换";
        public override SampleCategory Category => SampleCategory.StateMachine;

        protected override void OnRun()
        {
            Log("=== HFSM 触发器示例 ===");
            Output.Divider();

            // 1. 触发器概念
            Log("【1】触发器概念");
            Output.Bullet("触发器（Trigger）是一种事件驱动的转换机制");
            Output.Bullet("只有在触发器被激活时，才会检查对应的转换条件");
            Output.Bullet("适合处理用户输入、技能冷却等异步事件");
            Log("");

            // 2. 创建带触发器的状态机
            Log("【2】创建带触发器的状态机");
            var fsm = new StateMachine<string, string, string>();

            // 添加状态
            fsm.AddState("Idle", new State(
                onEnter: s => Log("[Idle] 进入空闲状态"),
                onLogic: s => Log("[Idle] 等待中...")
            ));

            fsm.AddState("Jump", new State(
                onEnter: s => Log("[Jump] 开始跳跃！"),
                onLogic: s => Log("[Jump] 上升中..."),
                onExit: s => Log("[Jump] 落地")
            ));

            fsm.AddState("Attack", new State(
                onEnter: s => Log("[Attack] 发动攻击！"),
                onLogic: s => Log("[Attack] 攻击进行中..."),
                onExit: s => Log("[Attack] 攻击结束")
            ));

            // 添加普通转换（条件触发）
            fsm.AddTransition(new Transition<string>(
                from: "Idle",
                to: "Jump",
                condition: t => true,
                onTransition: t => Log("[转换] Idle -> Jump")
            ));

            fsm.AddTransition(new Transition<string>(
                from: "Jump",
                to: "Idle",
                condition: t => true,
                onTransition: t => Log("[转换] Jump -> Idle")
            ));

            // 添加触发器转换
            fsm.AddTriggerTransition("OnAttack", new Transition<string>(
                from: "Idle",
                to: "Attack",
                onTransition: t => Log("[触发器] 激活 Attack 转换")
            ));

            fsm.AddTriggerTransition("OnAttack", new Transition<string>(
                from: "Jump",
                to: "Attack",
                onTransition: t => Log("[触发器] 跳跃中发动攻击")
            ));

            fsm.AddTriggerTransition("OnAttack", new Transition<string>(
                from: "Attack",
                to: "Idle",
                onTransition: t => Log("[触发器] 攻击结束返回 Idle")
            ));

            fsm.SetStartState("Idle");
            fsm.Init();

            Log("状态机已初始化，初始状态: " + fsm.ActiveStateName);
            Log("");

            // 3. 正常逻辑更新
            Log("【3】正常逻辑更新（无触发器）");
            Log("--- OnLogic() ---");
            fsm.OnLogic();
            Log("");

            // 4. 触发 OnAttack 触发器
            Log("【4】触发 OnAttack 触发器");
            Log("--- Trigger(\"OnAttack\") ---");
            fsm.Trigger("OnAttack");
            Log($"当前状态: {fsm.ActiveStateName}");
            Log("");

            // 5. 再次触发
            Log("【5】再次触发 OnAttack");
            Log("--- Trigger(\"OnAttack\") ---");
            fsm.Trigger("OnAttack");
            Log($"当前状态: {fsm.ActiveStateName}");
            Log("");

            // 6. 触发后正常更新
            Log("【6】触发后的逻辑更新");
            Log("--- OnLogic() ---");
            fsm.OnLogic();
            Log($"当前状态: {fsm.ActiveStateName}");
            Log("");

            Output.Divider();
        }
    }
}
