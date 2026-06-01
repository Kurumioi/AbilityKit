using System;
using System.Collections.Generic;

namespace AbilityKit.Core.Continuous
{
    /// <summary>
    /// 默认持续体管理器。
    /// 提供按 owner 索引、生命周期批量操作、准入策略和生命周期绑定器。
    /// </summary>
    public class DefaultContinuousManager : IContinuousManager
    {
        private readonly Dictionary<long, List<IContinuous>> _ownerContinuous = new Dictionary<long, List<IContinuous>>();
        private readonly HashSet<IContinuous> _registered = new HashSet<IContinuous>();
        private readonly HashSet<IContinuous> _active = new HashSet<IContinuous>();
        private readonly List<IContinuousAdmissionPolicy> _admissionPolicies = new List<IContinuousAdmissionPolicy>();
        private readonly List<IContinuousLifecycleBinder> _lifecycleBinders = new List<IContinuousLifecycleBinder>();

        /// <summary>
        /// 持续体注册事件。
        /// </summary>
        public event Action<IContinuous>? OnRegistered;

        /// <summary>
        /// 持续体激活事件。
        /// </summary>
        public event Action<IContinuous>? OnActivated;

        /// <summary>
        /// 持续体注销事件。
        /// </summary>
        public event Action<IContinuous, ContinuousEndReason>? OnUnregistered;

        /// <summary>
        /// 最近一次准入失败原因。
        /// </summary>
        public string? LastRejectReason { get; private set; }

        /// <inheritdoc />
        public int ActiveCount => _active.Count;

        /// <inheritdoc />
        public int TotalCount => _registered.Count;

        /// <summary>
        /// 创建默认持续体管理器。
        /// </summary>
        public DefaultContinuousManager(
            IEnumerable<IContinuousAdmissionPolicy>? admissionPolicies = null,
            IEnumerable<IContinuousLifecycleBinder>? lifecycleBinders = null)
        {
            if (admissionPolicies != null)
                _admissionPolicies.AddRange(admissionPolicies);

            if (lifecycleBinders != null)
                _lifecycleBinders.AddRange(lifecycleBinders);
        }

        /// <summary>
        /// 添加准入策略。
        /// </summary>
        public void AddAdmissionPolicy(IContinuousAdmissionPolicy policy)
        {
            if (policy != null && !_admissionPolicies.Contains(policy))
                _admissionPolicies.Add(policy);
        }

        /// <summary>
        /// 移除准入策略。
        /// </summary>
        public bool RemoveAdmissionPolicy(IContinuousAdmissionPolicy policy)
        {
            return policy != null && _admissionPolicies.Remove(policy);
        }

        /// <summary>
        /// 添加生命周期绑定器。
        /// </summary>
        public void AddLifecycleBinder(IContinuousLifecycleBinder binder)
        {
            if (binder != null && !_lifecycleBinders.Contains(binder))
                _lifecycleBinders.Add(binder);
        }

        /// <summary>
        /// 移除生命周期绑定器。
        /// </summary>
        public bool RemoveLifecycleBinder(IContinuousLifecycleBinder binder)
        {
            return binder != null && _lifecycleBinders.Remove(binder);
        }

        /// <inheritdoc />
        public bool Register(IContinuous continuous)
        {
            LastRejectReason = null;
            if (continuous == null || continuous.Config == null)
            {
                LastRejectReason = "Continuous or config is null";
                return false;
            }

            if (_registered.Contains(continuous))
                return true;

            if (!CanRegister(continuous, out var reason))
            {
                LastRejectReason = reason;
                return false;
            }

            _registered.Add(continuous);
            AddOwnerIndex(continuous);
            continuous.OnEnded += HandleContinuousEnded;

            NotifyRegistered(continuous);
            OnRegistered?.Invoke(continuous);
            return true;
        }

        /// <inheritdoc />
        public void Unregister(IContinuous continuous, ContinuousEndReason reason = ContinuousEndReason.CleanedUp)
        {
            if (continuous == null || !_registered.Remove(continuous))
                return;

            continuous.OnEnded -= HandleContinuousEnded;
            _active.Remove(continuous);
            RemoveOwnerIndex(continuous);

            NotifyUnregistered(continuous, reason);
            OnUnregistered?.Invoke(continuous, reason);
        }

        /// <inheritdoc />
        public bool TryActivate(IContinuous continuous)
        {
            LastRejectReason = null;
            if (continuous == null)
            {
                LastRejectReason = "Continuous is null";
                return false;
            }

            if (!_registered.Contains(continuous) && !Register(continuous))
                return false;

            if (!CanActivate(continuous, out var reason))
            {
                LastRejectReason = reason;
                return false;
            }

            continuous.Activate();
            if (continuous.IsActive)
                _active.Add(continuous);
            else
                _active.Remove(continuous);

            NotifyActivated(continuous);
            OnActivated?.Invoke(continuous);
            return continuous.IsActive;
        }

        /// <inheritdoc />
        public IReadOnlyList<IContinuous> GetOwnerContinuous(long ownerId)
        {
            if (!_ownerContinuous.TryGetValue(ownerId, out var list))
                return Array.Empty<IContinuous>();

            return list.AsReadOnly();
        }

        /// <inheritdoc />
        public IReadOnlyList<IContinuous> GetOwnerActiveContinuous(long ownerId)
        {
            if (!_ownerContinuous.TryGetValue(ownerId, out var list) || list.Count == 0)
                return Array.Empty<IContinuous>();

            var result = new List<IContinuous>();
            for (int i = 0; i < list.Count; i++)
            {
                var continuous = list[i];
                if (continuous != null && continuous.IsActive && !continuous.IsTerminated)
                    result.Add(continuous);
            }

            return result;
        }

        /// <summary>
        /// 获取所有已注册持续体快照。
        /// </summary>
        public IReadOnlyList<IContinuous> GetAllContinuous()
        {
            return new List<IContinuous>(_registered);
        }

        /// <summary>
        /// 获取所有活跃持续体快照。
        /// </summary>
        public IReadOnlyList<IContinuous> GetAllActiveContinuous()
        {
            return new List<IContinuous>(_active);
        }

        /// <inheritdoc />
        public void InterruptAll(long ownerId, string reason)
        {
            var snapshot = SnapshotOwner(ownerId);
            for (int i = 0; i < snapshot.Count; i++)
            {
                var continuous = snapshot[i];
                if (continuous == null || continuous.IsTerminated || !continuous.Config.CanBeInterrupted)
                    continue;

                continuous.Abort(reason);
            }
        }

        /// <inheritdoc />
        public void PauseAll(long ownerId)
        {
            var snapshot = SnapshotOwner(ownerId);
            for (int i = 0; i < snapshot.Count; i++)
            {
                var continuous = snapshot[i];
                if (continuous == null || !continuous.IsActive || continuous.IsPaused || continuous.IsTerminated)
                    continue;

                continuous.Pause();
                if (continuous.IsPaused)
                {
                    _active.Remove(continuous);
                    NotifyPaused(continuous);
                }
            }
        }

        /// <inheritdoc />
        public void ResumeAll(long ownerId)
        {
            var snapshot = SnapshotOwner(ownerId);
            for (int i = 0; i < snapshot.Count; i++)
            {
                var continuous = snapshot[i];
                if (continuous == null || !continuous.IsPaused || continuous.IsTerminated)
                    continue;

                if (!CanActivate(continuous, out var reason))
                {
                    LastRejectReason = reason;
                    continue;
                }

                continuous.Resume();
                if (continuous.IsActive)
                {
                    _active.Add(continuous);
                    NotifyResumed(continuous);
                }
            }
        }

        /// <summary>
        /// 按标签中断 owner 下所有匹配的持续体。
        /// </summary>
        public int InterruptByTags(long ownerId, ITagContainer tags, string reason)
        {
            if (tags == null || tags.Count == 0)
                return 0;

            int count = 0;
            var snapshot = SnapshotOwner(ownerId);
            for (int i = 0; i < snapshot.Count; i++)
            {
                var continuous = snapshot[i];
                if (continuous == null || continuous.IsTerminated || !continuous.Config.CanBeInterrupted)
                    continue;

                var tagConfig = continuous.Config as ITagConfig;
                if (tagConfig == null || tagConfig.Tags == null || !tagConfig.Tags.HasAny(tags))
                    continue;

                continuous.Abort(reason);
                count++;
            }

            return count;
        }

        /// <summary>
        /// 注销全部持续体。
        /// </summary>
        public void Clear(ContinuousEndReason reason = ContinuousEndReason.CleanedUp)
        {
            var snapshot = new List<IContinuous>(_registered);
            for (int i = 0; i < snapshot.Count; i++)
                Unregister(snapshot[i], reason);
        }

        /// <summary>
        /// 判断持续体是否允许注册。
        /// </summary>
        protected virtual bool CanRegister(IContinuous continuous, out string? reason)
        {
            for (int i = 0; i < _admissionPolicies.Count; i++)
            {
                if (!_admissionPolicies[i].CanRegister(continuous, this, out reason))
                    return false;
            }

            reason = null;
            return true;
        }

        /// <summary>
        /// 判断持续体是否允许激活。
        /// </summary>
        protected virtual bool CanActivate(IContinuous continuous, out string? reason)
        {
            if (continuous == null || continuous.IsTerminated)
            {
                reason = "Continuous is null or terminated";
                return false;
            }

            for (int i = 0; i < _admissionPolicies.Count; i++)
            {
                if (!_admissionPolicies[i].CanActivate(continuous, this, out reason))
                    return false;
            }

            reason = null;
            return true;
        }

        /// <summary>
        /// 根据持续体状态解析默认结束原因。
        /// </summary>
        protected virtual ContinuousEndReason ResolveEndReason(IContinuous continuous)
        {
            if (continuous == null)
                return ContinuousEndReason.CleanedUp;

            return continuous.State == ContinuousState.Aborted
                ? ContinuousEndReason.Interrupted
                : ContinuousEndReason.Completed;
        }

        private void HandleContinuousEnded(IContinuous continuous, ContinuousEndReason reason)
        {
            if (continuous == null)
                return;

            _active.Remove(continuous);
            NotifyEnded(continuous, reason);
            Unregister(continuous, reason);
        }

        private void AddOwnerIndex(IContinuous continuous)
        {
            var ownerId = continuous.Config.OwnerId;
            if (!_ownerContinuous.TryGetValue(ownerId, out var list))
            {
                list = new List<IContinuous>();
                _ownerContinuous.Add(ownerId, list);
            }

            if (!list.Contains(continuous))
                list.Add(continuous);
        }

        private void RemoveOwnerIndex(IContinuous continuous)
        {
            var ownerId = continuous.Config.OwnerId;
            if (!_ownerContinuous.TryGetValue(ownerId, out var list))
                return;

            list.Remove(continuous);
            if (list.Count == 0)
                _ownerContinuous.Remove(ownerId);
        }

        private List<IContinuous> SnapshotOwner(long ownerId)
        {
            if (!_ownerContinuous.TryGetValue(ownerId, out var list))
                return new List<IContinuous>();

            return new List<IContinuous>(list);
        }

        private void NotifyRegistered(IContinuous continuous)
        {
            for (int i = 0; i < _lifecycleBinders.Count; i++)
                _lifecycleBinders[i].OnRegistered(continuous, this);
        }

        private void NotifyActivated(IContinuous continuous)
        {
            for (int i = 0; i < _lifecycleBinders.Count; i++)
                _lifecycleBinders[i].OnActivated(continuous, this);
        }

        private void NotifyPaused(IContinuous continuous)
        {
            for (int i = 0; i < _lifecycleBinders.Count; i++)
                _lifecycleBinders[i].OnPaused(continuous, this);
        }

        private void NotifyResumed(IContinuous continuous)
        {
            for (int i = 0; i < _lifecycleBinders.Count; i++)
                _lifecycleBinders[i].OnResumed(continuous, this);
        }

        private void NotifyEnded(IContinuous continuous, ContinuousEndReason reason)
        {
            for (int i = 0; i < _lifecycleBinders.Count; i++)
                _lifecycleBinders[i].OnEnded(continuous, reason, this);
        }

        private void NotifyUnregistered(IContinuous continuous, ContinuousEndReason reason)
        {
            for (int i = 0; i < _lifecycleBinders.Count; i++)
                _lifecycleBinders[i].OnUnregistered(continuous, reason, this);
        }
    }
}
