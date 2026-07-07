using System;
using System.Collections.Generic;
using AbilityKit.Coordinator.Core;

namespace AbilityKit.Coordinator.SubFeatures
{
    /// <summary>
    /// 会话快照路由 SubFeature。
    ///
    /// 设计：
    /// - 处理帧快照路由。
    /// - 使用实体状态更新视图时间线。
    /// - 管理实体出生和销毁事件。
    /// </summary>
    public sealed class SessionSnapshotRoutingSubFeature : ISessionSubFeature
    {
        public string Name => "SnapshotRouting";
        public int Priority => 300; // 中等优先级。

        private ISessionHost _host;
        private readonly HashSet<int> _knownEntities = new();
        private int _lastFrame;

        public void OnAttach(ISessionHost host)
        {
            _host = host;
            _knownEntities.Clear();
            _lastFrame = 0;

            // 订阅快照事件。
            _host.Hooks.OnFirstFrameReceived += OnFirstFrameReceived;
        }

        public void OnDetach()
        {
            if (_host == null) return;

            _host.Hooks.OnFirstFrameReceived -= OnFirstFrameReceived;

            _host = null;
            _knownEntities.Clear();
        }

        public void OnTick(float deltaTime) { }

        private void OnFirstFrameReceived()
        {
            _knownEntities.Clear();
            _lastFrame = 0;
        }

        /// <summary>
        /// 将快照路由到视图时间线。
        /// </summary>
        public void RouteSnapshot(int frame, SnapshotEntityState[] states, double timeSeconds)
        {
            if (states == null) return;

            foreach (var state in states)
            {
                bool isNew = !_knownEntities.Contains(state.EntityId);

                if (isNew)
                {
                    _knownEntities.Add(state.EntityId);
                    // TODO：触发实体出生事件。
                }

            }

            _lastFrame = frame;
        }

        /// <summary>
        /// 获取所有已知实体 ID。
        /// </summary>
        public IReadOnlyCollection<int> GetKnownEntities() => _knownEntities;
    }
}
