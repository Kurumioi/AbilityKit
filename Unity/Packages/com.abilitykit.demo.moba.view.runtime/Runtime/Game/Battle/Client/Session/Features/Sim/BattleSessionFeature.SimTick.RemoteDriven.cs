namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleSessionFeature
    {
        private void TickRemoteDrivenLocalSim(float deltaTime)
        {
            _remoteDrivenLastTickedFrame = RemoteDrivenWorldTickDriver.Tick(new RemoteDrivenWorldTickOptions(
                _plan,
                _handles.RemoteDriven,
                _worldCatchUp,
                _snapshots,
                _remoteDrivenLastTickedFrame,
                GetFixedDeltaSeconds(),
                SessionSimRuntimeTuning.MaxCatchUpStepsPerUpdate));
        }
    }
}
