namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleSessionFeature
    {
        private void TickConfirmedAuthorityWorldSim(float deltaTime)
        {
            _confirmedLastTickedFrame = ConfirmedAuthorityWorldTickDriver.Tick(new ConfirmedAuthorityWorldTickOptions(
                _plan,
                _ctx,
                _handles.Confirmed,
                _worldCatchUp,
                _confirmedLastTickedFrame,
                GetFixedDeltaSeconds(),
                SessionSimRuntimeTuning.MaxCatchUpStepsPerUpdate));
        }
    }
}
