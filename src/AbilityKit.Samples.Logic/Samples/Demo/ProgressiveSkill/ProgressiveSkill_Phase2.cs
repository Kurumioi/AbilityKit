using System;
using System.Collections.Generic;
using BTCore.Runtime;
using BTCore.Runtime.Blackboards;
using BTCore.Runtime.Composites;
using BTCore.Runtime.Conditions;
using AbilityKit.Samples.Abstractions;
using BTAction = BTCore.Runtime.Actions.Action;
using BTSystemAction = System.Action;

namespace AbilityKit.Samples.Logic.Samples.Demo.ProgressiveSkill
{
    /// <summary>
    /// ProgressiveSkill Phase2 - 行为树示例
    /// 
    /// 本阶段目标：
    /// 1. 掌握行为树的基本概念
    /// 2. 理解行为树节点类型：Sequence, Selector, Condition, Action
    /// 3. 学会构建技能决策行为树
    /// 
    /// 使用的框架类型 (来自 BTCore.Runtime):
    /// - BTree: 行为树容器
    /// - BTData: 行为树数据
    /// - EntryNode: 入口节点
    /// - Sequence: 序列节点
    /// - Selector: 选择节点
    /// - Composite: 组合节点基类
    /// - BTNode: 节点基类
    /// - Blackboard: 黑板数据
    /// - NodeState: 节点状态
    /// 
    /// 演进路线:
    /// Phase0: 硬编码顺序执行
    /// Phase1: Pipeline 化
    /// Phase2 (当前): 行为树化
    /// Phase3: HFSM 化
    /// Phase4: Buff 化
    /// Phase5: 网络化
    /// </summary>
    [Sample]
    public sealed class ProgressiveSkill_Phase2 : SampleBase
    {
        public override string Title => "ProgressiveSkill Phase2";
        public override string Description => "行为树 - 使用 BTCore 框架";
        public override SampleCategory Category => SampleCategory.Demo;

        protected override void OnRun()
        {
            Log("================================================================================");
            Log("===           渐进式技能系统 - Phase2: 行为树 (BTCore)              ===");
            Log("================================================================================");
            Output.Divider();
            
            // 架构说明
            Log("【Phase2 架构说明】");
            Output.Bullet("BTCore: AbilityKit 行为树运行时框架");
            Output.Bullet("BTree: 行为树容器，管理节点和执行");
            Output.Bullet("EntryNode: 入口节点，行为树执行的起点");
            Output.Bullet("Sequence: 序列节点 - 所有子节点成功才成功");
            Output.Bullet("Selector: 选择节点 - 找到第一个成功的就停止");
            Output.Bullet("Blackboard: 黑板，存储行为树数据");
            Output.Bullet("NodeState: 节点状态 (Success/Failure/Running)");
            Log("");
            
            // 使用的框架类型
            Log("【2】使用的框架类型 (BTCore.Runtime)");
            Output.Bullet("BTree: 行为树容器");
            Output.Bullet("BTData: 行为树数据");
            Output.Bullet("EntryNode: 入口节点");
            Output.Bullet("Sequence: 序列组合");
            Output.Bullet("Selector: 选择组合");
            Output.Bullet("Blackboard: 黑板数据");
            Output.Bullet("NodeState: 节点状态枚举");
            Log("");
            
            // 创建实体
            Log("【3】创建实体");
            var hero = new Phase0.Entities.SkillEntity(1, "勇者亚索", maxHealth: 500f, maxMana: 300f);
            hero.AttackPower = 80f;
            
            var enemy1 = new Phase0.Entities.TargetEntity(1, "哥布林A", maxHealth: 200f);
            enemy1.Health = 50f; // 低血量
            enemy1.PositionX = 8f;
            
            var enemy2 = new Phase0.Entities.TargetEntity(2, "哥布林B", maxHealth: 200f);
            enemy2.PositionX = 5f; // 更近
            
            var enemy3 = new Phase0.Entities.TargetEntity(3, "哥布林C", maxHealth: 200f);
            enemy3.PositionX = 12f;
            
            Log($"  英雄: {hero}");
            Log($"  敌人A: {enemy1} (低血量, 距离: 8)");
            Log($"  敌人B: {enemy2} (满血, 距离: 5)");
            Log($"  敌人C: {enemy3} (满血, 距离: 12)");
            Log("");
            
            // 创建行为树
            Log("【4】目标选择行为树 (使用 BTCore)");
            Output.Line();
            Log("  行为树结构:");
            Log("    EntryNode");
            Log("      └─ Selector (选择目标)");
            Log("           ├─ Sequence (低血量敌人)");
            Log("           │    ├─ Condition: HasLowHp");
            Log("           │    └─ Action: SelectLowHp");
            Log("           ├─ Sequence (最近敌人)");
            Log("           │    ├─ Condition: HasNearest");
            Log("           │    └─ Action: SelectNearest");
            Log("           └─ Action (选择自己)");
            Log("");
            
            // 创建行为树上下文
            var btContext = new Phase2_BTContext
            {
                Caster = hero,
                Target = enemy1,
                Enemies = new List<Phase0.Entities.TargetEntity> { enemy1, enemy2, enemy3 }
            };
            
            // 构建行为树
            Log("  --- 构建行为树 ---");
            var tree = BuildTargetSelectionTree(btContext);
            Log($"  行为树已创建，包含 {tree.BTData.Nodes.Count} 个节点");
            
            // 执行行为树
            Log("");
            Log("  --- 执行行为树 ---");
            tree.Enable();
            tree.BTData.Nodes.ForEach(node => {
                if (node is EntryNode entry) {
                    Log($"  EntryNode 状态: {entry.State}");
                }
            });
            
            // 演示节点执行
            Log("");
            Log("  --- 节点执行流程 ---");
            var btData = tree.BTData;
            for (int i = 0; i < btData.Nodes.Count; i++)
            {
                var node = btData.Nodes[i];
                Log($"  [{i}] {node.GetType().Name}: {node.Name} -> {node.State}");
            }
            
            Log("");
            Log($"  决策结果: 选择目标 {btContext.BestTarget ?? (Phase0.Entities.TargetEntity)null}");
            Log($"  (因为存在低血量敌人，行为树选择了第一个匹配条件)");
            
            Log("");
            
            // 连击行为树
            Log("【5】连击行为树");
            Output.Line();
            Log("  行为树结构:");
            Log("    EntryNode");
            Log("      └─ Selector (连击选择)");
            Log("           ├─ Sequence (终极技)");
            Log("           │    ├─ Condition: Combo >= 3");
            Log("           │    └─ Action: CastUltimate");
            Log("           ├─ Sequence (重击)");
            Log("           │    ├─ Condition: Combo == 2");
            Log("           │    └─ Action: CastHeavy");
            Log("           └─ Action (轻击)");
            
            Output.Line();
            Log("  --- 演示连击流程 ---");
            
            // 重置连击
            btContext.ComboCount = 0;
            
            // 第一次攻击
            Log("  第一次攻击:");
            btContext.ComboCount = 1;
            var comboTree = BuildComboTree(btContext);
            ExecuteTree(comboTree);
            Log($"    连击数: {btContext.ComboCount}, 技能: {btContext.SelectedSkill}");
            
            // 第二次攻击
            Log("  第二次攻击:");
            btContext.ComboCount = 2;
            comboTree = BuildComboTree(btContext);
            ExecuteTree(comboTree);
            Log($"    连击数: {btContext.ComboCount}, 技能: {btContext.SelectedSkill}");
            
            // 第三次攻击 (触发终极技)
            Log("  第三次攻击 (触发终极技):");
            btContext.ComboCount = 3;
            comboTree = BuildComboTree(btContext);
            ExecuteTree(comboTree);
            Log($"    连击数: {btContext.ComboCount}, 技能: {btContext.SelectedSkill}");
            
            Log("");
            
            // 节点类型说明
            Log("【6】BTCore 节点类型");
            Output.Bullet("EntryNode: 入口节点，行为树执行的起点");
            Output.Bullet("Sequence: 序列组合，子节点依次执行，全成功才成功");
            Output.Bullet("Selector: 选择组合，子节点依次执行，有成功就停止");
            Output.Bullet("Parallel: 并行组合，同时执行所有子节点");
            Output.Bullet("Condition: 条件节点，检查条件");
            Output.Bullet("Action: 动作节点，执行具体操作");
            Output.Bullet("Decorator: 装饰节点，修改子节点行为");
            Log("");
            
            // 节点状态说明
            Log("【7】NodeState 节点状态");
            Output.Bullet("Inactive: 未激活");
            Output.Bullet("Running: 正在执行");
            Output.Bullet("Success: 执行成功");
            Output.Bullet("Failure: 执行失败");
            Log("");
            
            // 预告下一阶段
            Log("【8】下一阶段预告 (Phase3: HFSM)");
            Output.Bullet("引入层级状态机管理角色状态");
            Output.Bullet("Idle / Casting / Channeling / Recovery 等状态");
            Output.Bullet("状态转换触发 Trigger");
            Output.Bullet("Trigger 结果可请求状态转换");
            Log("");
            
            Output.Divider();
            Log("【总结】BTCore 框架提供了完整的运行时行为树能力");
            Log("       支持黑板数据、节点组合、条件中断等高级特性");
            Output.Divider();
        }
        
        /// <summary>
        /// 执行行为树
        /// </summary>
        private void ExecuteTree(BTree tree)
        {
            tree.Enable();
            // 在真实运行时，会每帧调用 Update，这里简化处理
            var entry = tree.BTData.EntryNode?.GetChild();
            if (entry != null)
            {
                entry.Update();
            }
        }
        
        /// <summary>
        /// 构建目标选择行为树
        /// </summary>
        private BTree BuildTargetSelectionTree(Phase2_BTContext ctx)
        {
            var tree = new BTree();
            var data = new BTData();
            
            // 创建入口节点
            var entryNode = new EntryNode { Name = "Entry" };
            data.Nodes.Add(entryNode);
            
            // 创建选择器
            var selector = new Selector { Name = "TargetSelector" };
            data.Nodes.Add(selector);
            
            // 优先级1：选择低血量敌人
            var lowHpSeq = new Sequence { Name = "LowHpSequence" };
            data.Nodes.Add(lowHpSeq);
            
            var lowHpCond = new Phase2_ConditionNode { Name = "HasLowHp", Condition = () => ctx.Enemies.Exists(e => e.Health < e.MaxHealth * 0.3f) };
            data.Nodes.Add(lowHpCond);
            
            var lowHpAction = new Phase2_ActionNode { Name = "SelectLowHp", Action = () => {
                ctx.BestTarget = ctx.Enemies.Find(e => e.Health < e.MaxHealth * 0.3f);
                Log($"      -> 选择低血量敌人: {ctx.BestTarget}");
            }};
            data.Nodes.Add(lowHpAction);
            
            // 优先级2：选择最近敌人
            var nearestSeq = new Sequence { Name = "NearestSequence" };
            data.Nodes.Add(nearestSeq);
            
            var nearestCond = new Phase2_ConditionNode { Name = "HasNearest", Condition = () => ctx.Enemies.Count > 0 };
            data.Nodes.Add(nearestCond);
            
            var nearestAction = new Phase2_ActionNode { Name = "SelectNearest", Action = () => {
                ctx.BestTarget = ctx.Enemies.Count > 0 ? ctx.Enemies[0] : null;
                Log($"      -> 选择最近敌人: {ctx.BestTarget}");
            }};
            data.Nodes.Add(nearestAction);
            
            // 默认：选择自己
            var selfAction = new Phase2_ActionNode { Name = "SelectSelf", Action = () => {
                ctx.BestTarget = null;
                Log($"      -> 未选择目标");
            }};
            data.Nodes.Add(selfAction);
            
            // 设置父子关系
            entryNode.ChildGuid = selector.Guid;
            selector.AddChild(lowHpSeq);
            lowHpSeq.AddChild(lowHpCond);
            lowHpSeq.AddChild(lowHpAction);
            selector.AddChild(nearestSeq);
            nearestSeq.AddChild(nearestCond);
            nearestSeq.AddChild(nearestAction);
            selector.AddChild(selfAction);
            
            data.EntryNode = entryNode;
            tree.BTData = data;
            tree.RebuildTree();
            
            return tree;
        }
        
        /// <summary>
        /// 构建连击行为树
        /// </summary>
        private BTree BuildComboTree(Phase2_BTContext ctx)
        {
            var tree = new BTree();
            var data = new BTData();
            
            // 创建入口节点
            var entryNode = new EntryNode { Name = "Entry" };
            data.Nodes.Add(entryNode);
            
            // 创建选择器
            var selector = new Selector { Name = "ComboSelector" };
            data.Nodes.Add(selector);
            
            // 连击 >= 3：释放终结技
            var ultimateSeq = new Sequence { Name = "UltimateSequence" };
            data.Nodes.Add(ultimateSeq);
            
            var combo3Cond = new Phase2_ConditionNode { Name = "Combo3", Condition = () => ctx.ComboCount >= 3 };
            data.Nodes.Add(combo3Cond);
            
            var ultimateAction = new Phase2_ActionNode { Name = "CastUltimate", Action = () => {
                ctx.SelectedSkill = "终结技";
                ctx.ComboCount = 0;
                Log($"      -> 释放终结技! 连击重置");
            }};
            data.Nodes.Add(ultimateAction);
            
            // 连击 == 2：释放重击
            var heavySeq = new Sequence { Name = "HeavySequence" };
            data.Nodes.Add(heavySeq);
            
            var combo2Cond = new Phase2_ConditionNode { Name = "Combo2", Condition = () => ctx.ComboCount == 2 };
            data.Nodes.Add(combo2Cond);
            
            var heavyAction = new Phase2_ActionNode { Name = "CastHeavy", Action = () => {
                ctx.SelectedSkill = "重击";
                ctx.ComboCount++;
                Log($"      -> 释放重击! 连击+1");
            }};
            data.Nodes.Add(heavyAction);
            
            // 默认：轻击
            var lightAction = new Phase2_ActionNode { Name = "CastLight", Action = () => {
                ctx.SelectedSkill = "轻击";
                ctx.ComboCount++;
                Log($"      -> 释放轻击! 连击+1");
            }};
            data.Nodes.Add(lightAction);
            
            // 设置父子关系
            entryNode.ChildGuid = selector.Guid;
            selector.AddChild(ultimateSeq);
            ultimateSeq.AddChild(combo3Cond);
            ultimateSeq.AddChild(ultimateAction);
            selector.AddChild(heavySeq);
            heavySeq.AddChild(combo2Cond);
            heavySeq.AddChild(heavyAction);
            selector.AddChild(lightAction);
            
            data.EntryNode = entryNode;
            tree.BTData = data;
            tree.RebuildTree();
            
            return tree;
        }
    }
    
    // ============================================
    // Phase2 行为树实现 (使用 BTCore 框架类型)
    // ============================================
    
    /// <summary>
    /// 行为树上下文
    /// </summary>
    public class Phase2_BTContext
    {
        public Phase0.Entities.SkillEntity Caster { get; set; }
        public Phase0.Entities.TargetEntity Target { get; set; }
        public Phase0.Entities.TargetEntity BestTarget { get; set; }
        public List<Phase0.Entities.TargetEntity> Enemies { get; set; } = new();
        public int ComboCount { get; set; }
        public string SelectedSkill { get; set; }
        public string AIDecision { get; set; }
    }
    
    /// <summary>
    /// 条件节点 - BTCore 条件节点的自定义实现
    /// </summary>
    public class Phase2_ConditionNode : Condition
    {
        public Func<bool> Condition { get; set; }
        
        public Phase2_ConditionNode()
        {
            Name = "Condition";
        }
        
        protected override bool Validate()
        {
            return Condition?.Invoke() ?? false;
        }
    }
    
    /// <summary>
    /// 动作节点 - BTCore 动作节点的自定义实现
    /// </summary>
    public class Phase2_ActionNode : BTAction
    {
        public BTSystemAction Action { get; set; }
        
        public Phase2_ActionNode()
        {
            Name = "Action";
        }
        
        protected override NodeState OnUpdate()
        {
            Action?.Invoke();
            return NodeState.Success;
        }
    }
}
