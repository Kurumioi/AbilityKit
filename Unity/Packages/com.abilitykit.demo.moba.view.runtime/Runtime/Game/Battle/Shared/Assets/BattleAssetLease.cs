using System.Collections.Generic;

namespace AbilityKit.Game.Battle.Shared.Assets
{
    /// <summary>
    /// 默认的 <see cref="IBattleAssetLease"/> 实现。
    /// 记录本次加载的资源路径集合，Dispose 后标记为非活跃。
    /// 当前阶段为引用标记实现（不实际调用 Resources.UnloadAsset，由 Unity 侧适配器扩展）。
    /// </summary>
    public sealed class BattleAssetLease : IBattleAssetLease
    {
        private volatile bool _active = true;

        public bool IsActive => _active;

        public long LaunchGeneration { get; }

        /// <summary>本次租约持有的资源路径集合（仅供诊断 / Unity 侧释放使用）。</summary>
        public IReadOnlyList<string> AssetPaths { get; }

        public BattleAssetLease(long launchGeneration, IReadOnlyList<string> assetPaths)
        {
            LaunchGeneration = launchGeneration;
            AssetPaths = assetPaths ?? System.Array.Empty<string>();
        }

        public void Dispose()
        {
            _active = false;
        }
    }
}
