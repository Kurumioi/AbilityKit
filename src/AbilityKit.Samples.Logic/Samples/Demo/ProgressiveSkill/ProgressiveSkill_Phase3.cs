using System;
using UnityHFSM;
using UnityHFSM.Extension;
using AbilityKit.Samples.Abstractions;

namespace AbilityKit.Samples.Logic.Samples.Demo.ProgressiveSkill
{
    /// <summary>
    /// ProgressiveSkill Phase3 - HFSM 示例
    /// 
    /// 本阶段目标：
    /// 1. 使用 HFSM 框架管理角色状态
    /// 2. 理解状态转换的概念
    /// 3. 掌握状态与 Pipeline 的结合
    /// 
    /// 使用的框架类型:
    /// - StateMachine: 状态机
    /// - State: 状态
    /// - Transition: 转换
    /// - CompositeActionState: 组合行为状态
    /// - SequenceBehaviour, SelectorBehaviour: 行为组合
    /// 
    /// 演进路线:
    /// Phase0: 硬编码顺序执行
    /// Phase1: Pipeline 化
    /// Phase2: 行为树化
    /// Phase3 (当前): HFSM 化
    /// Phase4: Buff 化
    /// Phase5: 网络化
    /// </summary>
    [Sample]
    public sealed class ProgressiveSkill_Phase3 : SampleBase
    {
        public override string Title => "ProgressiveSkill Phase3";
        public override string Description => "HFSM - 使用 StateMachine 框架";
        public override SampleCategory Category => SampleCategory.Demo;

        protected override void OnRun()
        {
            Log("================================================================================");
            Log("===           渐进式技能系统 - Phase3: HFSM                          ===");
            Log("================================================================================");
            Output.Divider();
            
            // 架构说明
            Log("【Phase3 架构说明】");
            Output.Bullet("HFSM: 层级有限状态机 (Hierarchical Finite State Machine)");
            Output.Bullet("StateMachine<TOwnId, TStateId, TEvent>: 框架提供的状态机");
            Output.Bullet("State<TStateId, TEvent>: 状态，包含进入/逻辑/退出回调");
            Output.Bullet("Transition: 转换，定义状态之间的切换规则");
            Output.Bullet("CompositeActionState: 组合行为状态，内嵌行为树");
            Log("");
            
            // 使用的框架类型
            Log("【2】使用的框架类型");
            Output.Bullet("StateMachine<TStateId, TEvent>: 状态机 (3个类型参数)");
            Output.Bullet("State<TStateId, TEvent>: 基础状态");
            Output.Bullet("Transition<TStateId>: 状态转换");
            Output.Bullet("TransitionAfter<TStateId>: 延迟转换");
            Output.Bullet("CompositeActionState<TStateId, TEvent>: 组合行为状态");
            Output.Bullet("SequenceBehaviour: 序列行为");
            Output.Bullet("SelectorBehaviour: 选择行为");
            Output.Bullet("CallbackBehaviour: 回调行为");
            Output.Bullet("DelayBehaviour: 延迟行为");
            Log("");
            
            // 创建实体
            Log("【3】创建实体");
            var hero = new Phase0.Entities.SkillEntity(1, "勇者亚索", maxHealth: 500f, maxMana: 300f);
            Log($"  英雄: {hero}");
            Log("");
            
            // 状态说明
            Log("【4】技能状态机状态");
            Log("  Idle (待机): 等待技能指令");
            Log("  Casting (施法): 正在施放技能");
            Log("  Channeling (引导): 引导技能施放中");
            Log("  Effect (效果): 技能效果生效中");
            Log("  Recovery (恢复): 技能后摇");
            Log("");
            
            // 创建状态机
            Log("【5】创建技能状态机 (使用框架)");
            Output.Line();
            
            var fsm = CreateSkillFSM();
            Log($"  状态机已创建: {fsm.GetType().Name}");
            Log("  状态:");
            foreach (var name in fsm.GetAllStateNames())
            {
                Log($"    - {name}");
            }
            Log("");
            
            // 模拟执行
            Log("【6】模拟状态机执行");
            Output.Line();
            
            Log("  --- 状态机初始化 ---");
            fsm.Init();
            Log($"    当前状态: {fsm.ActiveStateName}");
            
            Log("");
            Log("  --- 执行 5 帧 ---");
            for (int i = 0; i < 5; i++)
            {
                Log($"  帧 {i + 1}:");
                fsm.OnLogic();
            }
            
            Log("");
            
            // 状态转换图
            Log("【7】状态转换图");
            Log("  ┌─────────┐");
            Log("  │  Idle  │");
            Log("  └────┬────┘");
            Log("       │ CanCast");
            Log("       ↓");
            Log("  ┌─────────┐");
            Log("  │ Casting │");
            Log("  └────┬────┘");
            Log("       │ CastComplete");
            Log("       ↓");
            Log("  ┌─────────────┐");
            Log("  │ Channeling  │");
            Log("  └──────┬──────┘");
            Log("         │ ChannelComplete");
            Log("         ↓");
            Log("  ┌───────┐");
            Log("  │Effect │");
            Log("  └───┬───┘");
            Log("      │");
            Log("      ↓");
            Log("  ┌──────────┐");
            Log("  │ Recovery │");
            Log("  └────┬─────┘");
            Log("       │ RecoveryComplete");
            Log("       ↓");
            Log("  ┌─────────┐");
            Log("  │  Idle  │");
            Log("  └─────────┘");
            Log("");
            
            // HFSM 与其他系统集成
            Log("【8】HFSM 集成");
            Output.Bullet("HFSM + Pipeline: 状态内执行 Pipeline");
            Output.Bullet("HFSM + Behavior: 状态内嵌入 CompositeActionState");
            Output.Bullet("HFSM + Trigger: 状态转换触发 Trigger");
            Output.Bullet("Trigger -> HFSM: Trigger 结果请求状态转换");
            Log("");
            
            // 预告下一阶段
            Log("【9】下一阶段预告 (Phase4: Buff 系统)");
            Output.Bullet("引入完整的 Buff 系统");
            Output.Bullet("将效果抽象为可叠加的 Buff");
            Output.Bullet("支持 Buff 的添加、移除、刷新");
            Output.Bullet("Buff 可修改属性、触发效果");
            Log("");
            
            Output.Divider();
            Log("【总结】HFSM 框架提供了强大的状态管理能力，支持层级嵌套");
            Output.Divider();
        }
        
        /// <summary>
        /// 创建技能状态机 (使用 HFSM 框架)
        /// </summary>
        private StateMachine<string, string> CreateSkillFSM()
        {
            var fsm = new StateMachine<string, string>(needsExitTime: true);
            
            // 模拟数据
            float castTime = 0f;
            float channelTime = 0f;
            float recoveryTime = 0f;
            
            // ===== Idle 状态 =====
            var idleState = new State<string, string>(
                onEnter: s => Log("    [Idle] 进入待机状态"),
                onLogic: s => Log("    [Idle] 待机中...")
            );
            
            // ===== Casting 状态 =====
            var castingState = new State<string, string>(
                onEnter: s => {
                    castTime = 0f;
                    Log("    [Casting] 进入施法状态");
                },
                onLogic: s => {
                    castTime += 0.1f;
                    Log($"    [Casting] 施法中... {castTime:F1}s / 0.3s");
                }
            );
            
            // ===== Channeling 状态 =====
            var channelingState = new State<string, string>(
                onEnter: s => {
                    channelTime = 0f;
                    Log("    [Channeling] 进入引导状态");
                },
                onLogic: s => {
                    channelTime += 0.1f;
                    Log($"    [Channeling] 引导中... {channelTime:F1}s / 0.2s");
                }
            );
            
            // ===== Effect 状态 =====
            var effectState = new State<string, string>(
                onEnter: s => Log("    [Effect] 进入效果状态 - 造成伤害!"),
                onLogic: s => { }
            );
            
            // ===== Recovery 状态 =====
            var recoveryState = new State<string, string>(
                onEnter: s => {
                    recoveryTime = 0f;
                    Log("    [Recovery] 进入恢复状态");
                },
                onLogic: s => {
                    recoveryTime += 0.1f;
                    Log($"    [Recovery] 恢复中... {recoveryTime:F1}s / 0.2s");
                }
            );
            
            // 添加状态
            fsm.AddState("Idle", idleState);
            fsm.AddState("Casting", castingState);
            fsm.AddState("Channeling", channelingState);
            fsm.AddState("Effect", effectState);
            fsm.AddState("Recovery", recoveryState);
            
            // 添加转换
            fsm.AddTransition(new Transition<string>(
                from: "Idle",
                to: "Casting",
                condition: t => true
            ));
            
            fsm.AddTransition(new Transition<string>(
                from: "Casting",
                to: "Channeling",
                condition: t => castTime >= 0.3f
            ));
            
            fsm.AddTransition(new Transition<string>(
                from: "Channeling",
                to: "Effect",
                condition: t => channelTime >= 0.2f
            ));
            
            fsm.AddTransition(new Transition<string>(
                from: "Effect",
                to: "Recovery",
                condition: t => true
            ));
            
            fsm.AddTransition(new Transition<string>(
                from: "Recovery",
                to: "Idle",
                condition: t => recoveryTime >= 0.2f
            ));
            
            // 设置初始状态
            fsm.SetStartState("Idle");
            
            return fsm;
        }
    }
}
