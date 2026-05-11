using System;
using AbilityKit.Samples.Infrastructure;

namespace AbilityKit.Samples.Samples.Tags
{
    /// <summary>
    /// TagRequirements - ????
    /// </summary>
    public sealed class TagRequirements : SampleBase
    {
        public override string Title => "Tag Requirements";
        public override string Description => "???????????";
        public override SampleCategory Category => SampleCategory.Tags;

        protected override void OnRun()
        {
            Log("????(Tag Requirements)");
            Output.Divider();

            Log("TagRequirements ?????????????????");
            Log("");

            Log("????:");
            Log("  class GameplayTagRequirements");
            Log("  {");
            Log("      GameplayTagContainer RequireTags;   // ?????");
            Log("      GameplayTagContainer IgnoreTags;    // ?????");
            Log("  }");

            Output.Divider();

            Log("????:");
            Log("  RequirementsMet(container) ?? true ????:");
            Output.Bullet("container ???? RequireTags");
            Output.Bullet("container ????? IgnoreTags");

            Output.Divider();

            Log("??:");
            Log("  requirements.RequireTags = { \"Buff.Attack\" }");
            Log("  requirements.IgnoreTags = { \"Debuff.Silence\" }");
            Log("");
            Log("  ?? A: { \"Buff.Attack\", \"Buff.Speed\" } -> Pass");
            Log("  ?? B: { \"Buff.Speed\" }             -> Fail (?? Attack)");
            Log("  ?? C: { \"Buff.Attack\", \"Debuff.Silence\" } -> Fail (? Silence)");

            Output.Divider();

            Log("????:");
            Output.Bullet("????????");
            Output.Bullet("Buff ????");
            Output.Bullet("????");
        }
    }
}
