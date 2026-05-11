using System;
using AbilityKit.Samples.Infrastructure;

namespace AbilityKit.Samples.Samples.Triggering
{
    /// <summary>
    /// TriggerWithCondition - ???????
    /// </summary>
    public sealed class TriggerWithCondition : SampleBase
    {
        public override string Title => "Trigger with Condition";
        public override string Description => "????????????";
        public override SampleCategory Category => SampleCategory.Triggering;

        protected override void OnRun()
        {
            Log("???????");
            Output.Divider();

            Log("????? Evaluate ?????????");
            Log("");

            Log("Evaluate ??:");
            Log("  bool Evaluate(in TArgs args, in ExecCtx<TCtx> ctx)");
            Log("  - ?? true: ???? Execute");
            Log("  - ?? false: ??????");

            Output.Divider();

            Log("??????:");
            Output.Bullet("?????: Health > 50%");
            Output.Bullet("????: Mana >= 30");
            Output.Bullet("????: HasTag(\"Stunned\") == false");
            Output.Bullet("????: CooldownReady(skillId)");
            Output.Bullet("????: Distance < 10m");

            Output.Divider();

            Log("????:");
            Output.Bullet("AllCondition: ???????");
            Output.Bullet("AnyCondition: ??????");
            Output.Bullet("NotCondition: ????");

            Output.Divider();

            Log("????:");
            Log("  [????]");
            Log("      |");
            Log("  [Trigger1.Evaluate] -> false (??)");
            Log("      |");
            Log("  [Trigger2.Evaluate] -> true");
            Log("      |");
            Log("  [Trigger2.Execute] -> ????");
        }
    }
}
