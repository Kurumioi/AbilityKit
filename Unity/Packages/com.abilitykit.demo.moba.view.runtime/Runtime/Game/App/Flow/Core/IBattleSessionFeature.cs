using System;

namespace AbilityKit.Game.Flow
{
    public interface IBattleSessionFeature : IGamePhaseFeature
    {
        event Action SessionStarted;
        event Action FirstFrameReceived;
        event Action<Exception> SessionFailed;
        // 阶段 7a：真实资源加载完成信号（manifest barrier）。append-only。
        event Action AssetsLoadCompleted;
    }
}
