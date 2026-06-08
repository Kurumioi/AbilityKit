namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleSessionFeature
    {
        private void StartRemoteDrivenLocalWorld()
        {
            RemoteDrivenWorldInstaller.EnsureStarted(new RemoteDrivenWorldInstallOptions(
                _plan,
                _ctx,
                _handles.RemoteDriven,
                GetFixedDeltaSeconds(),
                ResolveIdealFrameLimit,
                () => DebugForceClientHashMismatch,
                () => _remoteDrivenLastTickedFrame = 0));
        }
    }
}
