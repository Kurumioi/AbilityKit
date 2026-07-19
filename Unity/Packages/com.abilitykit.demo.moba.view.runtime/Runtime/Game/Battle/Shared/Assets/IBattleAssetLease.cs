using System;

namespace AbilityKit.Game.Battle.Shared.Assets
{
    /// <summary>
    /// 战斗资源租约。加载成功后获得，战斗结束时 <see cref="IDisposable.Dispose"/> 释放。
    /// 用于 battle-scope 资源生命周期管理：一场战斗的资源在战斗结束时统一释放。
    /// </summary>
    public interface IBattleAssetLease : IDisposable
    {
        /// <summary>租约是否仍然活跃（未 Dispose）。</summary>
        bool IsActive { get; }

        /// <summary>对应的启动代次，用于校验租约归属。</summary>
        long LaunchGeneration { get; }
    }
}
