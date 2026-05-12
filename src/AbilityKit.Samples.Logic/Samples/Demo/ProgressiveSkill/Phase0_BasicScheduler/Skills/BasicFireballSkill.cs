namespace AbilityKit.Samples.Logic.Samples.Demo.ProgressiveSkill.Phase0.Skills
{
    /// <summary>
    /// 基础火球术 - Phase0 示例技能
    /// 展示最简单的技能执行方式：硬编码顺序执行
    /// </summary>
    public class BasicFireballSkill
    {
        private readonly SimpleSkillExecutor _executor;
        
        public BasicFireballSkill()
        {
            _executor = new SimpleSkillExecutor();
        }
        
        /// <summary>
        /// 施放火球术
        /// </summary>
        /// <param name="caster">施法者</param>
        /// <param name="target">目标</param>
        public void Cast(Entities.SkillEntity caster, Entities.TargetEntity target)
        {
            var skill = SkillDefinition.CreateFireball();
            var context = new SkillContext(caster, target, skill);
            
            _executor.Execute(context);
        }
    }
}
