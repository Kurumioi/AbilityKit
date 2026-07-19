using AbilityKit.Ability.Host;
using AbilityKit.Game.Flow.Battle.Modules;
using AbilityKit.Game.Flow.Modules;
using AbilityKit.Network.Abstractions;

namespace AbilityKit.Game.Flow
{
    public sealed partial class BattleSessionFeature
    {
        private void OnFrame(FramePacket packet)
        {
            if (_subFeatureHost != null)
            {
                var fctx = new FeatureModuleContext<BattleSessionFeature>(_phaseCtx, this);
                _subFeatureHost.ForEach<ISessionFramePacketTransformSubFeature<BattleSessionFeature>>(m => packet = m.TransformFramePacket(fctx, packet));
            }

            _lastFrame = packet.Frame.Value;

            if (!_firstFrameReceived)
            {
                _firstFrameReceived = true;
                _eventsCtrl.NotifyFirstFrameReceived(this);

                // Local sessions complete WorldInit before publishing their first frame and have no
                // authoritative room manifest to report. Preserve the explicit asset barrier while
                // leaving GatewayRemote sessions to complete it from the room loading workflow.
                if (_plan.HostMode == BattleStartConfig.BattleHostMode.Local)
                {
                    NotifyAssetsLoadCompleted();
                }
            }

            SessionContextBinder.BindLastFrame(_ctx, _state);

            if (_subFeatureHost != null)
            {
                var fctx = new FeatureModuleContext<BattleSessionFeature>(_phaseCtx, this);
                _subFeatureHost.ForEach<ISessionFrameReceivedSubFeature<BattleSessionFeature>>(m => m.OnFrameReceived(fctx, packet));
            }
        }
    }
}
