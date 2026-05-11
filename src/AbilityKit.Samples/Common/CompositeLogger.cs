using System;
using System.Collections.Generic;

namespace AbilityKit.Samples.Common
{
    /// <summary>
    /// 组合日志器 - 同时向多个日志器输出
    /// </summary>
    public sealed class CompositeLogger : ILogger
    {
        private readonly List<ILogger> _loggers;

        public CompositeLogger(params ILogger[] loggers)
        {
            _loggers = new List<ILogger>(loggers);
        }

        public CompositeLogger(IEnumerable<ILogger> loggers)
        {
            _loggers = new List<ILogger>(loggers);
        }

        /// <summary>
        /// 添加日志器
        /// </summary>
        public void Add(ILogger logger)
        {
            _loggers.Add(logger);
        }

        /// <summary>
        /// 移除日志器
        /// </summary>
        public bool Remove(ILogger logger)
        {
            return _loggers.Remove(logger);
        }

        private void Log(Action<ILogger> action)
        {
            foreach (var logger in _loggers)
            {
                action(logger);
            }
        }

        public void Info(string message) => Log(l => l.Info(message));
        public void Warn(string message) => Log(l => l.Warn(message));
        public void Error(string message) => Log(l => l.Error(message));
        public void Section(string title) => Log(l => l.Section(title));
        public void Line() => Log(l => l.Line());
        public void Divider() => Log(l => l.Divider());
        public void Bullet(string text) => Log(l => l.Bullet(text));
        public void Numbered(int num, string text) => Log(l => l.Numbered(num, text));
        public void KeyValue(string key, string value) => Log(l => l.KeyValue(key, value));

        public void Flush()
        {
            foreach (var logger in _loggers)
            {
                logger.Flush();
            }
        }
    }
}
