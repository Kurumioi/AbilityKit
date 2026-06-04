using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Share;
using AbilityKit.Demo.Moba.Console.Battle.Context;
using AbilityKit.Protocol.Moba;

namespace AbilityKit.Demo.Moba.Console.Battle.Snapshot
{
    /// <summary>
    /// 战斗快照管道
    /// 对齐 Unity BattleSnapshotPipeline
    /// 支持有序阶段的快照处理
    ///
    /// 注意：Console 使用已有的 ConsoleSnapshotViewAdapter 进行数据订阅
    /// 此管道用于管理额外的处理阶段
    /// </summary>
    public sealed class ConsoleBattleSnapshotPipeline : IDisposable
    {
        private readonly ConsoleBattleContext _context;
        private readonly Dictionary<int, List<SnapshotStage>> _stages = new();
        private bool _disposed;

        /// <summary>
        /// 快照阶段
        /// </summary>
        private struct SnapshotStage
        {
            public readonly int Order;
            public readonly Action<ConsoleBattleContext, int, object> Handler;
            public readonly Type DataType;

            public SnapshotStage(int order, Type dataType, Action<ConsoleBattleContext, int, object> handler)
            {
                Order = order;
                DataType = dataType;
                Handler = handler;
            }
        }

        public ConsoleBattleSnapshotPipeline(ConsoleBattleContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// 添加处理阶段（按 order 排序执行）
        /// </summary>
        public void AddStage<T>(int opCode, int order, Action<ConsoleBattleContext, int, T> handler)
        {
            if (!_stages.TryGetValue(opCode, out var stages))
            {
                stages = new List<SnapshotStage>();
                _stages[opCode] = stages;
            }

            stages.Add(new SnapshotStage(order, typeof(T), (ctx, frame, data) =>
            {
                if (data is T typedData)
                {
                    handler(ctx, frame, typedData);
                }
            }));

            stages.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        /// <summary>
        /// 处理 EnterGame 快照
        /// </summary>
        public void ProcessEnterGame(int frame, EnterGameData data)
        {
            InvokeStages(MobaOpCodes.Snapshot.EnterGame, frame, data);
        }

        /// <summary>
        /// 处理 ActorSpawn 快照
        /// </summary>
        public void ProcessActorSpawn(int frame, List<ActorSpawnData> data)
        {
            InvokeStages(MobaOpCodes.Snapshot.ActorSpawn, frame, data);
        }

        /// <summary>
        /// 处理 ActorTransform 快照
        /// </summary>
        public void ProcessActorTransform(int frame, List<ActorTransformData> data)
        {
            InvokeStages(MobaOpCodes.Snapshot.ActorTransform, frame, data);
        }

        /// <summary>
        /// 处理 ProjectileEvent 快照
        /// </summary>
        public void ProcessProjectileEvent(int frame, List<ProjectileEventData> data)
        {
            InvokeStages(MobaOpCodes.Snapshot.ProjectileEvent, frame, data);
        }

        /// <summary>
        /// 处理 AreaEvent 快照
        /// </summary>
        public void ProcessAreaEvent(int frame, List<AreaEventData> data)
        {
            InvokeStages(MobaOpCodes.Snapshot.AreaEvent, frame, data);
        }

        /// <summary>
        /// 处理 DamageEvent 快照
        /// </summary>
        public void ProcessDamageEvent(int frame, List<DamageEventData> data)
        {
            InvokeStages(MobaOpCodes.Snapshot.DamageEvent, frame, data);
        }

        /// <summary>
        /// 处理 StateHash 快照
        /// </summary>
        public void ProcessStateHash(int frame, StateHashData data)
        {
            InvokeStages(MobaOpCodes.Snapshot.StateHash, frame, data);
        }

        private void InvokeStages(int opCode, int frame, object data)
        {
            if (!_stages.TryGetValue(opCode, out var stages))
            {
                return;
            }

            foreach (var stage in stages)
            {
                try
                {
                    stage.Handler(_context, frame, data);
                }
                catch (Exception ex)
                {
                    Platform.Log.Error($"[SnapshotPipeline] Stage order={stage.Order} failed: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _stages.Clear();
        }
    }

    /// <summary>
    /// 标准阶段顺序定义
    /// 对齐 Unity 中的阶段顺序
    /// </summary>
    public static class SnapshotStageOrders
    {
        public const int First = 0;
        public const int Validation = 50;
        public const int Simulation = 100;
        public const int Input = 150;
        public const int PreView = 200;
        public const int View = 300;
        public const int Debug = 1000;
        public const int Last = 9999;
    }
}
