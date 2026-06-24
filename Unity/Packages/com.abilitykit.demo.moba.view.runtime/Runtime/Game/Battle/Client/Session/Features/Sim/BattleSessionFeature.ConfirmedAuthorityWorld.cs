namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleSessionFeature
    {
        private void StartConfirmedAuthorityWorld()
        {
            _worldInstaller.EnsureConfirmedAuthorityStarted(new ConfirmedAuthorityWorldInstallOptions(
                _plan,
                _ctx,
                _flow,
                _handles.Confirmed,
                _session != null,
                GetFixedDeltaSeconds(),
                ResolveIdealFrameLimit,
                () => _confirmedLastTickedFrame = 0));
        }
    }
}
