using System;
using System.Collections.Generic;
using System.Reflection;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Demo.Moba.Share;
using AbilityKit.Protocol.Moba.StateSync;

namespace ET.Logic
{
    /// <summary>
    /// OpCode 处理方法属性
    /// 用于标记处理特定 OpCode 的方法
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class SnapshotHandlerAttribute : Attribute
    {
        public int OpCode { get; }
        public SnapshotHandlerAttribute(int opCode) => OpCode = opCode;
    }

    /// <summary>
    /// 快照分发结果
    /// </summary>
    public enum SnapshotDispatchResult
    {
        Success,
        HandlerNotFound,
        DispatchFailed
    }

    /// <summary>
    /// 基于 Attribute 的快照分发器
    /// 自动收集所有标记了 [SnapshotHandler] 的方法并注册到分发表
    /// </summary>
    public class ETBattleOpCodeDispatcher
    {
        private readonly Dictionary<int, HandlerInfo> _handlers;
        private readonly object _owner;

        private struct HandlerInfo
        {
            public MethodInfo Method;
            public Type PayloadType;
        }

        public IReadOnlyCollection<int> RegisteredOpCodes => _handlers.Keys;

        public ETBattleOpCodeDispatcher(object owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _handlers = new Dictionary<int, HandlerInfo>();
            RegisterHandlers();
        }

        private void RegisterHandlers()
        {
            var type = _owner.GetType();
            var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<SnapshotHandlerAttribute>();
                if (attr != null)
                {
                    RegisterHandler(attr.OpCode, method);
                }
            }

            Log.Debug($"[ETBattleOpCodeDispatcher] Registered {_handlers.Count} handlers: {string.Join(", ", _handlers.Keys)}");
        }

        private void RegisterHandler(int opCode, MethodInfo method)
        {
            var parameters = method.GetParameters();
            if (parameters.Length != 2 || parameters[0].ParameterType != typeof(int) || parameters[1].ParameterType != typeof(byte[]))
            {
                Log.Warning($"[ETBattleOpCodeDispatcher] Method {method.Name} has invalid signature, expected (int, byte[])");
                return;
            }

            if (_handlers.ContainsKey(opCode))
            {
                Log.Warning($"[ETBattleOpCodeDispatcher] Handler for OpCode {opCode} already registered, skipping {method.Name}");
                return;
            }

            _handlers[opCode] = new HandlerInfo { Method = method, PayloadType = typeof(byte[]) };
            Log.Debug($"[ETBattleOpCodeDispatcher] Registered handler: {method.Name} for OpCode {opCode}");
        }

        /// <summary>
        /// Convert ArraySegment to byte[]
        /// </summary>
        private static byte[] SegmentToArray(ArraySegment<byte> segment)
        {
            if (segment.Array == null)
                return Array.Empty<byte>();

            if (segment.Offset == 0 && segment.Count == segment.Array.Length)
                return segment.Array;

            var result = new byte[segment.Count];
            Array.Copy(segment.Array, segment.Offset, result, 0, segment.Count);
            return result;
        }

        /// <summary>
        /// 分发快照到对应的处理方法
        /// </summary>
        public SnapshotDispatchResult Dispatch(int frame, in WorldStateSnapshot snapshot)
        {
            var opCode = snapshot.OpCode;

            if (!_handlers.TryGetValue(opCode, out var handler))
            {
                Log.Debug($"[ETBattleOpCodeDispatcher] No handler for OpCode {opCode}");
                return SnapshotDispatchResult.HandlerNotFound;
            }

            try
            {
                var payload = SegmentToArray(snapshot.Payload);
                handler.Method.Invoke(_owner, new object[] { frame, payload });
                return SnapshotDispatchResult.Success;
            }
            catch (Exception ex)
            {
                Log.Error($"[ETBattleOpCodeDispatcher] Dispatch failed for OpCode {opCode}: {ex.Message}");
                return SnapshotDispatchResult.DispatchFailed;
            }
        }

        /// <summary>
        /// 尝试分发快照
        /// </summary>
        public bool TryDispatch(int frame, in WorldStateSnapshot snapshot)
        {
            return Dispatch(frame, snapshot) == SnapshotDispatchResult.Success;
        }

        /// <summary>
        /// 检查是否有指定 OpCode 的处理方法
        /// </summary>
        public bool HasHandler(int opCode) => _handlers.ContainsKey(opCode);

        /// <summary>
        /// 获取已注册的处理方法数量
        /// </summary>
        public int HandlerCount => _handlers.Count;
    }
}
