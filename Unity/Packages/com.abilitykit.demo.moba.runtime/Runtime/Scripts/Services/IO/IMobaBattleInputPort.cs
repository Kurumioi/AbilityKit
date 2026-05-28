using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;

namespace AbilityKit.Demo.Moba.Services
{
    public interface IMobaBattleInputPort
    {
        void Submit(FrameIndex frame, IReadOnlyList<PlayerInputCommand> inputs);
    }
}
