using AbilityKit.Core.Snapshots.Routing;
using AbilityKit.Game.Battle;

namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleContext
    {
        private FrameSnapshotDispatcher _frameSnapshots;
        private SnapshotPipeline _snapshotPipeline;
        private SnapshotCmdHandler _cmdHandler;

        public FrameSnapshotDispatcher FrameSnapshots
        {
            get => _frameSnapshots;
        }

        public SnapshotPipeline SnapshotPipeline
        {
            get => _snapshotPipeline;
        }

        public SnapshotCmdHandler CmdHandler
        {
            get => _cmdHandler;
        }

        internal void BindSnapshotRouting(
            FrameSnapshotDispatcher snapshots,
            SnapshotPipeline pipeline,
            SnapshotCmdHandler cmdHandler)
        {
            _frameSnapshots = snapshots;
            _snapshotPipeline = pipeline;
            _cmdHandler = cmdHandler;
        }

        internal void ClearSnapshotRouting()
        {
            _frameSnapshots = null;
            _snapshotPipeline = null;
            _cmdHandler = null;
        }

        internal bool TryGetFrameSnapshots(out FrameSnapshotDispatcher snapshots)
        {
            snapshots = _frameSnapshots;
            return snapshots != null;
        }

        internal bool TryGetSnapshotPipeline(out SnapshotPipeline pipeline)
        {
            pipeline = _snapshotPipeline;
            return pipeline != null;
        }

        internal bool TryGetSnapshotCmdHandler(out SnapshotCmdHandler cmdHandler)
        {
            cmdHandler = _cmdHandler;
            return cmdHandler != null;
        }

        internal bool IsSnapshotRoutingBoundTo(
            FrameSnapshotDispatcher snapshots,
            SnapshotPipeline pipeline,
            SnapshotCmdHandler cmdHandler)
        {
            return ReferenceEquals(_frameSnapshots, snapshots)
                && ReferenceEquals(_snapshotPipeline, pipeline)
                && ReferenceEquals(_cmdHandler, cmdHandler);
        }
    }
}
