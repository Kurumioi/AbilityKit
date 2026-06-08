namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleSessionFeature
    {
        private void TryDestroyBattleWorlds()
        {
            SessionSimRuntimeDisposer.DestroyBattleWorlds(_plan, _handles);
        }

        private void DisposeConfirmedView()
        {
            SessionSimRuntimeDisposer.DisposeConfirmedView(
                _flow,
                _handles.Confirmed,
                DestroyEntityTree);
        }

        private void DisposeRemoteDrivenWorld()
        {
            SessionSimRuntimeDisposer.DisposeRemoteDrivenWorld(
                _handles.RemoteDriven,
                () => _remoteDrivenLastTickedFrame = 0);
        }

        private void DisposeConfirmedWorld()
        {
            SessionSimRuntimeDisposer.DisposeConfirmedWorld(
                _ctx,
                _handles.Confirmed,
                () => _confirmedLastTickedFrame = 0);
        }
    }
}
