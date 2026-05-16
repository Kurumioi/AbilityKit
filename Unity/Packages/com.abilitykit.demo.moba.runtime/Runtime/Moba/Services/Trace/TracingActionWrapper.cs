using System;
using System.Collections.Generic;
using AbilityKit.Core.Common.Log;
using AbilityKit.Trace;

namespace AbilityKit.Demo.Moba.Services
{
    /// <summary>
    /// 溯源感知的 Action 包装器
    /// 用于在 Action 执行时自动创建子节点
    /// </summary>
    public sealed class TracingActionWrapper
    {
        private readonly MobaTraceRegistry _trace;
        private readonly Stack<long> _scopeStack = new Stack<long>();
        private int _actionIndex = 0;

        public TracingActionWrapper(MobaTraceRegistry trace)
        {
            _trace = trace;
        }

        /// <summary>
        /// 开始一个新的溯源上下文
        /// </summary>
        public void BeginScope(long rootId)
        {
            _scopeStack.Push(rootId);
            _actionIndex = 0;
        }

        /// <summary>
        /// 结束当前溯源上下文
        /// </summary>
        public void EndScope(int reason = 0)
        {
            if (_scopeStack.Count > 0)
            {
                var rootId = _scopeStack.Pop();
                _trace.EndRoot(rootId, reason);
            }
        }

        /// <summary>
        /// 获取当前根节点 ID
        /// </summary>
        public long CurrentRootId => _scopeStack.Count > 0 ? _scopeStack.Peek() : 0;

        /// <summary>
        /// 记录动作执行
        /// </summary>
        public TraceTreeScope RecordAction(int actionId, int sourceActorId, int targetActorId)
        {
            if (_trace == null || _scopeStack.Count == 0)
                return default;

            var rootId = _scopeStack.Peek();
            var scope = _trace.CreateActionChild(
                parentRootId: rootId,
                actionId: actionId,
                sourceActorId: sourceActorId,
                targetActorId: targetActorId);

            _actionIndex++;
            return scope;
        }

        /// <summary>
        /// 获取完整的溯源链路
        /// </summary>
        public List<TraceSnapshot<MobaTraceMetadata>> GetChain()
        {
            if (_trace == null || _scopeStack.Count == 0)
                return new List<TraceSnapshot<MobaTraceMetadata>>();

            return _trace.GetChain(_scopeStack.Peek());
        }

        /// <summary>
        /// 验证链路完整性
        /// </summary>
        public void ValidateChain()
        {
            if (_trace != null && _scopeStack.Count > 0)
            {
                _trace.ValidateChain(_scopeStack.Peek());
            }
        }
    }
}
