namespace AbilityKit.Context
{
    /// <summary>
    /// 上下文值来源。
    /// </summary>
    public enum ContextValueSource
    {
        None = 0,
        Realtime = 1,
        Snapshot = 2,
        DefaultValue = 3
    }

    /// <summary>
    /// 上下文值读取模式。
    /// </summary>
    public enum ContextValueReadMode
    {
        RealtimeThenSnapshot = 0,
        RealtimeOnly = 1,
        SnapshotOnly = 2,
        SnapshotThenRealtime = 3
    }

    /// <summary>
    /// 上下文值读取结果。
    /// </summary>
    public readonly struct ContextValueResult<T>
    {
        public ContextValueResult(bool found, T value, ContextValueSource source)
        {
            Found = found;
            Value = value;
            Source = source;
        }

        public bool Found { get; }
        public T Value { get; }
        public ContextValueSource Source { get; }
        public bool IsRealtime => Source == ContextValueSource.Realtime;
        public bool IsSnapshot => Source == ContextValueSource.Snapshot;

        public static ContextValueResult<T> Missing()
        {
            return new ContextValueResult<T>(false, default, ContextValueSource.None);
        }

        public static ContextValueResult<T> FromDefault(T value)
        {
            return new ContextValueResult<T>(true, value, ContextValueSource.DefaultValue);
        }
    }
}
