using AbilityKit.Game.Battle;

namespace AbilityKit.Game.Flow
{
    internal sealed partial class BattleSessionHandles
    {
        internal BattleLogicSession Session;

        internal readonly SnapshotHandles Snapshot = new SnapshotHandles();
        internal readonly NetHandles Net = new NetHandles();

        internal readonly DispatcherHandles Dispatchers = new DispatcherHandles();

        internal readonly ReplayHandles Replay = new ReplayHandles();

        internal readonly PhaseHandles Phase = new PhaseHandles();

        internal readonly GatewayRoomHandles GatewayRoom = new GatewayRoomHandles();

        internal readonly ConfirmedHandles Confirmed = new ConfirmedHandles();

        internal readonly RemoteDrivenHandles RemoteDriven = new RemoteDrivenHandles();

        public void Reset()
        {
            Session = null;

            Snapshot.Reset();
            Net.Reset();
            Dispatchers.Reset();
            Replay.Reset();
            Phase.Reset();
            GatewayRoom.Reset();
            Confirmed.Reset();
            RemoteDriven.Reset();
        }
    }
}
