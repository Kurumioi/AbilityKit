using UnityEngine;

namespace AbilityKit.Game.Battle.Shared.Assets
{
    /// <summary>
    /// 基于 <see cref="IAssetProvider"/>（默认 <see cref="ResourcesAssetProvider"/>）的
    /// <see cref="IBattleAssetSource"/> 桥接实现。将 UnityEngine 资源加载适配为纯 C# 抽象。
    /// </summary>
    public sealed class ResourcesBattleAssetSource : IBattleAssetSource
    {
        private readonly IAssetProvider _provider;

        public ResourcesBattleAssetSource(IAssetProvider provider)
        {
            _provider = provider ?? throw new System.ArgumentNullException(nameof(provider));
        }

        public bool TryLoad(string path, out object asset)
        {
            if (string.IsNullOrEmpty(path))
            {
                asset = null;
                return false;
            }

            // 优先按 TextAsset 加载（配置表 / JSON 资源），失败则尝试通用 Object。
            var text = _provider.Load<TextAsset>(path);
            if (text != null)
            {
                asset = text;
                return true;
            }

            var obj = _provider.Load<Object>(path);
            if (obj != null)
            {
                asset = obj;
                return true;
            }

            asset = null;
            return false;
        }
    }

    /// <summary>
    /// 基于 Unity Resources 的 <see cref="IBattleAssetLoadService"/> 适配器。
    /// 包装 <see cref="BattleAssetLoadService"/>，使用 <see cref="ResourcesAssetProvider.Shared"/>。
    /// </summary>
    public sealed class ResourcesBattleAssetLoadService : IBattleAssetLoadService
    {
        /// <summary>默认单例，使用 <see cref="ResourcesAssetProvider.Shared"/>。</summary>
        public static readonly ResourcesBattleAssetLoadService Default =
            new ResourcesBattleAssetLoadService(ResourcesAssetProvider.Shared);

        private readonly BattleAssetLoadService _inner;

        public ResourcesBattleAssetLoadService(IAssetProvider provider)
        {
            _inner = new BattleAssetLoadService(new ResourcesBattleAssetSource(provider));
        }

        public System.Threading.Tasks.Task<BattleAssetLoadResult> LoadAsync(
            BattleAssetManifest manifest,
            System.IProgress<BattleAssetLoadProgress> progress = null,
            System.Threading.CancellationToken cancellationToken = default)
        {
            return _inner.LoadAsync(manifest, progress, cancellationToken);
        }
    }
}
