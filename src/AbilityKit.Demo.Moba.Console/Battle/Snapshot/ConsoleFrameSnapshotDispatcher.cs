using System;
using AbilityKit.Demo.Moba.Share;
using AbilityKit.Demo.Moba.Console.Events;
using AbilityKit.Demo.Moba.Console.Platform;
using ShareMobaOpCode = AbilityKit.Demo.Moba.Share.MobaOpCode;

namespace AbilityKit.Demo.Moba.Console.Battle.Snapshot
{
    /// <summary>
    /// Console 版本的帧快照分发器
    /// 封装 Share 层的 FrameSnapshotDispatcher，实现事件转换
    /// 同时实现 IFrameSnapshotDeserializer 接口
    /// </summary>
    public sealed class ConsoleFrameSnapshotDispatcher : IDisposable, IFrameSnapshotDeserializer
    {
        private readonly FrameSnapshotDispatcher _dispatcher;
        private bool _disposed;
        private int _currentFrame;

        public FrameSnapshotDispatcher Dispatcher => _dispatcher;
        public int CurrentFrame => _currentFrame;

        public ConsoleFrameSnapshotDispatcher()
        {
            _dispatcher = new FrameSnapshotDispatcher();
            _currentFrame = 0;

            SubscribeToEventBus();
        }

        public void SetFrame(int frame)
        {
            _currentFrame = frame;
        }

        public void AdvanceFrame()
        {
            _currentFrame++;
        }

        private void SubscribeToEventBus()
        {
            BattleEventBus.Subscribe<DamageEvent>(OnDamageEvent);
            BattleEventBus.Subscribe<EntityCreatedEvent>(OnEntityCreatedEvent);
            BattleEventBus.Subscribe<EntityDestroyedEvent>(OnEntityDestroyedEvent);
            BattleEventBus.Subscribe<MoveInputProcessedEvent>(OnMoveEvent);
            BattleEventBus.Subscribe<FrameSyncEvent>(OnFrameSyncEvent);
        }

        private void UnsubscribeFromEventBus()
        {
            BattleEventBus.Unsubscribe<DamageEvent>(OnDamageEvent);
            BattleEventBus.Unsubscribe<EntityCreatedEvent>(OnEntityCreatedEvent);
            BattleEventBus.Unsubscribe<EntityDestroyedEvent>(OnEntityDestroyedEvent);
            BattleEventBus.Unsubscribe<MoveInputProcessedEvent>(OnMoveEvent);
            BattleEventBus.Unsubscribe<FrameSyncEvent>(OnFrameSyncEvent);
        }

        private void OnDamageEvent(DamageEvent evt)
        {
            var damageData = new DamageEventData(
                evt.SourceId,
                evt.TargetId,
                evt.SkillId,
                0,
                (int)evt.Damage,
                (int)evt.CurrentHp,
                evt.IsDead);

            _dispatcher.DispatchDamageEvent(_currentFrame, new DamageEventData[] { damageData });
            Log.Trace($"[SnapshotDispatcher] Dispatched DamageEvent: Target#{evt.TargetId} -{evt.Damage:F0}");
        }

        private void OnEntityCreatedEvent(EntityCreatedEvent evt)
        {
            var transformData = new ActorTransformData(
                evt.ActorId,
                evt.X,
                0,
                evt.Z,
                0,
                1.0f);

            _dispatcher.DispatchActorTransform(_currentFrame, new ActorTransformData[] { transformData });
            Log.Trace($"[SnapshotDispatcher] Dispatched ActorSpawn: #{evt.ActorId} ({evt.Name})");
        }

        private void OnEntityDestroyedEvent(EntityDestroyedEvent evt)
        {
            Log.Trace($"[SnapshotDispatcher] EntityDestroyed: #{evt.ActorId}");
        }

        private void OnMoveEvent(MoveInputProcessedEvent evt)
        {
            Log.Trace($"[SnapshotDispatcher] MoveEvent: Actor#{evt.ActorId} -> ({evt.Dx:F2}, {evt.Dz:F2})");
        }

        private void OnFrameSyncEvent(FrameSyncEvent evt)
        {
            _currentFrame = evt.Frame;
        }

        public void DispatchEnterGame(EnterGameData data)
        {
            _dispatcher.DispatchEnterGame(_currentFrame, in data);
            Log.Trace($"[SnapshotDispatcher] Dispatched EnterGame: LocalPlayer#{data.LocalPlayerId}");
        }

        public void DispatchActorTransform(ActorTransformData[] transforms)
        {
            if (transforms == null || transforms.Length == 0) return;
            _dispatcher.DispatchActorTransform(_currentFrame, transforms);
        }

        public void DispatchProjectile(ProjectileEventData[] events)
        {
            if (events == null || events.Length == 0) return;
            _dispatcher.DispatchProjectileEvent(_currentFrame, events);
        }

        public void DispatchAreaEvent(AreaEventData[] events)
        {
            if (events == null || events.Length == 0) return;
            _dispatcher.DispatchAreaEvent(_currentFrame, events);
        }

        public void DispatchDamage(DamageEventData[] events)
        {
            if (events == null || events.Length == 0) return;
            _dispatcher.DispatchDamageEvent(_currentFrame, events);
        }

        public void DispatchStateHash(uint hash)
        {
            var data = new StateHashData(_currentFrame, hash);
            _dispatcher.DispatchStateHash(_currentFrame, in data);
        }

        /// <summary>
        /// 获取当前帧的序列化快照数据
        /// 用于回放录制
        /// </summary>
        public byte[] SerializeCurrentSnapshot()
        {
            try
            {
                // 构建简单的快照 JSON 用于录制
                var snapshot = new FrameSnapshotInfo
                {
                    Frame = _currentFrame,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                var json = System.Text.Json.JsonSerializer.Serialize(snapshot);
                return System.Text.Encoding.UTF8.GetBytes(json);
            }
            catch (Exception ex)
            {
                Log.Error($"[SnapshotDispatcher] Failed to serialize snapshot: {ex.Message}");
                return Array.Empty<byte>();
            }
        }

        /// <summary>
        /// 获取快照统计信息
        /// </summary>
        public SnapshotStats GetStats()
        {
            return new SnapshotStats
            {
                CurrentFrame = _currentFrame,
                SubscriberCount = _dispatcher.GetSubscriptionCount((int)ShareMobaOpCode.EnterGameSnapshot) +
                                 _dispatcher.GetSubscriptionCount((int)ShareMobaOpCode.DamageEventSnapshot)
            };
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            UnsubscribeFromEventBus();
            _dispatcher.Dispose();

            Log.Trace("[ConsoleFrameSnapshotDispatcher] Disposed");
        }

        #region IFrameSnapshotDeserializer 实现

        /// <inheritdoc />
        public bool TryDeserializeEnterGame(byte[] rawData, out EnterGameData result)
        {
            result = default;
            if (rawData == null || rawData.Length == 0) return false;

            try
            {
                var json = System.Text.Encoding.UTF8.GetString(rawData);
                // 简化实现：从 JSON 解析 EnterGameData
                // Console 层使用内部格式，此处返回失败
                Log.Warn("[SnapshotDispatcher] TryDeserializeEnterGame not implemented for Console format");
                return false;
            }
            catch (Exception ex)
            {
                Log.Error($"[SnapshotDispatcher] Failed to deserialize EnterGame: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc />
        public bool TryDeserializeActorTransform(byte[] rawData, out ActorTransformData[] result)
        {
            result = Array.Empty<ActorTransformData>();
            if (rawData == null || rawData.Length == 0) return false;

            try
            {
                var json = System.Text.Encoding.UTF8.GetString(rawData);
                Log.Warn("[SnapshotDispatcher] TryDeserializeActorTransform not implemented for Console format");
                return false;
            }
            catch (Exception ex)
            {
                Log.Error($"[SnapshotDispatcher] Failed to deserialize ActorTransform: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc />
        public bool TryDeserializeProjectileEvent(byte[] rawData, out ProjectileEventData[] result)
        {
            result = Array.Empty<ProjectileEventData>();
            if (rawData == null || rawData.Length == 0) return false;

            try
            {
                var json = System.Text.Encoding.UTF8.GetString(rawData);
                Log.Warn("[SnapshotDispatcher] TryDeserializeProjectileEvent not implemented for Console format");
                return false;
            }
            catch (Exception ex)
            {
                Log.Error($"[SnapshotDispatcher] Failed to deserialize ProjectileEvent: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc />
        public bool TryDeserializeAreaEvent(byte[] rawData, out AreaEventData[] result)
        {
            result = Array.Empty<AreaEventData>();
            if (rawData == null || rawData.Length == 0) return false;

            try
            {
                var json = System.Text.Encoding.UTF8.GetString(rawData);
                Log.Warn("[SnapshotDispatcher] TryDeserializeAreaEvent not implemented for Console format");
                return false;
            }
            catch (Exception ex)
            {
                Log.Error($"[SnapshotDispatcher] Failed to deserialize AreaEvent: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc />
        public bool TryDeserializeDamageEvent(byte[] rawData, out DamageEventData[] result)
        {
            result = Array.Empty<DamageEventData>();
            if (rawData == null || rawData.Length == 0) return false;

            try
            {
                var json = System.Text.Encoding.UTF8.GetString(rawData);
                Log.Warn("[SnapshotDispatcher] TryDeserializeDamageEvent not implemented for Console format");
                return false;
            }
            catch (Exception ex)
            {
                Log.Error($"[SnapshotDispatcher] Failed to deserialize DamageEvent: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc />
        public bool TryDeserializeStateHash(byte[] rawData, out StateHashData result)
        {
            result = default;
            if (rawData == null || rawData.Length == 0) return false;

            try
            {
                var json = System.Text.Encoding.UTF8.GetString(rawData);
                Log.Warn("[SnapshotDispatcher] TryDeserializeStateHash not implemented for Console format");
                return false;
            }
            catch (Exception ex)
            {
                Log.Error($"[SnapshotDispatcher] Failed to deserialize StateHash: {ex.Message}");
                return false;
            }
        }

        #endregion
    }

    /// <summary>
    /// 帧快照信息（用于序列化）
    /// </summary>
    public sealed class FrameSnapshotInfo
    {
        public int Frame { get; set; }
        public long Timestamp { get; set; }
    }

    /// <summary>
    /// 快照统计信息
    /// </summary>
    public struct SnapshotStats
    {
        public int CurrentFrame;
        public int SubscriberCount;
    }
}
