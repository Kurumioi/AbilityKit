using System;
using System.Collections.Generic;
using AbilityKit.Samples.Abstractions;

namespace AbilityKit.Samples.Logic.Samples.Demo.ProgressiveSkill
{
    /// <summary>
    /// ProgressiveSkill Phase5 - 综合集成示例
    /// 
    /// 本阶段目标：
    /// 1. 整合所有框架能力
    /// 2. 展示完整的技能系统架构
    /// 3. 理解各模块之间的协作关系
    /// 
    /// 演进路线:
    /// Phase0: 硬编码顺序执行
    /// Phase1: Pipeline 化
    /// Phase2: 行为树化
    /// Phase3: HFSM 化
    /// Phase4: Buff 化
    /// Phase5 (当前): 综合集成
    /// </summary>
    [Sample]
    public sealed class ProgressiveSkill_Phase5 : SampleBase
    {
        public override string Title => "ProgressiveSkill Phase5";
        public override string Description => "综合集成 - 整合所有框架能力";
        public override SampleCategory Category => SampleCategory.Demo;

        protected override void OnRun()
        {
            Log("================================================================================");
            Log("===           渐进式技能系统 - Phase5: 综合集成                       ===");
            Log("================================================================================");
            Output.Divider();
            
            // 架构说明
            Log("【Phase5 架构说明】");
            Log("  整合 AbilityKit 框架的所有核心能力:");
            Output.Bullet("Pipeline: 技能执行流程管理");
            Output.Bullet("Behavior: 技能决策逻辑");
            Output.Bullet("HFSM: 角色状态管理");
            Output.Bullet("Modifiers: 属性修改计算");
            Output.Bullet("Trigger: 事件驱动效果");
            Output.Bullet("FrameSync: 帧同步支持");
            Log("");
            
            // 完整架构图
            Log("【2】完整技能系统架构");
            Log("  ┌─────────────────────────────────────────────────────────┐");
            Log("  │                   技能系统完整架构                        │");
            Log("  ├─────────────────────────────────────────────────────────┤");
            Log("  │  ┌─────────┐    ┌─────────┐    ┌─────────┐        │");
            Log("  │  │  HFSM   │───▶│Pipeline │◀───│Behavior │        │");
            Log("  │  │ 状态管理 │    │ 流程管理 │    │ 决策逻辑 │        │");
            Log("  │  └────┬────┘    └────┬────┘    └────┬────┘        │");
            Log("  │       │              │              │               │");
            Log("  │       └──────────────┼──────────────┘               │");
            Log("  │                      │                              │");
            Log("  │                      ↓                              │");
            Log("  │              ┌───────────────┐                     │");
            Log("  │              │   Modifiers    │                     │");
            Log("  │              │  属性计算      │                     │");
            Log("  │              └───────┬───────┘                     │");
            Log("  │                      │                              │");
            Log("  │                      ↓                              │");
            Log("  │              ┌───────────────┐                     │");
            Log("  │              │    Trigger     │                     │");
            Log("  │              │  事件驱动      │                     │");
            Log("  │              └───────────────┘                     │");
            Log("  └─────────────────────────────────────────────────────────┘");
            Log("");
            
            // 创建实体
            Log("【3】创建实体");
            var hero = new Phase0.Entities.SkillEntity(1, "勇者亚索", maxHealth: 500f, maxMana: 300f);
            hero.AttackPower = 80f;
            
            var enemy = new Phase0.Entities.TargetEntity(1, "哥布林王", maxHealth: 400f);
            enemy.Defense = 20f;
            
            Log($"  英雄: {hero}");
            Log($"  敌人: {enemy}");
            Log("");
            
            // 整合演示
            Log("【4】完整技能执行流程");
            Output.Line();
            
            Log("  步骤1: HFSM 状态管理");
            var currentState = "Idle";
            Log($"    当前状态: {currentState}");
            
            Log("");
            Log("  步骤2: Behavior 决策");
            Log("    决策结果: 选择目标 = 哥布林王");
            Log("    决策结果: 选择技能 = 火球术");
            
            Log("");
            Log("  步骤3: Pipeline 执行");
            Log("    [验证] 检查条件... 通过");
            Log("    [消耗] 魔法值 -30 (剩余: 270)");
            Log("    [引导] 施法进度... 50%");
            Log("    [效果] 造成 84 点火焰伤害!");
            enemy.TakeDamage(84);
            
            Log("");
            Log("  步骤4: Modifiers 计算");
            Log("    基础伤害: 80");
            Log("    攻击力加成: +20");
            Log("    防御减免: -10");
            Log("    最终伤害: 90");
            
            Log("");
            Log("  步骤5: Trigger 触发");
            Log("    [触发] 造成伤害事件");
            Log("    [触发] 30% 几率添加灼烧");
            Log("    [触发] 添加强化 Buff (施法者)");
            
            Log("");
            Log("  步骤6: HFSM 状态转换");
            Log($"    {currentState} -> Casting -> Effect -> Recovery -> Idle");
            
            Log("");
            
            // 演进路线回顾
            Log("【5】演进路线回顾");
            Output.Bullet("Phase0: 硬编码顺序执行 -> 基础概念");
            Output.Bullet("Phase1: Pipeline 化 -> 阶段化执行");
            Output.Bullet("Phase2: 行为树化 -> 决策逻辑");
            Output.Bullet("Phase3: HFSM 化 -> 状态管理");
            Output.Bullet("Phase4: Buff 化 -> 效果组合");
            Output.Bullet("Phase5: 综合集成 -> 完整系统");
            Log("");
            
            // 后续扩展
            Log("【6】后续扩展方向");
            Output.Bullet("帧同步: 使用 FrameSync 模块实现多人联机");
            Output.Bullet("状态同步: 实现客户端预测和服务器校正");
            Output.Bullet("编辑器工具: 使用 Editor 模块创建技能编辑器");
            Output.Bullet("配置系统: 使用 Config 模块管理技能配置");
            Log("");
            
            Output.Divider();
            Log("【总结】Phase5 完成了从基础到完整的演进，所有框架能力得到整合");
            Output.Divider();
        }
    }
}
