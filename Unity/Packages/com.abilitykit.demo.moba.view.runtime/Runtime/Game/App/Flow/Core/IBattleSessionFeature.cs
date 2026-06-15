using System;

namespace AbilityKit.Game.Flow
{
    public interface IBattleSessionFeature : IGamePhaseFeature
    {
        event Action SessionStarted;
        event Action FirstFrameReceived;
        event Action<Exception> SessionFailed;
    }
}
