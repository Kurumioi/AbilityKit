using System;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Core.Logging;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Game.Battle;
using AbilityKit.Game.Battle.Moba.Config;
using AbilityKit.Network.Abstractions;

namespace AbilityKit.Game.Flow
{
    internal sealed class SessionOrchestrator
    {
        private readonly BattleSessionState _state;
        private readonly BattleSessionHandles _handles;
        private readonly ISessionOrchestratorHost _host;

        public SessionOrchestrator(BattleSessionState state, BattleSessionHandles handles, ISessionOrchestratorHost host)
        {
            _state = state;
            _handles = handles;
            _host = host;
        }

        public float GetFixedDeltaSeconds()
        {
            var plan = _host.Plan;
            return 1f / ResolveTickRate(plan);
        }

        public void StartSession()
        {
            StopSession();

            var plan = _host.Plan;
            StartLogicSession(plan);
            StartAuxiliaryWorlds(plan);
            _host.InvokeSessionStartingPipeline();
            ResetTickState();
            BindBattleContext();
            _host.InvokeReplaySetupPipeline();
        }

        public void StopSession()
        {
            if (_handles.Session == null) return;

            try
            {
                _handles.Session.FrameReceived -= _host.FrameReceivedHandler;

                _host.InvokeSessionStoppingPipeline();
                BattleLogicSessionHost.Stop();
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "[BattleSessionFeature] StopSession failed");
            }
            finally
            {
                DisposeContextRecordWriter();
                DisposeRuntimeResources();
                _host.ResetHandles();
            }
        }

        private void StartLogicSession(BattleStartPlan plan)
        {
            _handles.Session = _host.StartBattleLogicSession(BuildSessionOptions(plan));
            _handles.Session.FrameReceived += _host.FrameReceivedHandler;
        }

        private static BattleLogicSessionOptions BuildSessionOptions(BattleStartPlan plan)
        {
            return new BattleLogicSessionOptions
            {
                Mode = ResolveLogicMode(plan),
                WorldId = new WorldId(plan.WorldId),
                WorldType = plan.WorldType,
                ClientId = plan.ClientId,
                PlayerId = plan.PlayerId,

                ScanAssemblies = new[]
                {
                    typeof(AbilityKit.Ability.World.Services.WorldServiceContainerFactory).Assembly,
                    typeof(BattleLogicSession).Assembly,
                    typeof(AbilityKit.Demo.Moba.Systems.MobaWorldBootstrapModule).Assembly,
                    typeof(BattleSessionFeature).Assembly,
                },
                NamespacePrefixes = new[] { "AbilityKit" },

                AutoConnect = false,
                AutoCreateWorld = false,
                AutoJoin = false,
            };
        }

        private static BattleLogicMode ResolveLogicMode(BattleStartPlan plan)
        {
            return ShouldUseRemoteLogic(plan)
                ? BattleLogicMode.Remote
                : BattleLogicMode.Local;
        }

        private static bool ShouldUseRemoteLogic(BattleStartPlan plan)
        {
            return plan.SyncMode == BattleSyncMode.SnapshotAuthority || IsGatewayRemoteTransport(plan);
        }

        private void StartAuxiliaryWorlds(BattleStartPlan plan)
        {
            if (IsGatewayRemoteTransport(plan))
            {
                _host.StartRemoteDrivenLocalWorld();
            }

            if (plan.EnableConfirmedAuthorityWorld)
            {
                _host.StartConfirmedAuthorityWorld();
            }
        }

        private void ResetTickState()
        {
            _state.Tick.Reset();
        }

        private void BindBattleContext()
        {
            SessionContextBinder.BindRuntimeSession(_host.Context, _state, _handles);
        }

        private void DisposeContextRecordWriter()
        {
            try
            {
                var ctx = _host.Context;
                if (ctx == null) return;

                ctx.InputRecordWriter?.Dispose();
                ctx.InputRecordWriter = null;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "[BattleSessionFeature] Dispose InputRecordWriter on StopSession failed");
            }
        }

        private void DisposeRuntimeResources()
        {
            _host.TryDestroyBattleWorlds();
            _host.DisposeSnapshotRouting();
            _host.DisposeConfirmedView();
            _host.DisposeRemoteDrivenWorld();
            _host.DisposeConfirmedWorld();
            _host.DisposeNetworkIoDispatcher();
        }

        private static int ResolveTickRate(BattleStartPlan plan)
        {
            if (IsGatewayRemoteTransport(plan)) return 30;

            var tickRate = plan.TickRate;
            return tickRate > 0 ? tickRate : 30;
        }

        private static bool IsGatewayRemoteTransport(BattleStartPlan plan)
        {
            return plan.HostMode == BattleStartConfig.BattleHostMode.GatewayRemote && plan.UseGatewayTransport;
        }
    }
}
