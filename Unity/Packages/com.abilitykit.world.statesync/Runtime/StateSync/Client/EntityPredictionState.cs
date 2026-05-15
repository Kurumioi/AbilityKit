using System;
using System.Collections.Generic;

namespace AbilityKit.Ability.StateSync.Client
{
    /// <summary>
    /// 实体预测状态实现
    /// 封装单个实体的预测逻辑
    /// </summary>
    public sealed class EntityPredictionState : IEntityPredictionState
    {
        private readonly int _entityId;
        private readonly bool _isLocalPlayer;
        private readonly List<IClientPredictionHandler> _handlers = new List<IClientPredictionHandler>();
        private readonly Dictionary<int, AbilityKit.Ability.StateSync.Prediction.StateSlots> _snapshots = new Dictionary<int, AbilityKit.Ability.StateSync.Prediction.StateSlots>();
        private readonly List<StateChangeEvent> _pendingChanges = new List<StateChangeEvent>();
        private readonly Dictionary<string, object> _previousValues = new Dictionary<string, object>();

        private AbilityKit.Ability.StateSync.Prediction.StateSlots _currentSlots;
        private int _currentFrame;
        private int _confirmedFrame;
        private bool _isPredicted;

        public int EntityId => _entityId;
        public bool IsLocalPlayer => _isLocalPlayer;
        public AbilityKit.Ability.StateSync.Prediction.StateSlots CurrentSlots => _currentSlots;
        public bool IsPredicted => _isPredicted;
        public int CurrentFrame => _currentFrame;

        public event Action<string, object, object> OnSlotChanged;
        public event Action<int, int> OnRollback;

        public EntityPredictionState(int entityId, bool isLocalPlayer)
        {
            _entityId = entityId;
            _isLocalPlayer = isLocalPlayer;
            _currentSlots = new AbilityKit.Ability.StateSync.Prediction.StateSlots();
            _confirmedFrame = -1;
            _currentFrame = 0;
        }

        public void RegisterHandler(IClientPredictionHandler handler)
        {
            if (handler != null)
            {
                _handlers.Add(handler);
            }
        }

        public void Predict(IInputCommand input, int frame)
        {
            _currentFrame = frame;

            foreach (var handler in _handlers)
            {
                if (handler.Strategy == PredictionStrategy.None)
                    continue;

                // 记录变化前的值
                var keys = new List<string>(_currentSlots.Keys);
                foreach (var key in keys)
                {
                    if (!_previousValues.ContainsKey(key))
                    {
                        _previousValues[key] = GetSlotValue(key);
                    }
                }

                // 执行预测
                handler.PredictLocal(input, _currentSlots, frame);
            }

            // 收集状态变化
            CollectStateChanges(frame, isPredicted: true);

            _isPredicted = true;
        }

        public bool ApplyServerState(int serverFrame, ServerEntitySnapshot snapshot)
        {
            // 首先检查是否需要回滚
            if (serverFrame < _currentFrame)
            {
                // 需要回滚
                RollbackTo(serverFrame);
                OnRollback?.Invoke(_currentFrame, serverFrame);
            }

            // 应用服务器状态
            // 注意：这里需要业务层提供反序列化逻辑
            // 当前实现假设 ServerEntitySnapshot.Data 包含序列化后的 StateSlots

            _confirmedFrame = serverFrame;
            _isPredicted = false;

            return true;
        }

        public void RollbackTo(int frame)
        {
            if (frame >= _currentFrame)
                return;

            // 获取目标帧的快照
            if (_snapshots.TryGetValue(frame, out var snapshot))
            {
                // 记录回滚前的状态
                var oldFrame = _currentFrame;

                // 恢复快照
                _currentSlots.OverwriteFrom(snapshot);

                // 收集回滚产生的变化
                CollectStateChanges(frame, isRollback: true);

                _currentFrame = frame;
                _isPredicted = false;

                OnRollback?.Invoke(oldFrame, frame);
            }
        }

        public void CaptureSnapshot(int frame)
        {
            // 清理旧快照
            PruneOldSnapshots(frame);

            // 保存当前状态的克隆
            _snapshots[frame] = _currentSlots.Clone();
        }

        public AbilityKit.Ability.StateSync.Prediction.StateSlots GetSnapshot(int frame)
        {
            if (_snapshots.TryGetValue(frame, out var snapshot))
            {
                return snapshot;
            }
            return null;
        }

        public void AdvanceFrame()
        {
            _currentFrame++;
            _confirmedFrame = _currentFrame;
        }

        public IReadOnlyList<StateChangeEvent> GetPendingStateChanges()
        {
            return _pendingChanges;
        }

        public void ClearPendingStateChanges()
        {
            _pendingChanges.Clear();
        }

        private void CollectStateChanges(int frame, bool isPredicted = false, bool isRollback = false)
        {
            foreach (var kvp in _currentSlots.Keys)
            {
                var slotName = kvp;
                var newValue = GetSlotValue(slotName);

                if (_previousValues.TryGetValue(slotName, out var oldValue))
                {
                    if (!Equals(oldValue, newValue))
                    {
                        var evt = new StateChangeEvent
                        {
                            EntityId = _entityId,
                            Frame = frame,
                            SlotName = slotName,
                            OldValue = oldValue,
                            NewValue = newValue,
                            IsPredicted = isPredicted
                        };
                        _pendingChanges.Add(evt);
                        OnSlotChanged?.Invoke(slotName, oldValue, newValue);
                    }
                }
                else
                {
                    // 新增槽位
                    var evt = new StateChangeEvent
                    {
                        EntityId = _entityId,
                        Frame = frame,
                        SlotName = slotName,
                        OldValue = null,
                        NewValue = newValue,
                        IsPredicted = isPredicted
                    };
                    _pendingChanges.Add(evt);
                    OnSlotChanged?.Invoke(slotName, null, newValue);
                }

                _previousValues[slotName] = newValue;
            }
        }

        private object GetSlotValue(string slotName)
        {
            if (_currentSlots.Has(slotName))
            {
                // 尝试获取常见类型
                if (_currentSlots.Has(slotName + "_float"))
                {
                    return _currentSlots.GetFloat(slotName + "_float");
                }
                if (_currentSlots.Has(slotName + "_int"))
                {
                    return _currentSlots.GetInt(slotName + "_int");
                }
                return slotName; // 占位，实际应从 SlotValue 获取
            }
            return null;
        }

        private void PruneOldSnapshots(int currentFrame)
        {
            // 保留最近 30 帧的快照
            var framesToRemove = new List<int>();
            foreach (var frame in _snapshots.Keys)
            {
                if (frame < currentFrame - 30)
                {
                    framesToRemove.Add(frame);
                }
            }

            foreach (var frame in framesToRemove)
            {
                _snapshots.Remove(frame);
            }
        }

        private static bool Equals(object a, object b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            return a.Equals(b);
        }
    }
}
