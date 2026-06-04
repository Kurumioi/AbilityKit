namespace AbilityKit.Demo.Moba.Console.Services
{
    /// <summary>
    /// 技能施放阶段
    /// </summary>
    public enum SkillCastStage
    {
        PreCast = 0,
        Cast = 1,
        Timeline = 2,
        Completed = 3,
        Interrupted = 4,
    }
}
