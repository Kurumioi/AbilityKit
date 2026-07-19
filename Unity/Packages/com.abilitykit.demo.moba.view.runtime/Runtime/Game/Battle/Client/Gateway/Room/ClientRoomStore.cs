using System;
using System.Threading;

namespace AbilityKit.Game.Battle.Agent
{
    /// <summary>
    /// 客户端 Room 快照应用结果。
    /// </summary>
    public enum ClientRoomSnapshotApplyResult
    {
        /// <summary>首次应用或更新到更新 revision。事件已触发。</summary>
        Applied,
        /// <summary>相同 revision 的重复 push，幂等忽略，不触发事件。</summary>
        DuplicateIgnored,
        /// <summary>旧 revision 的乱序 push，忽略，不触发事件。</summary>
        StaleIgnored
    }

    /// <summary>
    /// 单一权威客户端 Room 状态仓库。
    /// <para>
    /// - 按 <see cref="ClientRoomSnapshot.RoomRevision"/> 单调递增应用（拒绝旧 revision）。
    /// - 通过 <see cref="ClientRoomSnapshot.LastEventSequence"/> 检测事件缺口，标记 <see cref="IsStale"/>。
    /// - 线程安全（lock）。
    /// </para>
    /// </summary>
    public sealed class ClientRoomStore
    {
        private readonly object _gate = new object();
        private ClientRoomSnapshot _current;
        private bool _stale;

        /// <summary>
        /// 快照变更事件（仅在真正应用新 revision 时触发；重复/旧 revision 不触发）。
        /// </summary>
        public event Action<ClientRoomSnapshot> OnSnapshotChanged;

        /// <summary>
        /// 当前最新快照（或 null）。
        /// </summary>
        public ClientRoomSnapshot Current
        {
            get
            {
                lock (_gate)
                {
                    return _current;
                }
            }
        }

        /// <summary>
        /// 是否检测到事件缺口（收到的 push EventSequence > 本地 + 1），提示需要补拉。
        /// </summary>
        public bool IsStale
        {
            get
            {
                lock (_gate)
                {
                    return _stale;
                }
            }
        }

        /// <summary>
        /// 应用一个快照。
        /// <para>
        /// - 旧 revision（小于 current）忽略。
        /// - 相同 revision 的重复 push 幂等忽略（不触发事件）。
        /// - 新 revision 接受，并检测 EventSequence 缺口。
        /// </para>
        /// </summary>
        public ClientRoomSnapshotApplyResult ApplySnapshot(ClientRoomSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            ClientRoomSnapshot toPublish = null;

            lock (_gate)
            {
                if (_current == null)
                {
                    // 首次应用：检测 EventSequence 缺口（>0 起点视为可能缺口）。
                    _stale = snapshot.LastEventSequence > 1L;
                    _current = snapshot;
                    toPublish = snapshot;
                }
                else
                {
                    if (snapshot.RoomRevision < _current.RoomRevision)
                    {
                        // 旧 revision：忽略。
                        return ClientRoomSnapshotApplyResult.StaleIgnored;
                    }

                    var sameRoom = string.Equals(
                        snapshot.RoomId,
                        _current.RoomId,
                        StringComparison.Ordinal);
                    if (sameRoom && snapshot.NumericRoomId == 0UL)
                    {
                        snapshot.NumericRoomId = _current.NumericRoomId;
                    }

                    if (snapshot.RoomRevision == _current.RoomRevision)
                    {
                        if (sameRoom &&
                            _current.NumericRoomId == 0UL &&
                            snapshot.NumericRoomId != 0UL)
                        {
                            _current = snapshot;
                            toPublish = snapshot;
                        }
                        else
                        {
                            // 相同 revision 且没有新增权威元数据：幂等忽略。
                            return ClientRoomSnapshotApplyResult.DuplicateIgnored;
                        }
                    }
                    else
                    {
                        // 新 revision：检测 EventSequence 缺口。
                        var expectedNext = _current.LastEventSequence + 1L;
                        _stale = snapshot.LastEventSequence > expectedNext;
                        _current = snapshot;
                        toPublish = snapshot;
                    }
                }
            }

            // 在锁外触发事件，避免回调内再次进入 store 造成死锁。
            OnSnapshotChanged?.Invoke(toPublish);
            return ClientRoomSnapshotApplyResult.Applied;
        }

        /// <summary>
        /// 补拉成功后清除 stale 标记。
        /// </summary>
        public void MarkRefreshed()
        {
            lock (_gate)
            {
                _stale = false;
            }
        }

        /// <summary>
        /// 重置仓库（清空当前快照与 stale 标记）。
        /// </summary>
        public void Reset()
        {
            lock (_gate)
            {
                _current = null;
                _stale = false;
            }
        }
    }
}
