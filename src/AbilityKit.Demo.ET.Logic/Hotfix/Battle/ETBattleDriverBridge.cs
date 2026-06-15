namespace ET.Logic
{
    /// <summary>
    /// Battle driver bridge.
    /// </summary>
    public static class ETBattleDriverBridge
    {
        public static void Start(ETBattleComponent self)
        {
            self.BattleDriver?.Start();
        }

        public static void Stop(ETBattleComponent self)
        {
            self.BattleDriver?.Stop();
        }

        public static int GetCurrentFrame(ETBattleComponent self)
        {
            return self.BattleDriver?.CurrentFrame ?? 0;
        }

    }
}
