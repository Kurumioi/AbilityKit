using System;

namespace AbilityKit.Samples.Logic.Samples.Demo.ProgressiveSkill.Phase0.Skills
{
    /// <summary>
    /// 简单技能执行器 - 硬编码顺序执行
    /// Phase0: 基础调度 - 不使用 Pipeline，仅展示基本流程
    /// </summary>
    public class SimpleSkillExecutor
    {
        public event Action<string> OnLog;
        
        /// <summary>
        /// 执行技能
        /// </summary>
        /// <param name="context">技能上下文</param>
        public void Execute(SkillContext context)
        {
            Log($"=== 开始执行技能: {context.Skill.Name} ===");
            
            // 1. 验证条件
            if (!Validate(context))
            {
                Log("  [验证失败] 技能无法执行");
                return;
            }
            
            // 2. 消耗资源
            ConsumeResources(context);
            
            // 3. 施法前摇
            Log($"  [施法] 开始引导... ({context.Skill.CastTime:F1}秒)");
            SimulateDelay(context.Skill.CastTime);
            
            // 4. 产生效果
            ApplyEffects(context);
            
            // 5. 设置冷却
            SetCooldown(context);
            
            Log($"=== 技能执行完成 ===");
        }
        
        /// <summary>
        /// 验证技能是否可以执行
        /// </summary>
        private bool Validate(SkillContext context)
        {
            // 检查施法者是否存活
            if (!context.Caster.IsAlive)
            {
                Log("  [验证] 施法者已死亡");
                return false;
            }
            
            // 检查目标是否有效
            if (context.Target == null || !context.Target.IsAlive)
            {
                Log("  [验证] 目标无效或已死亡");
                return false;
            }
            
            // 检查距离
            var casterPos = new { X = context.Caster.Id % 10, Y = 0f, Z = 0f };
            var targetPos = new { X = context.Target.Id % 10, Y = 0f, Z = 0f };
            var distance = Math.Abs(targetPos.X - casterPos.X);
            
            if (distance > context.Skill.Range)
            {
                Log($"  [验证] 目标距离过远 ({distance:F1} > {context.Skill.Range})");
                return false;
            }
            
            // 检查魔法值
            if (!context.Caster.HasMana(context.Skill.ManaCost))
            {
                Log($"  [验证] 魔法值不足 ({context.Caster.Mana:F0} < {context.Skill.ManaCost})");
                return false;
            }
            
            // 检查冷却
            if (context.CooldownRemaining > 0)
            {
                Log($"  [验证] 技能冷却中 ({context.CooldownRemaining:F1}秒)");
                return false;
            }
            
            Log("  [验证] 验证通过");
            return true;
        }
        
        /// <summary>
        /// 消耗资源
        /// </summary>
        private void ConsumeResources(SkillContext context)
        {
            context.Caster.ConsumeMana(context.Skill.ManaCost);
            Log($"  [消耗] 魔法值 -{context.Skill.ManaCost} (剩余: {context.Caster.Mana:F0})");
        }
        
        /// <summary>
        /// 模拟延迟
        /// </summary>
        private void SimulateDelay(float seconds)
        {
            // 在真实实现中，这里会使用 async/await 或协程
            // 这里简化处理，直接继续执行
        }
        
        /// <summary>
        /// 应用效果
        /// </summary>
        private void ApplyEffects(SkillContext context)
        {
            foreach (var effect in context.Skill.Effects)
            {
                ApplyEffect(context, effect);
            }
        }
        
        /// <summary>
        /// 应用单个效果
        /// </summary>
        private void ApplyEffect(SkillContext context, SkillEffect effect)
        {
            switch (effect.Type)
            {
                case EffectType.Damage:
                    var damage = CalculateDamage(context, effect);
                    context.Target.TakeDamage(damage);
                    Log($"  [效果] 造成 {damage:F0} 点{effect.DamageType}伤害");
                    break;
                    
                case EffectType.Heal:
                    context.Caster.Heal(effect.Value);
                    Log($"  [效果] 治疗 {effect.Value:F0} 点生命");
                    break;
                    
                case EffectType.Buff:
                    Log($"  [效果] 添加 Buff: {effect.BuffId}");
                    break;
                    
                case EffectType.Debuff:
                    Log($"  [效果] 添加 Debuff: {effect.BuffId}");
                    break;
            }
        }
        
        /// <summary>
        /// 计算伤害
        /// </summary>
        private float CalculateDamage(SkillContext context, SkillEffect effect)
        {
            var baseDamage = context.Skill.BaseDamage + effect.Value;
            var attackPower = context.Caster.AttackPower;
            var defense = context.Target.Defense;
            
            // 伤害公式: (攻击 * 技能系数 - 防御 * 防御系数) * 暴击倍率
            var damage = (attackPower * (baseDamage / 100f) - defense * 0.5f);
            return Math.Max(1, damage);
        }
        
        /// <summary>
        /// 设置冷却
        /// </summary>
        private void SetCooldown(SkillContext context)
        {
            context.CooldownRemaining = context.Skill.Cooldown;
            Log($"  [冷却] 技能进入冷却 ({context.Skill.Cooldown:F1}秒)");
        }
        
        private void Log(string message)
        {
            OnLog?.Invoke(message);
            Console.WriteLine(message);
        }
    }
}
