using System;
using AbilityKit.Pipeline;
using AbilityKit.Samples.Abstractions;

namespace AbilityKit.Samples.Logic.Samples.Demo.ProgressiveSkill
{
    /// <summary>
    /// ProgressiveSkill Phase1 - Pipeline 示例
    /// 
    /// 本阶段目标：
    /// 1. 使用 AbilityPipeline 框架重构技能执行
    /// 2. 理解 Pipeline 阶段的概念
    /// 3. 掌握自定义 Pipeline 阶段的创建
    /// 
    /// 相比 Phase0 的改进：
    /// - 使用框架的 AbilityPipeline 管理执行流程
    /// - 阶段可独立配置和复用
    /// - 支持阶段打断和恢复
    /// - 更易于测试和扩展
    /// 
    /// 演进路线:
    /// Phase0: 硬编码顺序执行
    /// Phase1 (当前): Pipeline 化 - 使用 AbilityPipeline 框架
    /// Phase2: Behavior 化 - 使用 BehaviorRuntime
    /// Phase3: Modifiers 化 - 使用 ModifierSystem
    /// Phase4: Trigger 化 - 使用 Triggering 模块
    /// Phase5: 综合集成
    /// </summary>
    [Sample]
    public sealed class ProgressiveSkill_Phase1 : SampleBase
    {
        public override string Title => "ProgressiveSkill Phase1";
        public override string Description => "Pipeline - 使用 AbilityPipeline 框架";
        public override SampleCategory Category => SampleCategory.Demo;

        protected override void OnRun()
        {
            Log("================================================================================");
            Log("===           渐进式技能系统 - Phase1: Pipeline                          ===");
            Log("================================================================================");
            Output.Divider();
            
            // 架构说明
            Log("【Phase1 架构说明】");
            Output.Bullet("AbilityPipeline: 框架提供的 Pipeline 核心类 (抽象)");
            Output.Bullet("AbilityInstantPhaseBase: 瞬时阶段基类");
            Output.Bullet("AbilityDurationalPhaseBase: 持续性阶段基类");
            Output.Bullet("AbilityInterruptiblePhaseBase: 可中断阶段基类");
            Log("");
            
            // 使用的框架类型
            Log("【2】使用的框架类型");
            Log("  - AbilityPipeline<TCtx>: 管线执行器 (抽象类，需派生)");
            Log("  - AAbilityPipelineContext: 管线上下文基类");
            Log("  - AbilityInstantPhaseBase: 瞬时阶段");
            Log("  - AbilityDurationalPhaseBase: 持续阶段");
            Log("  - AbilityInterruptiblePhaseBase: 可中断阶段");
            Log("");
            
            // 创建实体
            Log("【3】创建实体");
            var hero = new Phase0.Entities.SkillEntity(1, "勇者亚索", maxHealth: 500f, maxMana: 300f);
            hero.AttackPower = 80f;
            hero.Defense = 25f;
            
            var enemy = new Phase0.Entities.TargetEntity(1, "哥布林", maxHealth: 200f);
            enemy.PositionX = 10f;
            enemy.Defense = 10f;
            
            Log($"  英雄: {hero}");
            Log($"  敌人: {enemy}");
            Log("");
            
            // 创建火球术 Pipeline (使用框架的阶段类型)
            Log("【4】创建火球术 Pipeline (使用框架)");
            Output.Line();
            
            // 使用框架提供的延迟阶段
            var delayPhase = new AbilityDelayPhase<Phase1_SkillContext>(1.5f);
            delayPhase.PhaseName = "Channeling";
            
            Log($"  Pipeline 阶段:");
            Log("    1. ValidationPhase (验证) - 检查施法条件");
            Log("    2. ConsumePhase (消耗) - 消耗魔法值");
            Log("    3. AbilityDelayPhase (引导) - 施法时间");
            Log("    4. EffectPhase (效果) - 造成伤害");
            Log("    5. CooldownPhase (冷却) - 设置冷却");
            Log("");
            
            // 执行验证和消耗阶段 (瞬时)
            Log("【5】执行 Pipeline 阶段");
            Output.Line();
            
            var context = Phase1_SkillContext.Create(
                hero, 
                enemy, 
                Phase0.Skills.SkillDefinition.CreateFireball());
            
            // 阶段1: 验证
            Log("  --- 验证阶段 ---");
            var validationPhase = new Phase1_ValidationPhase();
            validationPhase.Execute(context);
            
            if (context.IsAborted)
            {
                Log($"  验证失败: {context.ValidationFailureReason}");
            }
            else
            {
                Log("  验证通过!");
                
                // 阶段2: 消耗
                Log("  --- 消耗阶段 ---");
                var consumePhase = new Phase1_ConsumePhase();
                consumePhase.Execute(context);
                
                // 阶段3: 引导 (模拟)
                Log("  --- 引导阶段 (模拟 1.5秒) ---");
                for (int i = 0; i < 3; i++)
                {
                    delayPhase.OnUpdate(context, 0.5f);
                    Log($"    引导进度... {((i + 1) * 0.5f / 1.5f) * 100:F0}%");
                }
                delayPhase.ForceComplete(context);
                
                // 阶段4: 效果
                Log("  --- 效果阶段 ---");
                var effectPhase = new Phase1_EffectPhase();
                effectPhase.Execute(context);
                
                // 阶段5: 冷却
                Log("  --- 冷却阶段 ---");
                var cooldownPhase = new Phase1_CooldownPhase();
                cooldownPhase.Execute(context);
            }
            
            Log("");
            Log($"  敌人最终 HP: {enemy.Health}/{enemy.MaxHealth}");
            Log("");
            
            // Pipeline 阶段类型说明
            Log("【6】框架提供的阶段类型");
            Output.Bullet("AbilityDelayPhase: 延迟阶段");
            Output.Bullet("AbilityConditionalPhase: 条件分支阶段");
            Output.Bullet("AbilitySequencePhase: 序列阶段");
            Output.Bullet("AbilityParallelPhase: 并行阶段");
            Output.Bullet("AbilityRepeatPhase: 重复阶段");
            Log("");
            
            // 自定义阶段说明
            Log("【7】如何创建自定义阶段");
            Log("  1. 继承 AbilityInstantPhaseBase<TCtx> - 瞬时阶段");
            Log("  2. 继承 AbilityDurationalPhaseBase<TCtx> - 持续阶段");
            Log("  3. 继承 AbilityInterruptiblePhaseBase<TCtx> - 可中断阶段");
            Log("");
            Log("  示例:");
            Log("    public class MyPhase : AbilityInstantPhaseBase<MyContext>");
            Log("    {");
            Log("        protected override void OnInstantExecute(MyContext context)");
            Log("        {");
            Log("            // 执行逻辑");
            Log("        }");
            Log("    }");
            Log("");
            
            // 预告下一阶段
            Log("【8】下一阶段预告 (Phase2: Behavior)");
            Output.Bullet("使用 BehaviorRuntime 处理决策逻辑");
            Output.Bullet("目标选择行为树");
            Output.Bullet("连击系统行为树");
            Output.Bullet("技能条件检查行为树");
            Log("");
            
            Output.Divider();
            Log("【总结】AbilityPipeline 框架提供了强大的技能执行流程管理能力");
            Output.Divider();
        }
    }
    
    // ============================================
    // Phase1 使用的类型
    // ============================================
    
    /// <summary>
    /// Phase1 技能上下文
    /// </summary>
    public class Phase1_SkillContext : AAbilityPipelineContext
    {
        public Phase0.Entities.SkillEntity Caster { get; set; }
        public Phase0.Entities.TargetEntity Target { get; set; }
        public Phase0.Skills.SkillDefinition SkillDefinition { get; set; }
        public float CooldownRemaining { get; set; }
        public string ValidationFailureReason { get; set; }
        
        private DateTime _startTime = DateTime.UtcNow;
        
        public static Phase1_SkillContext Create(
            Phase0.Entities.SkillEntity caster,
            Phase0.Entities.TargetEntity target,
            Phase0.Skills.SkillDefinition skill)
        {
            var ctx = new Phase1_SkillContext
            {
                Caster = caster,
                Target = target,
                SkillDefinition = skill,
                CooldownRemaining = 0f,
                ValidationFailureReason = null,
                _startTime = DateTime.UtcNow
            };
            ctx.PipelineState = EAbilityPipelineState.Ready;
            ctx.IsAborted = false;
            ctx.IsPaused = false;
            return ctx;
        }
        
        public new float ElapsedTime => (float)(DateTime.UtcNow - _startTime).TotalSeconds;
        
        public override void Reset()
        {
            base.Reset();
            Caster = null;
            Target = null;
            SkillDefinition = null;
            CooldownRemaining = 0f;
            ValidationFailureReason = null;
            _startTime = DateTime.UtcNow;
        }
        
        public override void Initialize(object abilityInstance)
        {
            AbilityInstance = abilityInstance;
            PipelineState = EAbilityPipelineState.Ready;
            IsAborted = false;
            IsPaused = false;
            _startTime = DateTime.UtcNow;
        }
    }
    
    /// <summary>
    /// 验证阶段
    /// </summary>
    public class Phase1_ValidationPhase : AbilityInstantPhaseBase<Phase1_SkillContext>
    {
        public Phase1_ValidationPhase() : base("Validation") { }
        
        protected override void OnInstantExecute(Phase1_SkillContext context)
        {
            var caster = context.Caster;
            var target = context.Target;
            var skill = context.SkillDefinition;
            
            if (!caster.IsAlive)
            {
                context.ValidationFailureReason = "施法者已死亡";
                context.IsAborted = true;
                return;
            }
            
            if (target == null || !target.IsAlive)
            {
                context.ValidationFailureReason = "目标无效";
                context.IsAborted = true;
                return;
            }
            
            if (!caster.HasMana(skill.ManaCost))
            {
                context.ValidationFailureReason = "魔法值不足";
                context.IsAborted = true;
                return;
            }
            
            if (context.CooldownRemaining > 0)
            {
                context.ValidationFailureReason = "技能冷却中";
                context.IsAborted = true;
                return;
            }
        }
    }
    
    /// <summary>
    /// 消耗阶段
    /// </summary>
    public class Phase1_ConsumePhase : AbilityInstantPhaseBase<Phase1_SkillContext>
    {
        public Phase1_ConsumePhase() : base("Consume") { }
        
        protected override void OnInstantExecute(Phase1_SkillContext context)
        {
            context.Caster.ConsumeMana(context.SkillDefinition.ManaCost);
            Console.WriteLine($"    [Consume] 消耗魔法值 -{context.SkillDefinition.ManaCost} (剩余: {context.Caster.Mana:F0})");
        }
    }
    
    /// <summary>
    /// 效果阶段
    /// </summary>
    public class Phase1_EffectPhase : AbilityInstantPhaseBase<Phase1_SkillContext>
    {
        public Phase1_EffectPhase() : base("Effect") { }
        
        protected override void OnInstantExecute(Phase1_SkillContext context)
        {
            var caster = context.Caster;
            var target = context.Target;
            var skill = context.SkillDefinition;
            
            var baseDamage = skill.BaseDamage;
            var attackPower = caster.AttackPower;
            var defense = target.Defense;
            
            var damage = (attackPower * (baseDamage / 100f) - defense * 0.5f);
            damage = Math.Max(1, damage);
            
            Console.WriteLine($"    [Effect] 造成 {damage:F0} 点火焰伤害");
            target.TakeDamage(damage);
        }
    }
    
    /// <summary>
    /// 冷却阶段
    /// </summary>
    public class Phase1_CooldownPhase : AbilityInstantPhaseBase<Phase1_SkillContext>
    {
        public Phase1_CooldownPhase() : base("Cooldown") { }
        
        protected override void OnInstantExecute(Phase1_SkillContext context)
        {
            context.CooldownRemaining = context.SkillDefinition.Cooldown;
            Console.WriteLine($"    [Cooldown] 技能进入冷却 ({context.SkillDefinition.Cooldown:F1}秒)");
        }
    }
}
