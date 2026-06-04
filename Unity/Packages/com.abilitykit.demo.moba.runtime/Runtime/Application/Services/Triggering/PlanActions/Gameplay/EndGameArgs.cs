namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    public readonly struct EndGameArgs
    {
        public readonly int ReasonId;
        public readonly int WinTeamId;

        public EndGameArgs(int reasonId, int winTeamId)
        {
            ReasonId = reasonId;
            WinTeamId = winTeamId;
        }
    }
}
