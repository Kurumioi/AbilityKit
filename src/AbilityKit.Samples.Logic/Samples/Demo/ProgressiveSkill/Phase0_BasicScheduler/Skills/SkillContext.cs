namespace AbilityKit.Samples.Logic.Samples.Demo.ProgressiveSkill.Phase0.Skills
{
    /// <summary>
    /// 技能上下文 - 技能执行时的运行时环境
    /// </summary>
    public class SkillContext
    {
        /// <summary>
        /// 施法者
        /// </summary>
        public Phase0.Entities.SkillEntity Caster { get; }
        
        /// <summary>
        /// 目标
        /// </summary>
        public Phase0.Entities.TargetEntity Target { get; }
        
        /// <summary>
        /// 技能定义
        /// </summary>
        public SkillDefinition Skill { get; }
        
        /// <summary>
        /// 当前冷却剩余时间
        /// </summary>
        public float CooldownRemaining { get; set; }
        
        /// <summary>
        /// 技能当前阶段
        /// </summary>
        public SkillPhase CurrentPhase { get; set; }
        
        /// <summary>
        /// 阶段进度 (0-1)
        /// </summary>
        public float PhaseProgress { get; set; }
        
        /// <summary>
        /// 是否被打断
        /// </summary>
        public bool IsInterrupted { get; set; }
        
        /// <summary>
        /// 打断原因
        /// </summary>
        public string InterruptReason { get; set; }
        
        /// <summary>
        /// 是否完成
        /// </summary>
        public bool IsCompleted { get; set; }

        public SkillContext(
            Phase0.Entities.SkillEntity caster,
            Phase0.Entities.TargetEntity target,
            SkillDefinition skill)
        {
            Caster = caster;
            Target = target;
            Skill = skill;
            CooldownRemaining = 0f;
            CurrentPhase = SkillPhase.None;
            PhaseProgress = 0f;
            IsInterrupted = false;
            IsCompleted = false;
        }
    }
    
    /// <summary>
    /// 技能阶段
    /// </summary>
    public enum SkillPhase
    {
        None,
        Validation,
        Consuming,
        Casting,
        Effect,
        Recovery,
        Completed
    }
}
