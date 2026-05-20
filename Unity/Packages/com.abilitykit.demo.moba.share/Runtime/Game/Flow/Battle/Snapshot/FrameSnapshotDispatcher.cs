using System;
using System.Collections.Generic;

namespace AbilityKit.Demo.Moba.Share
{
    /// <summary>
    /// 帧快照分发器
    /// 负责将帧快照数据分发给订阅者
    /// 参考 view.runtime 的 FrameSnapshotDispatcher 实现
    /// </summary>
    public sealed class FrameSnapshotDispatcher : IFrameSnapshotDispatcher
    {
        private readonly Dictionary<int, List<SnapshotSubscription>> _subscriptions = new Dictionary<int, List<SnapshotSubscription>>();
        private readonly object _lock = new object();
        private bool _isDisposed;

        /// <summary>
        /// 订阅快照事件
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="opCode">操作码</param>
        /// <param name="handler">处理函数</param>
        /// <returns>订阅句柄，用于取消订阅</returns>
        public IDisposable Subscribe<T>(int opCode, Action<int, T> handler)
        {
            if (_isDisposed) return null;

            var subscription = new SnapshotSubscription(opCode, typeof(T), (frame, data) =>
            {
                if (data is T typedData)
                {
                    handler(frame, typedData);
                }
            });

            lock (_lock)
            {
                if (!_subscriptions.TryGetValue(opCode, out var list))
                {
                    list = new List<SnapshotSubscription>();
                    _subscriptions[opCode] = list;
                }
                list.Add(subscription);
            }

            return subscription;
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        /// <param name="subscription">订阅句柄</param>
        public void Unsubscribe(IDisposable subscription)
        {
            if (subscription == null) return;

            lock (_lock)
            {
                foreach (var kvp in _subscriptions)
                {
                    if (kvp.Value.Remove(subscription as SnapshotSubscription))
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 分发进入游戏事件
        /// </summary>
        public void DispatchEnterGame(int frameIndex, in EnterGameData data)
        {
            Dispatch(frameIndex, (int)MobaOpCode.EnterGameSnapshot, data);
        }

        /// <summary>
        /// 分发角色变换事件
        /// </summary>
        public void DispatchActorTransform(int frameIndex, in ActorTransformData[] data)
        {
            Dispatch(frameIndex, (int)MobaOpCode.ActorTransformSnapshot, data);
        }

        /// <summary>
        /// 分发弹道事件
        /// </summary>
        public void DispatchProjectileEvent(int frameIndex, in ProjectileEventData[] data)
        {
            Dispatch(frameIndex, (int)MobaOpCode.ProjectileEventSnapshot, data);
        }

        /// <summary>
        /// 分发区域事件
        /// </summary>
        public void DispatchAreaEvent(int frameIndex, in AreaEventData[] data)
        {
            Dispatch(frameIndex, (int)MobaOpCode.AreaEventSnapshot, data);
        }

        /// <summary>
        /// 分发伤害事件
        /// </summary>
        public void DispatchDamageEvent(int frameIndex, in DamageEventData[] data)
        {
            Dispatch(frameIndex, (int)MobaOpCode.DamageEventSnapshot, data);
        }

        /// <summary>
        /// 分发状态哈希
        /// </summary>
        public void DispatchStateHash(int frameIndex, in StateHashData data)
        {
            Dispatch(frameIndex, (int)MobaOpCode.StateHashSnapshot, data);
        }

        /// <summary>
        /// 分发角色生成事件
        /// </summary>
        public void DispatchActorSpawn(int frameIndex, in ActorSpawnData[] data)
        {
            Dispatch(frameIndex, (int)MobaOpCode.ActorSpawnSnapshot, data);
        }

        /// <summary>
        /// 分发快照数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="frameIndex">帧索引</param>
        /// <param name="opCode">操作码</param>
        /// <param name="data">数据</param>
        public void Dispatch<T>(int frameIndex, int opCode, T data)
        {
            if (_isDisposed) return;

            List<SnapshotSubscription> subscriptions;
            lock (_lock)
            {
                if (!_subscriptions.TryGetValue(opCode, out var list))
                {
                    return;
                }

                // 复制列表以避免在分发过程中修改
                subscriptions = new List<SnapshotSubscription>(list);
            }

            // 分发给所有订阅者
            for (int i = 0; i < subscriptions.Count; i++)
            {
                try
                {
                    subscriptions[i].Invoke(frameIndex, data);
                }
                catch (Exception ex)
                {
                    LogException(ex, subscriptions[i].OpCode);
                }
            }
        }

        /// <summary>
        /// 清空所有订阅
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _subscriptions.Clear();
            }
        }

        /// <summary>
        /// 获取订阅数量
        /// </summary>
        public int GetSubscriptionCount(int opCode)
        {
            lock (_lock)
            {
                if (_subscriptions.TryGetValue(opCode, out var list))
                {
                    return list.Count;
                }
                return 0;
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            Clear();
        }

        private static void LogException(Exception ex, int opCode)
        {
            System.Diagnostics.Debug.WriteLine($"[FrameSnapshotDispatcher] Exception in handler for opCode {opCode}: {ex}");
        }
    }

    /// <summary>
    /// 快照订阅
    /// </summary>
    internal sealed class SnapshotSubscription : IDisposable
    {
        public int OpCode { get; }
        public Type DataType { get; }
        private readonly Action<int, object> _handler;

        public SnapshotSubscription(int opCode, Type dataType, Action<int, object> handler)
        {
            OpCode = opCode;
            DataType = dataType;
            _handler = handler;
        }

        public void Invoke<T>(int frameIndex, T data)
        {
            _handler(frameIndex, data);
        }

        public void Dispose()
        {
            // 订阅句柄的释放由订阅者负责
        }
    }

    /// <summary>
    /// MOBA 操作码
    /// 注意：这些值必须与 Runtime 层的 AbilityKit.Demo.Moba.Services.MobaOpCode 保持一致
    /// </summary>
    public static class MobaOpCode
    {
        public const int EnterGameSnapshot = 4002;
        public const int ActorTransformSnapshot = 4003;
        public const int StateHashSnapshot = 4004;
        public const int ActorSpawnSnapshot = 4005;
        public const int ProjectileEventSnapshot = 4006;
        public const int DamageEventSnapshot = 4007;
        public const int ActorDespawnSnapshot = 4008;
        public const int AreaEventSnapshot = 4009;
        public const int SkillCastSnapshot = 2001;
        public const int SkillHitSnapshot = 2002;
        public const int UnitDeathSnapshot = 3001;
        public const int UnitReviveSnapshot = 3002;
    }
}
