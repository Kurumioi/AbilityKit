using System;
using System.Threading;
using AbilityKit.Game.Battle.Shared.Assets;

namespace AbilityKit.Game.Flow
{
    /// <summary>
    /// 默认 <see cref="IBattleAssetLoadCoordinator"/> 实现。
    /// 桥接 <see cref="IBattleAssetLoadService"/>（阶段 6 已实现）：
    /// 在 Flow LoadAssets 阶段按 manifest 异步加载全部必需资源，
    /// 全部成功（manifest barrier 通过）才回调 onSuccess=true。
    /// 首帧不再代表资源加载完成——只有真实 manifest barrier 通过才推进。
    /// </summary>
    internal sealed class BattleAssetLoadCoordinator : IBattleAssetLoadCoordinator
    {
        private readonly IBattleAssetLoadService _loadService;
        private readonly Func<BattleAssetManifest> _manifestProvider;

        private IBattleAssetLease _currentLease;
        private Action<bool> _pendingCallback;
        private CancellationTokenSource _cts;

        public bool IsLoading => _pendingCallback != null;

        public BattleAssetLoadCoordinator(
            IBattleAssetLoadService loadService,
            Func<BattleAssetManifest> manifestProvider)
        {
            _loadService = loadService ?? throw new ArgumentNullException(nameof(loadService));
            _manifestProvider = manifestProvider ?? throw new ArgumentNullException(nameof(manifestProvider));
        }

        public void StartLoading(Action<bool> onComplete)
        {
            if (_pendingCallback != null)
            {
                throw new InvalidOperationException("资源加载已在进行中。");
            }

            _pendingCallback = onComplete ?? throw new ArgumentNullException(nameof(onComplete));
            var manifest = _manifestProvider()
                ?? throw new InvalidOperationException("资源清单（manifest）为 null。");

            _cts = new CancellationTokenSource();
            LoadAsyncCore(manifest, _cts.Token);
        }

        private async void LoadAsyncCore(BattleAssetManifest manifest, CancellationToken cancellationToken)
        {
            bool success;
            try
            {
                var result = await _loadService
                    .LoadAsync(manifest, progress: null, cancellationToken)
                    .ConfigureAwait(true);

                if (result != null && result.Success && result.Lease != null && result.Lease.IsActive)
                {
                    _currentLease = result.Lease;
                    success = true;
                }
                else
                {
                    _currentLease = null;
                    success = false;
                }
            }
            catch (OperationCanceledException)
            {
                _currentLease = null;
                success = false;
            }
            catch (Exception)
            {
                _currentLease = null;
                success = false;
            }

            var cb = _pendingCallback;
            _pendingCallback = null;
            _cts?.Dispose();
            _cts = null;
            cb?.Invoke(success);
        }

        public void Cancel()
        {
            try
            {
                _cts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // 已释放，忽略。
            }

            var cb = _pendingCallback;
            _pendingCallback = null;
            _cts?.Dispose();
            _cts = null;
            _currentLease = null;
            cb?.Invoke(false);
        }

        /// <summary>释放当前持有的资源租约（战斗结束时调用）。</summary>
        public void ReleaseLease()
        {
            _currentLease?.Dispose();
            _currentLease = null;
        }
    }
}
