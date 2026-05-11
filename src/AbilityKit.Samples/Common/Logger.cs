namespace AbilityKit.Samples.Common
{
    /// <summary>
    /// 默认日志器实例
    /// </summary>
    public static class Logger
    {
        private static ILogger _instance = new ConsoleLogger();

        /// <summary>
        /// 获取或设置当前日志器实例
        /// </summary>
        public static ILogger Instance
        {
            get => _instance;
            set => _instance = value ?? new ConsoleLogger();
        }

        /// <summary>
        /// 使用指定的日志器执行操作
        /// </summary>
        public static T With<T>(ILogger logger, System.Func<T> action) where T : class
        {
            var previous = _instance;
            _instance = logger;
            try
            {
                return action();
            }
            finally
            {
                _instance = previous;
            }
        }

        /// <summary>
        /// 创建组合日志器（控制台 + 文件）
        /// </summary>
        public static CompositeLogger CreateConsoleAndFile(string filePath)
        {
            return new CompositeLogger(new ConsoleLogger(), new FileLogger(filePath));
        }
    }
}
