using AbilityKit.Ability.Host;
using AbilityKit.Core.Common.Record.Lockstep;
using AbilityKit.Core.Common.SnapshotRouting;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Network.Abstractions;

namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleSessionFeature
    {
        BattleStartPlan INetAdapterContextHost.Plan => _plan;
        IWorld INetAdapterContextHost.RemoteDrivenWorld => _handles.RemoteDriven.World;
        IWorld INetAdapterContextHost.ConfirmedWorld => _handles.Confirmed.World;

        IRemoteFrameSource<PlayerInputCommand[]> INetAdapterContextHost.RemoteDrivenInputSource
        {
            get => _handles.RemoteDriven.InputSource;
            set => _handles.RemoteDriven.InputSource = value;
        }

        IConsumableRemoteFrameSource<PlayerInputCommand[]> INetAdapterContextHost.RemoteDrivenConsumable
        {
            get => _handles.RemoteDriven.Consumable;
            set => _handles.RemoteDriven.Consumable = value;
        }

        IRemoteFrameSink<PlayerInputCommand[]> INetAdapterContextHost.RemoteDrivenSink
        {
            get => _handles.RemoteDriven.Sink;
            set => _handles.RemoteDriven.Sink = value;
        }

        IRemoteFrameSource<PlayerInputCommand[]> INetAdapterContextHost.ConfirmedInputSource
        {
            get => _handles.Confirmed.InputSource;
            set => _handles.Confirmed.InputSource = value;
        }

        IConsumableRemoteFrameSource<PlayerInputCommand[]> INetAdapterContextHost.ConfirmedConsumable
        {
            get => _handles.Confirmed.Consumable;
            set => _handles.Confirmed.Consumable = value;
        }

        IRemoteFrameSink<PlayerInputCommand[]> INetAdapterContextHost.ConfirmedSink
        {
            get => _handles.Confirmed.Sink;
            set => _handles.Confirmed.Sink = value;
        }

        FrameSnapshotDispatcher INetAdapterContextHost.Snapshots => _snapshots;
    }
}
