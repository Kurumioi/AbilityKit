using System;

namespace AbilityKit.Ability.Behavior
{
    /// <summary>
    /// 黑板接口
    /// 抽象了行为树黑板的数据存储能力
    /// </summary>
    public interface IBlackboard
    {
        /// <summary>
        /// 获取黑板值
        /// </summary>
        T GetValue<T>(string key);

        /// <summary>
        /// 设置黑板值
        /// </summary>
        void SetValue<T>(string key, T value);

        /// <summary>
        /// 检查键是否存在
        /// </summary>
        bool HasKey(string key);

        /// <summary>
        /// 获取类型名称（用于调试）
        /// </summary>
        string BlackboardType { get; }
    }

    /// <summary>
    /// 黑板键类型不匹配异常
    /// </summary>
    public sealed class BlackboardTypeMismatchException : Exception
    {
        public string Key { get; }
        public Type ExpectedType { get; }
        public Type ActualType { get; }

        public BlackboardTypeMismatchException(string key, Type expectedType, Type actualType)
            : base($"Blackboard key '{key}' type mismatch: expected {expectedType?.Name ?? "null"}, got {actualType?.Name ?? "null"}")
        {
            Key = key;
            ExpectedType = expectedType;
            ActualType = actualType;
        }
    }

    /// <summary>
    /// 黑板值包装器
    /// 用于类型安全的值存储和转换
    /// </summary>
    public readonly struct BlackboardValue
    {
        public readonly Type ValueType;
        public readonly object Value;

        public BlackboardValue(object value)
        {
            Value = value;
            ValueType = value?.GetType();
        }

        public T Get<T>()
        {
            if (Value == null)
                return default;

            if (Value is T typedValue)
                return typedValue;

            try
            {
                return (T)Convert.ChangeType(Value, typeof(T));
            }
            catch
            {
                throw new BlackboardTypeMismatchException(null, typeof(T), Value.GetType());
            }
        }

        public static BlackboardValue<T> Create<T>(T value) => new BlackboardValue<T>(value);
    }

    /// <summary>
    /// 泛型黑板值包装器
    /// </summary>
    public readonly struct BlackboardValue<T>
    {
        public T Value { get; }

        public BlackboardValue(T value)
        {
            Value = value;
        }

        public static implicit operator T(BlackboardValue<T> wrapper) => wrapper.Value;
        public static implicit operator BlackboardValue<T>(T value) => new BlackboardValue<T>(value);
    }
}
