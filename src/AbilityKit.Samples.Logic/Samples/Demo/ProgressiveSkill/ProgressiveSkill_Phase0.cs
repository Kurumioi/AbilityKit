using System;
using AbilityKit.Samples.Abstractions;

namespace AbilityKit.Samples.Logic.Samples.Demo.ProgressiveSkill
{
    /// <summary>
    /// ProgressiveSkill Phase0 - 基础调度示例
    /// 
    /// 本阶段目标：
    /// 1. 理解技能执行的基本流程
    /// 2. 掌握技能上下文 (SkillContext) 的概念
    /// 3. 熟悉简单技能执行器 (SimpleSkillExecutor) 的实现
    /// 
    /// 演进路线:
    /// Phase0 (当前): 硬编码顺序执行
    /// Phase1: Pipeline 化 - 将执行拆分为可配置的阶段
    /// Phase2: 行为树化 - 使用行为树处理决策
    /// Phase3: 状态机化 - 使用 HFSM 管理状态
    /// Phase4: Buff 化 - 将效果抽象为 Buff
    /// Phase5: 网络化 - 支持帧同步/状态同步
    /// </summary>
    [Sample]
    public sealed class ProgressiveSkill_Phase0 : SampleBase
    {
        public override string Title => "ProgressiveSkill Phase0";
        public override string Description => "基础调度 - 硬编码顺序执行的技能系统";
        public override SampleCategory Category => SampleCategory.Demo;

        protected override void OnRun()
        {
            Log("================================================================================");
            Log("===           渐进式技能系统 - Phase0: 基础调度                              ===");
            Log("================================================================================");
            Output.Divider();
            
            // 架构说明
            Log("【Phase0 架构说明】");
            Output.Bullet("技能执行器 (SkillExecutor): 硬编码的顺序执行流程");
            Output.Bullet("技能上下文 (SkillContext): 封装执行时的所有信息");
            Output.Bullet("技能定义 (SkillDefinition): 技能的配置数据");
            Output.Bullet("执行流程: 验证 → 消耗 → 施法 → 效果 → 冷却");
            Log("");
            
            // 创建实体
            Log("【1】创建实体");
            var hero = new Phase0.Entities.SkillEntity(1, "勇者亚索", maxHealth: 500f, maxMana: 300f);
            hero.AttackPower = 80f;
            hero.Defense = 25f;
            
            var enemy1 = new Phase0.Entities.TargetEntity(1, "哥布林", maxHealth: 200f);
            enemy1.PositionX = 5f;
            enemy1.Defense = 10f;
            
            var enemy2 = new Phase0.Entities.TargetEntity(2, "哥布林王", maxHealth: 400f);
            enemy2.PositionX = 10f;
            enemy2.Defense = 20f;
            
            Log($"  英雄: {hero}");
            Log($"  敌人1: {enemy1} (距离: {enemy1.PositionX})");
            Log($"  敌人2: {enemy2} (距离: {enemy2.PositionX})");
            Log("");
            
            // 演示火球术
            Log("【2】施放火球术 (目标: 哥布林)");
            Output.Line();
            
            var fireballSkill = new Phase0.Skills.BasicFireballSkill();
            fireballSkill.Cast(hero, enemy1);
            
            Log("");
            
            // 演示治疗术
            Log("【3】施放治疗术 (目标: 英雄自己)");
            Output.Line();
            
            var healSkill = new Phase0.Skills.BasicFireballSkill();
            // 创建一个治疗技能
            var healDef = Phase0.Skills.SkillDefinition.CreateHeal();
            var healContext = new Phase0.Skills.SkillContext(hero, new Phase0.Entities.TargetEntity(999, "治疗目标", maxHealth: 500f), healDef);
            var healExecutor = new Phase0.Skills.SimpleSkillExecutor();
            healExecutor.Execute(healContext);
            
            Log("");
            
            // 展示问题
            Log("【4】Phase0 的问题");
            Output.Bullet("所有逻辑硬编码在 SkillExecutor 中");
            Output.Bullet("技能流程无法灵活配置");
            Output.Bullet("不支持打断、暂停、恢复");
            Output.Bullet("效果系统简陋，无法组合");
            Output.Bullet("难以测试单个阶段");
            Log("");
            
            // 预告下一阶段
            Log("【5】下一阶段预告 (Phase1: Pipeline)");
            Output.Bullet("将技能执行拆分为多个独立阶段");
            Output.Bullet("每个阶段可独立配置和复用");
            Output.Bullet("支持阶段打断和恢复");
            Output.Bullet("便于添加新阶段");
            Log("");
            
            Output.Divider();
            Log("【总结】Phase0 展示了技能系统的基本概念，但缺乏灵活性和可扩展性");
            Output.Divider();
        }
    }
}
