using System;
using AbilityKit.Samples.Infrastructure;

namespace AbilityKit.Samples.Samples.Tags
{
    /// <summary>
    /// TagStack - ????
    /// </summary>
    public sealed class TagStack : SampleBase
    {
        public override string Title => "Tag Stack";
        public override string Description => "??????????";
        public override SampleCategory Category => SampleCategory.Tags;

        protected override void OnRun()
        {
            Log("???? (Tag Stack)");
            Output.Divider();

            Log("TagStack ???????????? Buff ????");
            Log("");

            Log("????:");
            Output.Bullet("stack.AddStack(tag, count)     // ????");
            Output.Bullet("stack.RemoveStack(tag, count)  // ????");
            Output.Bullet("stack.GetStackCount(tag)       // ????");
            Output.Bullet("stack.ResetAllStacks()            // ????");

            Output.Divider();

            Log("????:");
            Output.Bullet("??? 0 ???????");
            Output.Bullet("??????????");
            Output.Bullet("?????????????");

            Output.Divider();

            Log("??:");
            Log("  AddStack(\"Buff.AttackSpeed\", 5)  // ???? +5 ?");
            Log("  AddStack(\"Buff.AttackSpeed\", 3)  // +3 ? -> ? 8 ?");
            Log("  RemoveStack(\"Buff.AttackSpeed\", 6) // -6 ? -> ????");

            Output.Divider();

            Log("????:");
            Output.Bullet("??? Buff (? MOBA ?????)");
            Output.Bullet("????? Buff");
            Output.Bullet("????????????");
        }
    }
}
