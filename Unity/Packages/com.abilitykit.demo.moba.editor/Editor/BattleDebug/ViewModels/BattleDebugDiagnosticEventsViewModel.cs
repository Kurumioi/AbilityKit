using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Diagnostics;
using AbilityKit.Game.Editor.Diagnostics;

namespace AbilityKit.Game.Editor
{
    /// <summary>
    /// 诊断事件面板的 ViewModel：持有过滤状态、查询缓存和结果状态，
    /// 不依赖 UnityEditor，可被 IMGUI、UI Toolkit 或其他前端复用。
    /// 绘制层只读取 <see cref="Items"/>、<see cref="StatusMessage"/> 和 <see cref="StoreRevision"/> 进行渲染。
    /// </summary>
    internal sealed class BattleDebugDiagnosticEventsViewModel
    {
        private const int DisplayLimit = 200;

        private long _lastRequestId;
        private long _lastStoreRevision = -1;
        private bool _lastFilterBySelectedActor;
        private bool _lastFailuresOnly;
        private string _lastSearchText;
        private long _lastSelectedActorId;
        private bool _lastHasSelection;
        private IReadOnlyList<BattleDiagnosticEvent> _cachedItems;

        /// <summary>是否按选中实体 ActorId 过滤。</summary>
        public bool FilterBySelectedActor { get; set; } = true;

        /// <summary>是否只显示失败事件。</summary>
        public bool FailuresOnly { get; set; }

        /// <summary>文本搜索关键字。</summary>
        public string SearchText { get; set; } = string.Empty;

        /// <summary>当前缓存的查询结果（可能为 null 或空）。</summary>
        public IReadOnlyList<BattleDiagnosticEvent> Items => _cachedItems;

        /// <summary>最近一次查询的状态消息（空字符串表示无特殊状态）。</summary>
        public string StatusMessage { get; private set; } = string.Empty;

        /// <summary>最近一次查询时的 Store Revision。</summary>
        public long StoreRevision => _lastStoreRevision;

        /// <summary>标记缓存失效，下次 <see cref="RefreshIfNeeded"/> 会重新查询。</summary>
        public void InvalidateCache()
        {
            _cachedItems = null;
            _lastStoreRevision = -1;
        }

        /// <summary>
        /// 如果缓存有效则直接返回；否则根据当前过滤条件和选中实体重新查询。
        /// </summary>
        /// <param name="session">诊断只读会话。</param>
        /// <param name="selectedActorId">当前选中实体的 ActorId（0 表示无选中）。</param>
        /// <param name="hasSelection">是否存在有效选中。</param>
        /// <returns>当前缓存的事件列表（可能为 null）。</returns>
        public IReadOnlyList<BattleDiagnosticEvent> RefreshIfNeeded(
            IBattleDiagnosticReadOnlySession session,
            long selectedActorId,
            bool hasSelection)
        {
            var currentRevision = session.EventStoreRevision;
            if (_cachedItems != null &&
                _lastStoreRevision == currentRevision &&
                _lastFilterBySelectedActor == FilterBySelectedActor &&
                _lastFailuresOnly == FailuresOnly &&
                string.Equals(_lastSearchText, SearchText, StringComparison.Ordinal) &&
                _lastSelectedActorId == selectedActorId &&
                _lastHasSelection == hasSelection)
            {
                return _cachedItems;
            }

            _lastRequestId++;
            if (_lastRequestId <= 0) _lastRequestId = 1;

            var filter = BuildFilter(selectedActorId, hasSelection);
            var page = new BattleDiagnosticPageRequest(currentRevision, 0, DisplayLimit);
            var query = new BattleDiagnosticEventQuery(_lastRequestId, filter, page);

            var result = session.QueryEvents(query);
            _lastStoreRevision = currentRevision;
            _lastFilterBySelectedActor = FilterBySelectedActor;
            _lastFailuresOnly = FailuresOnly;
            _lastSearchText = SearchText;
            _lastSelectedActorId = selectedActorId;
            _lastHasSelection = hasSelection;

            if (result.Status.CanDisplayResults)
            {
                _cachedItems = result.Items;
                StatusMessage = result.Status.HasMore
                    ? $"显示前 {result.Items.Count} 条（仍有更多）"
                    : string.Empty;
            }
            else
            {
                _cachedItems = result.Items;
                StatusMessage = result.Status.Phase == BattleDiagnosticQueryPhase.Empty
                    ? "无匹配事件"
                    : $"查询不可用：{result.Status.Availability} {result.Status.Message}";
            }

            return _cachedItems;
        }

        private BattleDiagnosticFilter BuildFilter(long selectedActorId, bool hasSelection)
        {
            long actorId = 0;
            var relation = BattleDiagnosticActorRelation.Any;

            if (FilterBySelectedActor && hasSelection)
            {
                actorId = selectedActorId;
                relation = BattleDiagnosticActorRelation.Either;
            }

            return new BattleDiagnosticFilter(
                frames: new BattleDiagnosticFrameFilter(
                    BattleDiagnosticFrames.Invalid,
                    BattleDiagnosticFrames.Invalid),
                channels: BattleDiagnosticEventChannel.All,
                actorId: actorId,
                actorRelation: relation,
                failuresOnly: FailuresOnly,
                unfinishedOnly: false,
                searchText: SearchText ?? string.Empty);
        }
    }
}
