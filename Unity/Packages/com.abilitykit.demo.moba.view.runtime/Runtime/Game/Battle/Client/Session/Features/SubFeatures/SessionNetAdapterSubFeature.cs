using AbilityKit.Ability.Host;
using AbilityKit.Game.Flow.Modules;

namespace AbilityKit.Game.Flow
{
    internal sealed class SessionNetAdapterSubFeature :
        ISessionSubFeature<BattleSessionFeature>,
        ISessionFramePacketTransformSubFeature<BattleSessionFeature>,
        IGameModuleId,
        IGameModuleDependencies
    {
        public string Id => "session_net_adapter";

        public System.Collections.Generic.IEnumerable<string> Dependencies => new[] { "session_events" };

        public void OnAttach(in FeatureModuleContext<BattleSessionFeature> ctx) { }

        public void OnDetach(in FeatureModuleContext<BattleSessionFeature> ctx) { }

        public FramePacket TransformFramePacket(in FeatureModuleContext<BattleSessionFeature> ctx, FramePacket packet)
        {
            if (!BattleSessionFeatureRuntimeAccess.TryGet<ISessionNetAdapterRuntime>(ctx, out var runtime)) return packet;

            return runtime.Net.TransformFramePacket(runtime.Session, runtime.NetAdapter, packet);
        }

        public void Tick(in FeatureModuleContext<BattleSessionFeature> ctx, float deltaTime) { }

        public void RebindAll(in FeatureModuleContext<BattleSessionFeature> ctx) { }
    }
}
