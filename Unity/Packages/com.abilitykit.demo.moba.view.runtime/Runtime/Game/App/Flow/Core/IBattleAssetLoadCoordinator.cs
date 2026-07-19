using System;

namespace AbilityKit.Game.Flow
{
    /// <summary>
    /// 战斗资源加载协调器：在 Flow LoadAssets 阶段驱动 BattleAssetLoadService，
    /// 完成后通过回调通知 BattleScopeManager 触发 AssetsLoadCompleted。
    /// 纯 C# 接口，便于测试。
    /// </summary>
    internal interface IBattleAssetLoadCoordinator
    {
        /// <summary>开始加载战斗资源。完成后调用 onComplete(onSuccess)。</summary>
        void StartLoading(Action<bool> onComplete);

        /// <summary>取消正在进行的加载。</summary>
        void Cancel();

        /// <summary>当前是否正在加载。</summary>
        bool IsLoading { get; }
    }
}
