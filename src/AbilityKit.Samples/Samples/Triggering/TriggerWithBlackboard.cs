using System;
using AbilityKit.Samples.Infrastructure;

namespace AbilityKit.Samples.Samples.Triggering
{
    /// <summary>
    /// TriggerWithBlackboard - ???????
    /// </summary>
    public sealed class TriggerWithBlackboard : SampleBase
    {
        public override string Title => "Trigger with Blackboard";
        public override string Description => "?????????????????";
        public override SampleCategory Category => SampleCategory.Triggering;

        protected override void OnRun()
        {
            Log("?????(Blackboard)");
            Output.Divider();

            Log("?????????????????????????");
            Log("");

            Log("????: IBlackboard");
            Output.Bullet("SetInt(key, value) / GetInt(key)");
            Output.Bullet("SetFloat(key, value) / GetFloat(key)");
            Output.Bullet("SetObject<T>(key, value) / GetObject<T>(key)");

            Output.Divider();

            Log("????:");
            Output.Bullet("???: combo = GetInt(\"combo\") + 1");
            Output.Bullet("Buff ??: SetBool(\"hasShield\", true)");
            Output.Bullet("????: SetObject(\"lastTarget\", target)");

            Output.Divider();

            Log("??????:");
            Log("  [Trigger1] -> ?? combo=1");
            Log("  [Trigger2] -> ?? combo>=3 -> ?? bonus");
            Log("  [Trigger3] -> ?? damage (??? bonus ??)");

            Output.Divider();

            Log("API ????:");
            Log("  AbilityKit.Triggering.Blackboard");
        }
    }
}
