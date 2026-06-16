using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// ETMobaBattleDriver System.
    /// ET lifecycle owns component construction/destruction; the driver keeps only explicit runtime start/stop/tick methods.
    /// </summary>
    [EntitySystemOf(typeof(ETMobaBattleDriver))]
    [FriendOf(typeof(ETMobaBattleDriver))]
    public static partial class ETMobaBattleDriverSystem
    {
        [EntitySystem]
        private static void Awake(this ETMobaBattleDriver self)
        {
            Log.Info("[ETMobaBattleDriverSystem] Awake");
        }

        [EntitySystem]
        private static void Update(this ETMobaBattleDriver self)
        {
        }

        [EntitySystem]
        private static void Destroy(this ETMobaBattleDriver self)
        {
            self.Destroy();
        }
    }
}
