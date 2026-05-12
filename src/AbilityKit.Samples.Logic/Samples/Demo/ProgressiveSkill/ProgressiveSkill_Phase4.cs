using System;
using System.Collections.Generic;
using AbilityKit.Samples.Abstractions;

namespace AbilityKit.Samples.Logic.Samples.Demo.ProgressiveSkill
{
    /// <summary>
    /// ProgressiveSkill Phase4 - Buff 系统示例
    /// 
    /// 本阶段目标：
    /// 1. 掌握 Buff 系统的核心概念
    /// 2. 理解 Buff 的生命周期管理
    /// 3. 学会创建和组合各种 Buff 类型
    /// 
    /// 演进路线:
    /// Phase0: 硬编码顺序执行
    /// Phase1: Pipeline 化
    /// Phase2: 行为树化
    /// Phase3: HFSM 化
    /// Phase4 (当前): Buff 化
    /// Phase5: 网络化
    /// </summary>
    [Sample]
    public sealed class ProgressiveSkill_Phase4 : SampleBase
    {
        public override string Title => "ProgressiveSkill Phase4";
        public override string Description => "Buff 系统 - 将效果抽象为可组合的 Buff";
        public override SampleCategory Category => SampleCategory.Demo;

        protected override void OnRun()
        {
            Log("================================================================================");
            Log("===           渐进式技能系统 - Phase4: Buff 系统                       ===");
            Log("================================================================================");
            Output.Divider();
            
            // 架构说明
            Log("【Phase4 架构说明】");
            Output.Bullet("Buff: 可叠加的持续效果");
            Output.Bullet("BuffDefinition: Buff 的定义和配置");
            Output.Bullet("BuffInstance: Buff 的运行时实例");
            Output.Bullet("BuffSystem: Buff 的管理器和生命周期");
            Log("");
            
            // Buff 类型说明
            Log("【1】Buff 类型");
            Output.Bullet("DOT (Damage Over Time): 持续伤害 - 灼烧、流血");
            Output.Bullet("HOT (Heal Over Time): 持续治疗");
            Output.Bullet("StatModifier: 属性修改 - 攻击力、速度");
            Output.Bullet("Shield: 护盾 - 吸收伤害");
            Output.Bullet("CrowdControl: 控制效果 - 眩晕、沉默");
            Log("");
            
            // 创建实体和 Buff 系统
            Log("【2】创建实体和 Buff 系统");
            
            var hero = new Phase0.Entities.SkillEntity(1, "勇者亚索", maxHealth: 500f, maxMana: 300f);
            var enemy = new Phase0.Entities.TargetEntity(1, "哥布林", maxHealth: 400f);
            
            var buffSystem = new Phase4_BuffSystem();
            
            Log($"  英雄: {hero}");
            Log($"  敌人: {enemy}");
            Log($"  Buff 系统已创建");
            Log("");
            
            // 添加 Buff 示例
            Log("【3】添加 Buff");
            Output.Line();
            
            Log("  --- 添加灼烧 Debuff ---");
            var burningDef = Phase4_BuffDefinition.CreateBurning(15f, 5f);
            buffSystem.AddBuff(hero.Id, burningDef);
            
            Log("");
            Log("  --- 添加强化 Buff ---");
            var empowerDef = Phase4_BuffDefinition.CreateEmpower(50f, 3f);
            buffSystem.AddBuff(hero.Id, empowerDef);
            
            Log("");
            Log("  --- 添加减速 Debuff ---");
            var slowDef = Phase4_BuffDefinition.CreateSlow(0.5f, 4f);
            buffSystem.AddBuff(hero.Id, slowDef);
            
            Log("");
            
            // 更新 Buff
            Log("【4】更新 Buff (模拟 3 秒)");
            Output.Line();
            
            for (int i = 0; i < 3; i++)
            {
                Log($"  --- 第 {i + 1} 秒 ---");
                buffSystem.Update(hero.Id, 1f);
                
                var buffs = buffSystem.GetBuffs(hero.Id);
                foreach (var buff in buffs)
                {
                    Log($"    当前 Buff: {buff}");
                }
            }
            
            Log("");
            
            // Buff 叠加示例
            Log("【5】Buff 叠加");
            Output.Line();
            
            Log("  --- 再次添加灼烧 (Refresh 策略) ---");
            burningDef = Phase4_BuffDefinition.CreateBurning(15f, 5f);
            burningDef.StackPolicy = Phase4_StackPolicy.Refresh;
            buffSystem.AddBuff(hero.Id, burningDef);
            
            Log("");
            Log("  --- 添加强化 (Stack 策略) ---");
            empowerDef = Phase4_BuffDefinition.CreateEmpower(50f, 3f);
            empowerDef.StackPolicy = Phase4_StackPolicy.Stack;
            empowerDef.MaxStacks = 3;
            buffSystem.AddBuff(hero.Id, empowerDef);
            buffSystem.AddBuff(hero.Id, empowerDef);
            buffSystem.AddBuff(hero.Id, empowerDef);
            
            Log("");
            
            // Buff 系统架构
            Log("【6】Buff 系统架构");
            Log("  ┌──────────────────┐");
            Log("  │    BuffSystem    │");
            Log("  ├──────────────────┤");
            Log("  │ AddBuff()       │");
            Log("  │ RemoveBuff()    │");
            Log("  │ Update()        │");
            Log("  │ HasBuff()       │");
            Log("  │ GetBuffs()      │");
            Log("  └────────┬─────────┘");
            Log("           │");
            Log("           ↓");
            Log("  ┌──────────────────┐");
            Log("  │   BuffInstance    │");
            Log("  ├──────────────────┤");
            Log("  │ Definition       │");
            Log("  │ Elapsed         │");
            Log("  │ StackCount      │");
            Log("  │ OnApply()       │");
            Log("  │ OnTick()        │");
            Log("  │ OnRemove()      │");
            Log("  └──────────────────┘");
            Log("");
            
            // 叠加策略
            Log("【7】叠加策略");
            Output.Bullet("None: 不可叠加，已存在则跳过");
            Output.Bullet("Refresh: 刷新持续时间");
            Output.Bullet("Stack: 增加叠加层数");
            Output.Bullet("Replace: 替换原有 Buff");
            Log("");
            
            // 预告下一阶段
            Log("【8】下一阶段预告 (Phase5: 网络同步)");
            Output.Bullet("引入帧同步/状态同步");
            Output.Bullet("技能输入序列化");
            Output.Bullet("状态插值和预测");
            Output.Bullet("多方客户端同步");
            Log("");
            
            Output.Divider();
            Log("【总结】Buff 系统提供了可组合、可叠加的效果管理能力");
            Output.Divider();
        }
    }
    
    // ============================================
    // Phase4 Buff 系统实现
    // ============================================
    
    /// <summary>
    /// 叠加策略
    /// </summary>
    public enum Phase4_StackPolicy
    {
        None,
        Refresh,
        Stack,
        Replace
    }
    
    /// <summary>
    /// Buff 定义
    /// </summary>
    public class Phase4_BuffDefinition
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public float Duration { get; set; }
        public int MaxStacks { get; set; } = 1;
        public Phase4_StackPolicy StackPolicy { get; set; } = Phase4_StackPolicy.None;
        public float DamagePerTick { get; set; }
        public float TickInterval { get; set; } = 1f;
        public float HealPerTick { get; set; }
        public float StatBonus { get; set; }
        
        public static Phase4_BuffDefinition CreateBurning(float damagePerTick = 10f, float duration = 5f)
        {
            return new Phase4_BuffDefinition
            {
                Id = "Burning",
                Name = "灼烧",
                Duration = duration,
                DamagePerTick = damagePerTick,
                TickInterval = 1f,
                StackPolicy = Phase4_StackPolicy.Refresh
            };
        }
        
        public static Phase4_BuffDefinition CreateEmpower(float attackBonus = 50f, float duration = 3f)
        {
            return new Phase4_BuffDefinition
            {
                Id = "Empower",
                Name = "强化",
                Duration = duration,
                StatBonus = attackBonus,
                StackPolicy = Phase4_StackPolicy.Stack,
                MaxStacks = 3
            };
        }
        
        public static Phase4_BuffDefinition CreateSlow(float speedReduction = 0.5f, float duration = 4f)
        {
            return new Phase4_BuffDefinition
            {
                Id = "Slow",
                Name = "减速",
                Duration = duration,
                StatBonus = speedReduction,
                StackPolicy = Phase4_StackPolicy.Refresh
            };
        }
    }
    
    /// <summary>
    /// Buff 实例
    /// </summary>
    public class Phase4_BuffInstance
    {
        public string Id => Definition.Id;
        public Phase4_BuffDefinition Definition { get; }
        public float Elapsed { get; private set; }
        public int StackCount { get; set; } = 1;
        public bool IsExpired => Elapsed >= Definition.Duration;
        
        public Phase4_BuffInstance(Phase4_BuffDefinition definition)
        {
            Definition = definition;
            Elapsed = 0;
        }
        
        public void Update(float deltaTime)
        {
            Elapsed += deltaTime;
            
            // 检查 Tick
            if (Definition.DamagePerTick > 0)
            {
                var ticks = (int)(Elapsed / Definition.TickInterval);
                var prevTicks = (int)((Elapsed - deltaTime) / Definition.TickInterval);
                
                if (ticks > prevTicks)
                {
                    Console.WriteLine($"      [灼烧] 造成 {Definition.DamagePerTick * StackCount:F0} 点火焰伤害!");
                }
            }
        }
        
        public void Reset()
        {
            Elapsed = 0;
        }
        
        public override string ToString() => $"{Definition.Name}({StackCount}/{Definition.MaxStacks}, {Definition.Duration - Elapsed:F1}s)";
    }
    
    /// <summary>
    /// Buff 系统
    /// </summary>
    public class Phase4_BuffSystem
    {
        private readonly Dictionary<long, List<Phase4_BuffInstance>> _entityBuffs = new();
        
        public void AddBuff(long entityId, Phase4_BuffDefinition definition)
        {
            if (!_entityBuffs.TryGetValue(entityId, out var buffs))
            {
                buffs = new List<Phase4_BuffInstance>();
                _entityBuffs[entityId] = buffs;
            }
            
            // 检查是否已存在
            var existing = buffs.Find(b => b.Definition.Id == definition.Id);
            if (existing != null)
            {
                switch (definition.StackPolicy)
                {
                    case Phase4_StackPolicy.Refresh:
                        existing.Reset();
                        Console.WriteLine($"    [Buff] 刷新 {definition.Name} 持续时间");
                        return;
                        
                    case Phase4_StackPolicy.Stack:
                        if (existing.StackCount < definition.MaxStacks)
                        {
                            existing.StackCount++;
                            existing.Reset();
                            Console.WriteLine($"    [Buff] {definition.Name} 叠加层数: {existing.StackCount}");
                        }
                        return;
                        
                    case Phase4_StackPolicy.Replace:
                        buffs.Remove(existing);
                        Console.WriteLine($"    [Buff] 替换 {definition.Name}");
                        break;
                        
                    case Phase4_StackPolicy.None:
                    default:
                        Console.WriteLine($"    [Buff] {definition.Name} 已存在，跳过");
                        return;
                }
            }
            
            // 添加新 Buff
            var instance = new Phase4_BuffInstance(definition);
            buffs.Add(instance);
            Console.WriteLine($"    [Buff] 添加 {definition.Name} 到实体 {entityId}");
        }
        
        public void Update(long entityId, float deltaTime)
        {
            if (!_entityBuffs.TryGetValue(entityId, out var buffs))
                return;
            
            var expiredBuffs = new List<Phase4_BuffInstance>();
            
            foreach (var buff in buffs)
            {
                buff.Update(deltaTime);
                
                if (buff.IsExpired)
                {
                    expiredBuffs.Add(buff);
                }
            }
            
            foreach (var buff in expiredBuffs)
            {
                buffs.Remove(buff);
                Console.WriteLine($"    [Buff] {buff.Definition.Name} 已过期");
            }
        }
        
        public List<Phase4_BuffInstance> GetBuffs(long entityId)
        {
            if (_entityBuffs.TryGetValue(entityId, out var buffs))
            {
                return new List<Phase4_BuffInstance>(buffs);
            }
            return new List<Phase4_BuffInstance>();
        }
    }
}
