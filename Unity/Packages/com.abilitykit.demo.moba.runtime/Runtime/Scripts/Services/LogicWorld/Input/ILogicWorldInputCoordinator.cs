using System.Collections.Generic;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;

namespace AbilityKit.Demo.Moba.Services.LogicWorld
{
    /// <summary>
    /// 逻辑世界输入协调器统一接口，承接外部输入批次并交由具体逻辑层处理。
    /// </summary>
    public interface ILogicWorldInputCoordinator
    {
        void Submit(FrameIndex frame, IReadOnlyList<PlayerInputCommand> inputs);
    }
}
