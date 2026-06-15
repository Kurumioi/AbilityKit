using System;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Core.Common.Log;
using AbilityKit.Core.Common.Record.Lockstep;
using AbilityKit.Ability.World.Abstractions;
using AbilityKit.Game.Battle.Component;
using AbilityKit.Game.Battle.Moba.Config;
using AbilityKit.Game.Flow.Battle.Replay;
using AbilityKit.Game.Flow.Modules;

namespace AbilityKit.Game.Flow
{
    internal sealed class SessionReplaySubFeature :
        ISessionSubFeature<BattleSessionFeature>,
        ISessionPreTickSubFeature<BattleSessionFeature>,
        ISessionReplaySetupSubFeature<BattleSessionFeature>,
        ISessionFrameReceivedSubFeature<BattleSessionFeature>,
        IGameModuleId,
        IGameModuleDependencies
    {
        public string Id => "session_replay";

        public System.Collections.Generic.IEnumerable<string> Dependencies => new[] { "session_events" };

        public void OnAttach(in FeatureModuleContext<BattleSessionFeature> ctx) { }

        public void OnDetach(in FeatureModuleContext<BattleSessionFeature> ctx) { }

        public void PreTick(in FeatureModuleContext<BattleSessionFeature> ctx, float deltaTime)
        {
            if (!BattleSessionFeatureRuntimeAccess.TryGet<ISessionReplayRuntime>(ctx, out var runtime)) return;

            runtime.Replay.PreTick(runtime.Plan, runtime.State, runtime.Handles, runtime.Context, (ISessionReplayHost)ctx.Feature);
        }

        public void SetupReplayOrRecord(in FeatureModuleContext<BattleSessionFeature> ctx)
        {
            if (!BattleSessionFeatureRuntimeAccess.TryGet<ISessionReplayRuntime>(ctx, out var runtime)) return;

            IBattleReplayDriverProvider provider = null;
            ctx.Phase.Features.TryGet(out provider);
            if (provider == null && ctx.Phase.Entry != null)
            {
                ctx.Phase.Entry.TryGet(out provider);
            }
            runtime.Replay.SetupReplayOrRecord(provider, runtime.Plan, runtime.Handles, runtime.Context);
        }

        public void OnFrameReceived(in FeatureModuleContext<BattleSessionFeature> ctx, FramePacket packet)
        {
            if (!BattleSessionFeatureRuntimeAccess.TryGet<ISessionReplayRuntime>(ctx, out var runtime)) return;

            runtime.Replay.OnFrameReceived(runtime.Plan, runtime.State, runtime.Handles, runtime.Context, packet);
        }

        public void Tick(in FeatureModuleContext<BattleSessionFeature> ctx, float deltaTime) { }

        public void RebindAll(in FeatureModuleContext<BattleSessionFeature> ctx) { }
    }
}
