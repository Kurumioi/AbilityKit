using System;
using System.Collections.Generic;

namespace AbilityKit.Triggering.Runtime.Scheduler
{
    /// <summary>
    /// 调度器注册中心
    /// 提供统一的调度器生命周期管理和查询能力
    ///
    /// 【框架层】默认实现，支持依赖注入
    /// 具体项目可实现 ISchedulerRegistry 接口提供自定义实现
    /// </summary>
    public sealed class SchedulerRegistry : ISchedulerRegistry
    {
        private readonly Dictionary<int, SchedulerEntry> _schedulers = new();
        private readonly Dictionary<int, List<int>> _byBusinessId = new();
        private readonly Dictionary<int, List<int>> _byTriggerId = new();
        private int _nextHandleId = 1;

        #region 属性

        public int TotalCount => _schedulers.Count;

        public int ActiveCount
        {
            get
            {
                int count = 0;
                foreach (var entry in _schedulers.Values)
                {
                    if (entry.Scheduler.IsActive)
                        count++;
                }
                return count;
            }
        }

        #endregion

        #region 创建

        public SchedulerHandle CreateScheduler(
            int schedulerId,
            int businessId,
            int triggerId,
            in SchedulerConfig config,
            Action<object> actionCallback,
            object context = null,
            Action<object, object> onComplete = null,
            Action<object, object> onInterrupt = null)
        {
            if (actionCallback == null)
                return SchedulerHandle.Invalid;

            // 如果已存在，移除旧的
            if (_schedulers.TryGetValue(schedulerId, out var existing))
            {
                existing.Scheduler.Stop();
                RemoveFromIndex(existing, businessId, triggerId);
            }

            var scheduler = new Scheduler(
                schedulerId,
                businessId,
                triggerId,
                config,
                context,
                actionCallback,
                onComplete,
                onInterrupt);

            // 自动启动
            scheduler.Start();

            var handle = new SchedulerHandle(schedulerId, _nextHandleId++);
            var entry = new SchedulerEntry(handle, scheduler);

            _schedulers[schedulerId] = entry;
            AddToIndex(entry, businessId, triggerId);

            return handle;
        }

        public SchedulerHandle CreatePeriodicScheduler(
            int schedulerId,
            int businessId,
            int triggerId,
            float intervalMs,
            int maxExecutions,
            Action<object> actionCallback,
            object context = null,
            Action<object, object> onComplete = null)
        {
            var config = SchedulerConfig.Periodic(intervalMs, maxExecutions);
            return CreateScheduler(schedulerId, businessId, triggerId, config, actionCallback, context, onComplete);
        }

        public SchedulerHandle CreateDelayedScheduler(
            int schedulerId,
            int businessId,
            int triggerId,
            float delayMs,
            Action<object> actionCallback,
            object context = null,
            Action<object, object> onComplete = null)
        {
            var config = SchedulerConfig.Delayed(delayMs);
            return CreateScheduler(schedulerId, businessId, triggerId, config, actionCallback, context, onComplete);
        }

        public SchedulerHandle CreateContinuousScheduler(
            int schedulerId,
            int businessId,
            int triggerId,
            float intervalMs,
            Action<object> actionCallback,
            object context = null,
            Action<object, object> onInterrupt = null)
        {
            var config = SchedulerConfig.Continuous(intervalMs);
            return CreateScheduler(schedulerId, businessId, triggerId, config, actionCallback, context, null, onInterrupt);
        }

        #endregion

        #region 查询

        public IScheduler GetScheduler(SchedulerHandle handle)
        {
            if (!handle.IsValid)
                return null;

            if (_schedulers.TryGetValue(handle.SchedulerId, out var entry))
            {
                if (entry.Handle.Version == handle.Version)
                    return entry.Scheduler;
            }

            return null;
        }

        public bool TryGetSchedulerData(SchedulerHandle handle, out SchedulerData data)
        {
            var scheduler = GetScheduler(handle);
            if (scheduler == null)
            {
                data = default;
                return false;
            }

            data = new SchedulerData
            {
                SchedulerId = scheduler.Handle.SchedulerId,
                Name = scheduler.Name,
                BusinessId = scheduler.BusinessId,
                TriggerId = scheduler.TriggerId,
                State = scheduler.State,
                Config = scheduler.Config,
                ExecutionCount = scheduler.ExecutionCount,
                ElapsedMs = scheduler.ElapsedMs,
                NextExecuteMs = scheduler.Config.DelayMs
            };
            return true;
        }

        public IEnumerable<IScheduler> FindByBusinessId(int businessId)
        {
            if (!_byBusinessId.TryGetValue(businessId, out var list))
                yield break;

            foreach (var schedulerId in list)
            {
                if (_schedulers.TryGetValue(schedulerId, out var entry))
                    yield return entry.Scheduler;
            }
        }

        public IEnumerable<IScheduler> FindByTriggerId(int triggerId)
        {
            if (!_byTriggerId.TryGetValue(triggerId, out var list))
                yield break;

            foreach (var schedulerId in list)
            {
                if (_schedulers.TryGetValue(schedulerId, out var entry))
                    yield return entry.Scheduler;
            }
        }

        public IEnumerable<IScheduler> GetActiveSchedulers()
        {
            foreach (var entry in _schedulers.Values)
            {
                if (entry.Scheduler.IsActive)
                    yield return entry.Scheduler;
            }
        }

        #endregion

        #region 控制

        public bool Pause(SchedulerHandle handle)
        {
            var scheduler = GetScheduler(handle);
            if (scheduler == null)
                return false;

            scheduler.Pause();
            return true;
        }

        public bool Resume(SchedulerHandle handle)
        {
            var scheduler = GetScheduler(handle);
            if (scheduler == null)
                return false;

            scheduler.Resume();
            return true;
        }

        public bool Interrupt(SchedulerHandle handle, string reason = null)
        {
            var scheduler = GetScheduler(handle);
            if (scheduler == null)
                return false;

            scheduler.Stop();
            return true;
        }

        public bool Remove(SchedulerHandle handle)
        {
            if (!handle.IsValid)
                return false;

            if (_schedulers.TryGetValue(handle.SchedulerId, out var entry))
            {
                if (entry.Handle.Version == handle.Version)
                {
                    RemoveFromIndex(entry, entry.Scheduler.BusinessId, entry.Scheduler.TriggerId);
                    _schedulers.Remove(handle.SchedulerId);
                    return true;
                }
            }

            return false;
        }

        public void PauseAll()
        {
            foreach (var entry in _schedulers.Values)
            {
                entry.Scheduler.Pause();
            }
        }

        public void ResumeAll()
        {
            foreach (var entry in _schedulers.Values)
            {
                entry.Scheduler.Resume();
            }
        }

        public int InterruptAll(string reason = null)
        {
            int count = 0;
            foreach (var entry in _schedulers.Values)
            {
                if (entry.Scheduler.IsActive)
                {
                    entry.Scheduler.Stop();
                    count++;
                }
            }
            return count;
        }

        public int InterruptByBusinessId(int businessId, string reason = null)
        {
            if (!_byBusinessId.TryGetValue(businessId, out var list))
                return 0;

            int count = 0;
            var toRemove = new List<int>();

            foreach (var schedulerId in list)
            {
                if (_schedulers.TryGetValue(schedulerId, out var entry))
                {
                    if (entry.Scheduler.IsActive)
                    {
                        entry.Scheduler.Stop();
                        count++;
                    }
                    toRemove.Add(schedulerId);
                }
            }

            foreach (var id in toRemove)
            {
                list.Remove(id);
            }

            return count;
        }

        public int InterruptByTriggerId(int triggerId, string reason = null)
        {
            if (!_byTriggerId.TryGetValue(triggerId, out var list))
                return 0;

            int count = 0;
            var toRemove = new List<int>();

            foreach (var schedulerId in list)
            {
                if (_schedulers.TryGetValue(schedulerId, out var entry))
                {
                    if (entry.Scheduler.IsActive)
                    {
                        entry.Scheduler.Stop();
                        count++;
                    }
                    toRemove.Add(schedulerId);
                }
            }

            foreach (var id in toRemove)
            {
                list.Remove(id);
            }

            return count;
        }

        #endregion

        #region 更新

        public void Update(float deltaTimeMs)
        {
            var toRemove = new List<int>();

            foreach (var kvp in _schedulers)
            {
                var scheduler = kvp.Value.Scheduler;
                bool shouldContinue = scheduler.Update(deltaTimeMs, null);

                // 只有当调度器真正结束时才移除（Completed, Cancelled）
                // Paused 状态的调度器应该保留，等待 Resume
                if (!shouldContinue)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var schedulerId in toRemove)
            {
                if (_schedulers.TryGetValue(schedulerId, out var entry))
                {
                    RemoveFromIndex(entry, entry.Scheduler.BusinessId, entry.Scheduler.TriggerId);
                    _schedulers.Remove(schedulerId);
                }
            }
        }

        #endregion

        #region 清理

        public void Clear()
        {
            foreach (var entry in _schedulers.Values)
            {
                entry.Scheduler.Stop();
            }

            _schedulers.Clear();
            _byBusinessId.Clear();
            _byTriggerId.Clear();
        }

        #endregion

        #region 内部

        private void AddToIndex(SchedulerEntry entry, int businessId, int triggerId)
        {
            if (businessId != 0)
            {
                if (!_byBusinessId.TryGetValue(businessId, out var list))
                {
                    list = new List<int>();
                    _byBusinessId[businessId] = list;
                }
                list.Add(entry.Handle.SchedulerId);
            }

            if (triggerId != 0)
            {
                if (!_byTriggerId.TryGetValue(triggerId, out var list))
                {
                    list = new List<int>();
                    _byTriggerId[triggerId] = list;
                }
                list.Add(entry.Handle.SchedulerId);
            }
        }

        private void RemoveFromIndex(SchedulerEntry entry, int businessId, int triggerId)
        {
            if (businessId != 0 && _byBusinessId.TryGetValue(businessId, out var bizList))
            {
                bizList.Remove(entry.Handle.SchedulerId);
                if (bizList.Count == 0)
                    _byBusinessId.Remove(businessId);
            }

            if (triggerId != 0 && _byTriggerId.TryGetValue(triggerId, out var trigList))
            {
                trigList.Remove(entry.Handle.SchedulerId);
                if (trigList.Count == 0)
                    _byTriggerId.Remove(triggerId);
            }
        }

        private readonly struct SchedulerEntry
        {
            public readonly SchedulerHandle Handle;
            public readonly Scheduler Scheduler;

            public SchedulerEntry(SchedulerHandle handle, Scheduler scheduler)
            {
                Handle = handle;
                Scheduler = scheduler;
            }
        }

        #endregion
    }
}
