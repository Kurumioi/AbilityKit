using System;
using System.Collections.Generic;

namespace AbilityKit.Ability.Behavior.BTree
{
    /// <summary>
    /// 黑板桥接器
    /// 在 AbilityKit 的 IBehaviorState 和 IBlackboard 之间同步数据
    /// 支持类型安全的值转换
    /// </summary>
    public sealed class BTreeBlackboardBridge
    {
        private readonly Dictionary<string, string> _stateToBlackboard;
        private readonly Dictionary<string, string> _blackboardToState;
        private readonly Dictionary<string, Type> _keyTypes;

        private IBlackboard _blackboard;

        /// <summary>
        /// 黑板接口（可注入）
        /// </summary>
        public IBlackboard Blackboard
        {
            get => _blackboard ?? throw new InvalidOperationException("Blackboard not set");
            set => _blackboard = value ?? throw new ArgumentNullException(nameof(value));
        }

        public BTreeBlackboardBridge()
        {
            _stateToBlackboard = new Dictionary<string, string>();
            _blackboardToState = new Dictionary<string, string>();
            _keyTypes = new Dictionary<string, Type>();
        }

        /// <summary>
        /// 设置默认的 BTCore 黑板实现
        /// </summary>
        public void SetDefaultBlackboard()
        {
            _blackboard = new BTCoreBlackboardAdapter();
        }

        /// <summary>
        /// 注册状态键到黑板键的映射，同时指定类型
        /// </summary>
        public void RegisterMapping(string stateKey, string blackboardKey, Type valueType)
        {
            _stateToBlackboard[stateKey] = blackboardKey;
            _blackboardToState[blackboardKey] = stateKey;
            _keyTypes[blackboardKey] = valueType;
        }

        /// <summary>
        /// 注册状态键到黑板键的映射（泛型版本）
        /// </summary>
        public void RegisterMapping<T>(string stateKey, string blackboardKey)
        {
            RegisterMapping(stateKey, blackboardKey, typeof(T));
        }

        /// <summary>
        /// 从 AbilityKit 状态同步到黑板
        /// </summary>
        public void SyncStateToBlackboard(IBehaviorState state)
        {
            if (_blackboard == null)
                return;

            foreach (var kvp in _stateToBlackboard)
            {
                var stateKey = kvp.Key;
                var bbKey = kvp.Value;

                if (!state.Has(stateKey))
                    continue;

                var value = state.Get<object>(stateKey);
                if (value == null)
                    continue;

                if (_keyTypes.TryGetValue(bbKey, out var expectedType))
                {
                    SetBlackboardValue(bbKey, value, expectedType);
                }
                else
                {
                    _blackboard.SetValue(bbKey, value);
                }
            }
        }

        /// <summary>
        /// 从黑板同步到 AbilityKit 状态
        /// </summary>
        public void SyncBlackboardToState(IBehaviorState state)
        {
            if (_blackboard == null)
                return;

            foreach (var kvp in _blackboardToState)
            {
                var bbKey = kvp.Key;
                var stateKey = kvp.Value;

                if (!_blackboard.HasKey(bbKey))
                    continue;

                if (_keyTypes.TryGetValue(bbKey, out var expectedType))
                {
                    var value = GetBlackboardValue(bbKey, expectedType);
                    if (value != null)
                    {
                        state.Set(stateKey, value);
                    }
                }
            }
        }

        /// <summary>
        /// 获取黑板值（类型安全）
        /// </summary>
        public T GetValue<T>(string blackboardKey)
        {
            if (_blackboard == null)
                return default;

            return _blackboard.GetValue<T>(blackboardKey);
        }

        /// <summary>
        /// 获取黑板值（运行时类型检查）
        /// </summary>
        public object GetValue(string blackboardKey, Type expectedType)
        {
            if (_blackboard == null)
                return null;

            return GetBlackboardValue(blackboardKey, expectedType);
        }

        /// <summary>
        /// 设置黑板值（类型安全）
        /// </summary>
        public void SetValue<T>(string blackboardKey, T value)
        {
            if (_blackboard == null)
                return;

            _blackboard.SetValue(blackboardKey, value);
        }

        /// <summary>
        /// 检查黑板键是否存在
        /// </summary>
        public bool HasKey(string blackboardKey)
        {
            return _blackboard?.HasKey(blackboardKey) ?? false;
        }

        private void SetBlackboardValue(string key, object value, Type expectedType)
        {
            try
            {
                var method = typeof(IBlackboard).GetMethod(nameof(IBlackboard.SetValue));
                var genericMethod = method.MakeGenericMethod(expectedType);
                genericMethod.Invoke(_blackboard, new object[] { key, value });
            }
            catch
            {
                _blackboard.SetValue(key, value);
            }
        }

        private object GetBlackboardValue(string key, Type expectedType)
        {
            try
            {
                var method = typeof(IBlackboard).GetMethod(nameof(IBlackboard.GetValue));
                var genericMethod = method.MakeGenericMethod(expectedType);
                return genericMethod.Invoke(_blackboard, new object[] { key });
            }
            catch
            {
                return _blackboard.GetValue<object>(key);
            }
        }
    }

    /// <summary>
    /// BTCore 黑板适配器
    /// 将 IBlackboard 适配到 BTCore 的 Blackboard
    /// </summary>
    internal sealed class BTCoreBlackboardAdapter : IBlackboard
    {
        private readonly BTCore.Runtime.Blackboards.Blackboard _blackboard;

        public string BlackboardType => "BTCore";

        public BTCoreBlackboardAdapter()
        {
            _blackboard = new BTCore.Runtime.Blackboards.Blackboard();
        }

        public T GetValue<T>(string key)
        {
            return _blackboard.GetValue<T>(key);
        }

        public void SetValue<T>(string key, T value)
        {
            _blackboard.SetValue(key, value);
        }

        public bool HasKey(string key)
        {
            return _blackboard.Find<object>(key) != null;
        }
    }
}
