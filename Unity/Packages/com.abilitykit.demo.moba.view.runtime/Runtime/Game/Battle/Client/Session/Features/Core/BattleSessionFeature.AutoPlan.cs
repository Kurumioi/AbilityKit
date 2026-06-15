using System;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Core.Logging;
using AbilityKit.Protocol.Moba;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Game.Battle.Requests;

namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleSessionFeature
    {
        private void ApplyAutoPlanActions()
        {
            if (!_autoPlanLogged)
            {
                _autoPlanLogged = true;
            }

            var world = _plan.World;
            var sync = _plan.Sync;
            var gateway = _plan.Gateway;
            var auto = _plan.Auto;

            if (_plan.HostMode == BattleStartConfig.BattleHostMode.GatewayRemote && gateway.UseGatewayTransport)
            {
                Log.Info("[BattleSessionFeature] GatewayRemote transport active. Skipping AutoCreateWorld/AutoJoin (not applicable). Use GatewayAutoCreateRoom/GatewayAutoJoinRoom for room lifecycle. AutoConnect/AutoReady are supported.");
                if (auto.AutoConnect)
                {
                    Log.Info($"[BattleSessionFeature] GatewayRemote AutoConnect -> Connect() to {gateway.Host}:{gateway.Port}");
                    _session?.Connect();
                }

                if (auto.AutoReady)
                {
                    Log.Info($"[BattleSessionFeature] GatewayRemote AutoReady -> SubmitInput(Ready). worldId='{world.WorldId}' playerId={world.PlayerId} frame={_lastFrame + 1}");
                    var cmd = new PlayerInputCommand(new FrameIndex(_lastFrame + 1), new PlayerId(world.PlayerId), opCode: MobaOpCodes.Input.Ready, payload: Array.Empty<byte>());
                    _session?.SubmitInput(new SubmitInputRequest(new WorldId(world.WorldId), cmd));
                }
                return;
            }

            var isLocal = sync.SyncMode != BattleSyncMode.SnapshotAuthority && _plan.HostMode == BattleStartConfig.BattleHostMode.Local;
            if (isLocal) _session?.Connect();
            else if (auto.AutoConnect) _session?.Connect();

            if (auto.AutoCreateWorld) CreateWorld();
            if (auto.AutoJoin)
            {
                _session?.Join(new JoinWorldRequest(new WorldId(world.WorldId), new PlayerId(world.PlayerId)));
            }
            if (auto.AutoReady)
            {
                var cmd = new PlayerInputCommand(new FrameIndex(_lastFrame + 1), new PlayerId(world.PlayerId), opCode: MobaOpCodes.Input.Ready, payload: Array.Empty<byte>());
                _session?.SubmitInput(new SubmitInputRequest(new WorldId(world.WorldId), cmd));
            }
        }
    }
}

