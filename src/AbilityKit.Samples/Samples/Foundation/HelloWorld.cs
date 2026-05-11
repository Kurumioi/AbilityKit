using System;
using System.Collections.Generic;
using AbilityKit.Samples.Infrastructure;

namespace AbilityKit.Samples.Samples.Foundation
{
    /// <summary>
    /// HelloWorld - ??????
    /// </summary>
    public sealed class HelloWorld : SampleBase
    {
        public override string Title => "Hello World";
        public override string Description => "????????? Samples ??????";
        public override SampleCategory Category => SampleCategory.Foundation;

        protected override void OnRun()
        {
            Log("???? AbilityKit.Samples!");
            Log("???????????????????");

            Output.Divider();

            Log("Framework ????:");
            Output.Bullet("Core: ?????????????");
            Output.Bullet("Triggering: ??????");
            Output.Bullet("GameplayTags: ????");
            Output.Bullet("Modifiers: ?????");
            Output.Bullet("Pipeline: ?????");
            Output.Bullet("Flow: ???????");
            Output.Bullet("HFSM: ???????");
            Output.Bullet("Combat: ????");
            Output.Bullet("Abilities: ????");

            Output.Divider();

            Log("????? (Common):");
            Output.Bullet("Logger: ??????");
            Output.Bullet("Clock: ????");
            Output.Bullet("Entity: ?????");
            Output.Bullet("MathUtil: ????");

            Output.Divider();

            Log("??????? SampleBase ?????:");
            Output.Numbered(1, "Logger - ????");
            Output.Numbered(2, "Clock - ????");
            Output.Numbered(3, "Log/Warn/Error - ????");
            Output.Numbered(4, "AdvanceTime/SimulateFrames - ????");
        }
    }
}
