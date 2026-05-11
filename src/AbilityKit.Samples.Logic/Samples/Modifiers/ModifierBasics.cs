using System;
using AbilityKit.Samples.Abstractions;

namespace AbilityKit.Samples.Logic.Samples.Modifiers
{
    /// <summary>
    /// ModifierBasics - 淇敼鍣ㄥ熀纭€
    /// </summary>
    [Sample]
    public sealed class ModifierBasics : SampleBase
    {
        public override string Title => "Modifier Basics";
        public override string Description => "?? AbilityKit.Modifiers ??????";
        public override SampleCategory Category => SampleCategory.Modifiers;

        protected override void OnRun()
        {
            Log("?????(Modifiers)");
            Output.Divider();

            Log("????????? RPG ?????????????");
            Log("");

            Log("????:");
            Output.Bullet("ModifierKey: ??????");
            Output.Bullet("ModifierData: ?????");
            Output.Bullet("ModifierOp: ???????");
            Output.Bullet("MagnitudeStrategyData: ????");

            Output.Divider();

            Log("???? (ModifierOp):");
            Output.Bullet("Additive:     ?? (+50)");
            Output.Bullet("PercentAdd:    ????? (+20%)");
            Output.Bullet("PercentMult:   ????? (x1.5)");
            Output.Bullet("Override:      ???");

            Output.Divider();

            Log("????:");
            Log("  Final = Base * (1 + PercentMult) * (1 + PercentAdd) + Additive");

            Output.Divider();

            Log("???:");
            Log("  Override > PercentMult > PercentAdd > Additive");
        }
    }
}
