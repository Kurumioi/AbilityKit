using System;
using System.Collections.Generic;
using AbilityKit.Core.Common.SnapshotRouting;

namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleSessionFeature
    {
        private void EnsureSnapshotRoutingBuilt() => BuildSnapshotRouting();

        private void DisposeSnapshotRoutingIfAny() => DisposeSnapshotRouting();

        private void BuildSnapshotRouting()
        {
            var catalog = new SnapshotRegistryCatalog()
                .Add("battle", AbilityKit.Game.Flow.Snapshot.BattleSnapshotRegistry.RegisterAll)
                .Add("shared", AbilityKit.Game.Flow.Snapshot.SharedSnapshotRegistry.RegisterAll);

            ISet<string> enabledRegistryIds = null;
            if (_plan.EnabledSnapshotRegistryIds != null && _plan.EnabledSnapshotRegistryIds.Length > 0)
            {
                enabledRegistryIds = new HashSet<string>(_plan.EnabledSnapshotRegistryIds, StringComparer.Ordinal);
            }

            // Use the standard FrameSnapshotDispatcher, subscribed directly to BattleLogicSession.FrameReceived
            _snapshots = new AbilityKit.Core.Common.SnapshotRouting.FrameSnapshotDispatcher();
            _session.FrameReceived += OnSessionFrameReceived;

            _routing = enabledRegistryIds == null
                ? SnapshotRoutingBuilder.Build(_ctx, _snapshots, catalog.Registries)
                : SnapshotRoutingBuilder.Build(_ctx, _snapshots, catalog.Registries, enabledRegistryIds);

            _pipeline = _routing.Pipeline;
            _cmdHandler = _routing.CmdHandler;

            _netAdapterCtx = new BattleSessionNetAdapterContext((INetAdapterContextHost)this);
            _netAdapter = new BattleSessionNetAdapter(_netAdapterCtx);

            if (_ctx != null)
            {
                _ctx.FrameSnapshots = _snapshots;
                _ctx.SnapshotPipeline = _pipeline;
                _ctx.CmdHandler = _cmdHandler;
            }
        }

        private void DisposeSnapshotRouting()
        {
            _session.FrameReceived -= OnSessionFrameReceived;

            _routing?.Dispose();
            _routing = null;

            if (_ctx != null)
            {
                _ctx.SnapshotPipeline = null;
                _ctx.CmdHandler = null;
                _ctx.FrameSnapshots = null;
            }

            _netAdapter = null;
            _netAdapterCtx = null;
            _cmdHandler = null;
            _pipeline = null;
            _snapshots = null;
        }

        private void OnSessionFrameReceived(AbilityKit.Ability.Host.FramePacket packet)
        {
            _snapshots.Feed(packet);
        }
    }
}
