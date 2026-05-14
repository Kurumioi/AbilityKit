using System;
using AbilityKit.Demo.Moba.Console.Core.Battle.Context;
using AbilityKit.Demo.Moba.Console.Battle;

namespace AbilityKit.Demo.Moba.Console.Battle.Sync;

/// <summary>
/// 同步适配器工厂
/// 根据配置创建帧同步或状态同步适配器
/// </summary>
public static class SyncAdapterFactory
{
    /// <summary>
    /// 根据配置创建同步适配器
    /// </summary>
    /// <param name="context">战斗上下文</param>
    /// <param name="config">启动配置</param>
    /// <returns>同步适配器实例</returns>
    public static IBattleSyncAdapter Create(ConsoleBattleContext context, BattleStartConfig config)
    {
        return config.SyncMode switch
        {
            BattleSyncMode.Lockstep => CreateFrameSyncAdapter(context, config),
            BattleSyncMode.SnapshotAuthority => CreateStateSyncAdapter(context, config),
            BattleSyncMode.HybridPredictReconcile => CreateHybridSyncAdapter(context, config),
            _ => throw new ArgumentException($"Unknown SyncMode: {config.SyncMode}")
        };
    }

    /// <summary>
    /// 创建帧同步适配器
    /// </summary>
    private static IBattleSyncAdapter CreateFrameSyncAdapter(ConsoleBattleContext context, BattleStartConfig config)
    {
        Platform.Log.Sync($"[SyncFactory] Creating FrameSyncAdapter (Lockstep mode)");
        return new FrameSyncAdapter();
    }

    /// <summary>
    /// 创建状态同步适配器
    /// </summary>
    private static IBattleSyncAdapter CreateStateSyncAdapter(ConsoleBattleContext context, BattleStartConfig config)
    {
        Platform.Log.Sync($"[SyncFactory] Creating StateSyncAdapter (SnapshotAuthority mode)");
        return new StateSyncAdapter();
    }

    /// <summary>
    /// 创建混合模式适配器
    /// 混合模式使用帧同步 + 客户端预测
    /// 目前暂时返回帧同步适配器
    /// </summary>
    private static IBattleSyncAdapter CreateHybridSyncAdapter(ConsoleBattleContext context, BattleStartConfig config)
    {
        Platform.Log.Sync($"[SyncFactory] Creating HybridSyncAdapter (HybridPredictReconcile mode - using FrameSync for now)");
        // TODO: 实现混合模式
        // 混合模式需要：帧同步 + ClientPredictionRunner + RollbackCoordinator
        // 目前暂时使用帧同步适配器
        return new FrameSyncAdapter();
    }
}
