using System;
using System.Collections.Generic;
using AbilityKit.Ability.StateSync.Buffer;
using AbilityKit.Ability.StateSync.Diff;
using AbilityKit.Ability.StateSync.Snapshot;

namespace AbilityKit.Ability.StateSync
{
    public sealed class StateManager : IStateManager
    {
        public Action<string> Log;

        private readonly Dictionary<long, IRollbackable> _rollbackables = new Dictionary<long, IRollbackable>();
        private readonly SnapshotBuffer _snapshotBuffer;
        private readonly StateDiffProvider _diffProvider;
        private readonly object _lock = new object();

        /// <summary>
        /// 实体回滚数据缓冲区：Frame -> (EntityId -> RollbackState bytes)
        /// 与 WorldStateSnapshot 分离，用于存储实体的完整回滚状态
        /// </summary>
        private readonly Dictionary<int, Dictionary<long, byte[]>> _entityRollbackBuffers = new Dictionary<int, Dictionary<long, byte[]>>();

        public StateManager(SnapshotBuffer snapshotBuffer, StateDiffProvider diffProvider = null)
        {
            _snapshotBuffer = snapshotBuffer ?? throw new ArgumentNullException(nameof(snapshotBuffer));
            _diffProvider = diffProvider ?? new StateDiffProvider();
        }

        public void RegisterRollbackable(IRollbackable entity)
        {
            lock (_lock)
            {
                if (_rollbackables.ContainsKey(entity.EntityId))
                {
                    Log?.Invoke($"[StateManager] Entity {entity.EntityId} already registered");
                    return;
                }

                _rollbackables[entity.EntityId] = entity;
                Log?.Invoke($"[StateManager] Registered entity {entity.EntityId} with key {entity.SnapshotKey}");
            }
        }

        public void UnregisterRollbackable(long entityId)
        {
            lock (_lock)
            {
                if (_rollbackables.Remove(entityId))
                {
                    Log?.Invoke($"[StateManager] Unregistered entity {entityId}");
                }
            }
        }

        public void CaptureState(int frame)
        {
            lock (_lock)
            {
                var snapshot = new WorldStateSnapshot
                {
                    Version = WorldStateSnapshot.CurrentVersion,
                    Frame = frame,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                // 捕获所有实体的回滚状态
                var entityRollbackData = new Dictionary<long, byte[]>();
                foreach (var kvp in _rollbackables)
                {
                    var entityId = kvp.Key;
                    var entity = kvp.Value;

                    var rollbackState = entity.CreateRollbackState();
                    if (rollbackState != null)
                    {
                        entityRollbackData[entityId] = rollbackState.Serialize();
                    }
                }

                // 存储实体回滚数据
                _entityRollbackBuffers[frame] = entityRollbackData;

                // 存储快照
                _snapshotBuffer.Store(frame, snapshot);
                Log?.Invoke($"[StateManager] Captured state for frame={frame} with {entityRollbackData.Count} entities");
            }
        }

        public bool TryRestore(int frame)
        {
            lock (_lock)
            {
                if (!_snapshotBuffer.TryGet(frame, out var snapshot))
                {
                    Log?.Invoke($"[StateManager] No snapshot found for frame={frame}");
                    return false;
                }

                RestoreSnapshot(snapshot, frame);
                Log?.Invoke($"[StateManager] Restored state for frame={frame}");
                return true;
            }
        }

        public IStateDiff ComputeDiff(int fromFrame, int toFrame)
        {
            lock (_lock)
            {
                if (!_snapshotBuffer.TryGet(fromFrame, out var fromSnapshot) ||
                    !_snapshotBuffer.TryGet(toFrame, out var toSnapshot))
                {
                    Log?.Invoke($"[StateManager] Cannot compute diff: missing snapshot(s)");
                    return null;
                }

                return _diffProvider.ComputeDiff(toSnapshot, fromSnapshot);
            }
        }

        public byte[] GetFullState(int frame)
        {
            lock (_lock)
            {
                if (!_snapshotBuffer.TryGet(frame, out var snapshot))
                {
                    return null;
                }

                return snapshot.ToBytes();
            }
        }

        public IReadOnlyList<int> GetCapturedFrames()
        {
            lock (_lock)
            {
                return _snapshotBuffer.GetCapturedFrames();
            }
        }

        public void ClearHistory()
        {
            lock (_lock)
            {
                _snapshotBuffer.Clear();
                _entityRollbackBuffers.Clear();
                Log?.Invoke("[StateManager] Cleared snapshot history");
            }
        }

        /// <summary>
        /// 从快照恢复所有实体的状态
        /// 要求业务层实现 IRollbackable 接口
        /// </summary>
        private void RestoreSnapshot(WorldStateSnapshot snapshot, int frame)
        {
            if (!_entityRollbackBuffers.TryGetValue(frame, out var entityRollbackData))
            {
                Log?.Invoke($"[StateManager] No entity rollback data found for frame={frame}");
                return;
            }

            foreach (var kvp in entityRollbackData)
            {
                var entityId = kvp.Key;
                var rollbackData = kvp.Value;

                if (_rollbackables.TryGetValue(entityId, out var entity))
                {
                    var rollbackState = entity.CreateRollbackState();
                    if (rollbackState != null)
                    {
                        rollbackState.Deserialize(rollbackData);
                        entity.RestoreFromRollbackState(rollbackState);
                        Log?.Invoke($"[StateManager] Restored entity {entityId} for frame={frame}");
                    }
                }
                else
                {
                    Log?.Invoke($"[StateManager] Entity {entityId} not found for rollback");
                }
            }
        }
    }
}
