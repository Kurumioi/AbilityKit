namespace AbilityKit.Game.Flow
{
    internal static class SessionSimRuntimeTuning
    {
        public const int MaxCatchUpStepsPerUpdate = 5;
        public const int RetainedInputFrames = 120;

        public static int NormalizeInputDelayFrames(int inputDelayFrames)
        {
            return inputDelayFrames < 0 ? 0 : inputDelayFrames;
        }
    }
}
