using System;
using AbilityKit.Demo.Moba.Console.Core.Battle.Context;
using AbilityKit.Demo.Moba.Console.Battle;
using AbilityKit.Demo.Moba.Console.Simulation;
using AbilityKit.Demo.Moba.Share;

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
    public static IBattleSyncAdapter Create(ConsoleBattleContext context, BattleStartConfig config)
    {
        return config.SyncMode switch
        {
            SyncMode.Lockstep => CreateFrameSyncAdapter(context, config),
            SyncMode.SnapshotAuthority => CreateStateSyncAdapter(context, config),
            SyncMode.Hybrid => CreateHybridSyncAdapter(context, config),
            _ => throw new ArgumentException($"Unknown SyncMode: {config.SyncMode}")
        };
    }

    private static IBattleSyncAdapter CreateFrameSyncAdapter(ConsoleBattleContext context, BattleStartConfig config)
    {
        Platform.Log.Sync($"[SyncFactory] Creating FrameSyncAdapter (Lockstep mode)");
        var adapter = new FrameSyncAdapter();
        adapter.Initialize(context, config);
        return adapter;
    }

    private static IBattleSyncAdapter CreateStateSyncAdapter(ConsoleBattleContext context, BattleStartConfig config)
    {
        Platform.Log.Sync($"[SyncFactory] Creating StateSyncAdapter (SnapshotAuthority mode)");
        var adapter = new StateSyncAdapter();
        adapter.Initialize(context, config);
        return adapter;
    }

    private static IBattleSyncAdapter CreateHybridSyncAdapter(ConsoleBattleContext context, BattleStartConfig config)
    {
        Platform.Log.Sync($"[SyncFactory] Creating HybridSyncAdapter (HybridPredictReconcile mode)");
        var adapter = new HybridSyncAdapter();
        adapter.Initialize(context, config);
        return adapter;
    }
}
