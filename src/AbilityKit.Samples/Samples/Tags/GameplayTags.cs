using System;
using AbilityKit.Samples.Infrastructure;

namespace AbilityKit.Samples.Samples.Tags
{
    /// <summary>
    /// GameplayTags - ?? GameplayTags
    /// </summary>
    public sealed class GameplayTags : SampleBase
    {
        public override string Title => "Gameplay Tags";
        public override string Description => "?? AbilityKit.GameplayTags ?????";
        public override SampleCategory Category => SampleCategory.Tags;

        protected override void OnRun()
        {
            Log("???? (Gameplay Tags)");
            Output.Divider();

            Log("GameplayTags ???????????????");
            Log("");

            Log("????:");
            Output.Bullet("GameplayTag: ???????? \"Damage.Fire.Burning\"?");
            Output.Bullet("GameplayTagContainer: ????");
            Output.Bullet("GameplayTagManager: ?????");
            Output.Bullet("GameplayTagStack: ??????");

            Output.Divider();

            Log("????:");
            Output.Bullet("container.HasTag(tag)       // ????");
            Output.Bullet("container.HasTagAny(tags)  // ????");
            Output.Bullet("container.HasTagAll(tags)  // ????");

            Output.Divider();

            Log("????:");
            Log("  \"Damage\"");
            Log("    --> \"Damage.Fire\"");
            Log("          --> \"Damage.Fire.Burning\"");

            Output.Divider();

            Log("????:");
            Output.Bullet("Buff/Debuff ??");
            Output.Bullet("????: Unit.Hero, Unit.Minion");
            Output.Bullet("??: Team.Ally, Team.Enemy");
            Output.Bullet("??: Status.Stunned, Status.Invisible");

            Output.Divider();

            Log("API ????:");
            Log("  AbilityKit.GameplayTags.GameplayTags.Core");
        }
    }
}
