using System;
using AbilityKit.Demo.Moba.Console.Core.Battle.Context;
using AbilityKit.Demo.Moba.Console.Battle;
using AbilityKit.Demo.Moba.Console.Simulation;
using AbilityKit.Demo.Moba.Share;
using ShareSyncMode = AbilityKit.Demo.Moba.Share.SyncMode;

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
            ShareSyncMode.Lockstep => CreateFrameSyncAdapter(context, config, null),
            ShareSyncMode.SnapshotAuthority => CreateStateSyncAdapter(context, config, null),
            ShareSyncMode.Hybrid => CreateHybridSyncAdapter(context, config, null),
            _ => throw new ArgumentException($"Unknown SyncMode: {config.SyncMode}")
        };
    }

    /// <summary>
    /// 根据配置创建同步适配器（带 Session）
    /// </summary>
    /// <param name="context">战斗上下文</param>
    /// <param name="config">启动配置</param>
    /// <param name="session">模拟会话（用于获取角色状态）</param>
    /// <returns>同步适配器实例</returns>
    public static IBattleSyncAdapter Create(ConsoleBattleContext context, BattleStartConfig config, ISimulatedBattleSession? session)
    {
        return config.SyncMode switch
        {
            ShareSyncMode.Lockstep => CreateFrameSyncAdapter(context, config, session),
            ShareSyncMode.SnapshotAuthority => CreateStateSyncAdapter(context, config, session),
            ShareSyncMode.Hybrid => CreateHybridSyncAdapter(context, config, session),
            _ => throw new ArgumentException($"Unknown SyncMode: {config.SyncMode}")
        };
    }

    /// <summary>
    /// 创建帧同步适配器
    /// </summary>
    private static IBattleSyncAdapter CreateFrameSyncAdapter(ConsoleBattleContext context, BattleStartConfig config, ISimulatedBattleSession? session)
    {
        Platform.Log.Sync($"[SyncFactory] Creating FrameSyncAdapter (Lockstep mode)");
        var adapter = new FrameSyncAdapter();
        adapter.Initialize(context, config, session);
        return adapter;
    }

    /// <summary>
    /// 创建状态同步适配器
    /// </summary>
    private static IBattleSyncAdapter CreateStateSyncAdapter(ConsoleBattleContext context, BattleStartConfig config, ISimulatedBattleSession? session)
    {
        Platform.Log.Sync($"[SyncFactory] Creating StateSyncAdapter (SnapshotAuthority mode)");
        return new StateSyncAdapter();
    }

    /// <summary>
    /// 创建混合模式适配器
    /// 混合模式使用帧同步 + 客户端预测 + 回滚
    /// </summary>
    private static IBattleSyncAdapter CreateHybridSyncAdapter(ConsoleBattleContext context, BattleStartConfig config, ISimulatedBattleSession? session)
    {
        Platform.Log.Sync($"[SyncFactory] Creating HybridSyncAdapter (HybridPredictReconcile mode)");
        return new HybridSyncAdapter();
    }
}
