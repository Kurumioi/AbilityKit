using System;
using System.Collections.Generic;
using AbilityKit.Game.Battle.Shared.Assets;

namespace AbilityKit.Game.Battle.Presentation.Features.Loading
{
    /// <summary>
    /// 战斗加载进度快照。
    /// 由 <see cref="IBattleAssetLoadCoordinator"/> 在加载过程中更新，
    /// Loading Screen Feature 在 OnGUI 中读取以渲染进度条与当前加载项。
    /// </summary>
    public sealed class BattleAssetLoadProgressSnapshot
    {
        public bool IsLoading { get; internal set; }
        public int LoadedCount { get; internal set; }
        public int TotalCount { get; internal set; }
        public string CurrentAssetKey { get; internal set; } = string.Empty;
        public bool Completed { get; internal set; }
        public bool Success { get; internal set; }
        public string ErrorMessage { get; internal set; } = string.Empty;
        public IReadOnlyList<BattleAssetLoadError> Errors { get; internal set; } = Array.Empty<BattleAssetLoadError>();

        public float Progress01 => TotalCount <= 0 ? 0f : LoadedCount / (float)TotalCount;

        internal void Reset()
        {
            IsLoading = false;
            LoadedCount = 0;
            TotalCount = 0;
            CurrentAssetKey = string.Empty;
            Completed = false;
            Success = false;
            ErrorMessage = string.Empty;
            Errors = Array.Empty<BattleAssetLoadError>();
        }
    }

    /// <summary>
    /// 战斗加载进度观察者。Loading Screen Feature 注册自身，
    /// 任意加载源（coordinator、未来可能多步加载）更新进度后通知 UI。
    /// </summary>
    public interface IBattleAssetLoadProgressObserver
    {
        void OnLoadStarted(BattleAssetLoadProgressSnapshot snapshot);
        void OnLoadProgressed(BattleAssetLoadProgressSnapshot snapshot);
        void OnLoadCompleted(BattleAssetLoadProgressSnapshot snapshot);
        void OnLoadCancelled(BattleAssetLoadProgressSnapshot snapshot);
    }

    /// <summary>
    /// 静态注册表：Loading Screen 在 OnAttach 注册，Coordinator 或其他加载驱动方在加载过程中触发。
    /// 允许没有完整 DI 链路的情况下也能驱动 UI 进度。
    /// </summary>
    public static class BattleAssetLoadProgressHub
    {
        private static IBattleAssetLoadProgressObserver _observer;

        public static void Register(IBattleAssetLoadProgressObserver observer)
        {
            _observer = observer;
        }

        public static void Unregister(IBattleAssetLoadProgressObserver observer)
        {
            if (ReferenceEquals(_observer, observer))
            {
                _observer = null;
            }
        }

        public static bool HasObserver => _observer != null;

        public static void NotifyStarted(BattleAssetLoadProgressSnapshot snapshot)
        {
            _observer?.OnLoadStarted(snapshot);
        }

        public static void NotifyProgressed(BattleAssetLoadProgressSnapshot snapshot)
        {
            _observer?.OnLoadProgressed(snapshot);
        }

        public static void NotifyCompleted(BattleAssetLoadProgressSnapshot snapshot)
        {
            _observer?.OnLoadCompleted(snapshot);
        }

        public static void NotifyCancelled(BattleAssetLoadProgressSnapshot snapshot)
        {
            _observer?.OnLoadCancelled(snapshot);
        }
    }
}