using System;
using System.Collections.Generic;

namespace AbilityKit.Demo.Moba.Console.Platform
{
    public interface ILogSink
    {
        void Log(OutputChannel channel, string message);
        string Name { get; }
    }

    public static class Log
    {
        private static IOutput _output = new Console_.ConsoleOutput();
        private static readonly List<ILogSink> _sinks = new();
        private static readonly object _lock = new();

        public static void SetOutput(IOutput output)
        {
            _output = output ?? new Console_.ConsoleOutput();
        }

        public static IOutput Output => _output;

        public static void AddSink(ILogSink sink)
        {
            if (sink == null) return;
            lock (_lock)
            {
                if (!_sinks.Contains(sink))
                    _sinks.Add(sink);
            }
        }

        public static void RemoveSink(ILogSink sink)
        {
            if (sink == null) return;
            lock (_lock)
                _sinks.Remove(sink);
        }

        public static void ClearSinks()
        {
            lock (_lock)
                _sinks.Clear();
        }

        public static IReadOnlyList<ILogSink> GetSinks()
        {
            lock (_lock)
                return _sinks.ToArray();
        }

        private static void PublishToSinks(OutputChannel channel, string message)
        {
            ILogSink[] snapshot;
            lock (_lock)
            {
                if (_sinks.Count == 0) return;
                snapshot = _sinks.ToArray();
            }

            foreach (var sink in snapshot)
            {
                try { sink.Log(channel, message); }
                catch { }
            }
        }

        public enum LogLevel
        {
            Trace = 0, Debug = 1, System = 2, Config = 3, Phase = 4, Battle = 5,
            Skill = 6, Damage = 7, Sync = 8, Input = 9, View = 10,
            Prediction = 11, Warning = 12, Error = 13
        }

        private static LogLevel _minLevel = LogLevel.Battle;

        public static void SetMinLevel(LogLevel level) => _minLevel = level;
        public static LogLevel MinLevel => _minLevel;
        public static void EnableTrace() => _minLevel = LogLevel.Trace;
        public static void DisableTrace() => _minLevel = LogLevel.Battle;

        private static bool ShouldLog(OutputChannel channel)
        {
            var level = channel switch
            {
                OutputChannel.System => LogLevel.System,
                OutputChannel.Phase => LogLevel.Phase,
                OutputChannel.Battle => LogLevel.Battle,
                OutputChannel.Skill => LogLevel.Skill,
                OutputChannel.Damage => LogLevel.Damage,
                OutputChannel.Sync => LogLevel.Warning,      // ?????????Warning??????
                OutputChannel.Input => LogLevel.Warning,      // ?????????Warning??????
                OutputChannel.Prediction => LogLevel.Prediction,
                OutputChannel.View => LogLevel.View,
                OutputChannel.Buff => LogLevel.Skill,
                OutputChannel.Projectile => LogLevel.Skill,
                OutputChannel.Area => LogLevel.Battle,
                OutputChannel.Entity => LogLevel.Battle,
                OutputChannel.Config => LogLevel.System,
                OutputChannel.Debug => LogLevel.Debug,
                OutputChannel.Warning => LogLevel.Warning,
                OutputChannel.Error => LogLevel.Error,
                OutputChannel.Trace => LogLevel.Trace,
                _ => LogLevel.System
            };
            return level >= _minLevel;
        }

        private static void WriteToChannel(OutputChannel channel, string message)
        {
            if (!ShouldLog(channel)) return;
            _output.Write(channel, message);
            PublishToSinks(channel, message);
        }

        private static void WriteFormatToChannel(OutputChannel channel, string format, params object[] args)
        {
            if (!ShouldLog(channel)) return;
            var message = string.Format(format, args);
            _output.WriteFormat(channel, format, args);
            PublishToSinks(channel, message);
        }

        public static void System(string message) => WriteToChannel(OutputChannel.System, message);
        public static void System(string format, params object[] args) => WriteFormatToChannel(OutputChannel.System, format, args);
        public static void Phase(string message) => WriteToChannel(OutputChannel.Phase, message);
        public static void Phase(string format, params object[] args) => WriteFormatToChannel(OutputChannel.Phase, format, args);
        public static void View(string message) => WriteToChannel(OutputChannel.View, message);
        public static void View(string format, params object[] args) => WriteFormatToChannel(OutputChannel.View, format, args);
        public static void Sync(string message) => WriteToChannel(OutputChannel.Sync, message);
        public static void Sync(string format, params object[] args) => WriteFormatToChannel(OutputChannel.Sync, format, args);
        public static void Input(string message) => WriteToChannel(OutputChannel.Input, message);
        public static void Input(string format, params object[] args) => WriteFormatToChannel(OutputChannel.Input, format, args);
        public static void Prediction(string message) => WriteToChannel(OutputChannel.Prediction, message);
        public static void Prediction(string format, params object[] args) => WriteFormatToChannel(OutputChannel.Prediction, format, args);
        public static void Battle(string message) => WriteToChannel(OutputChannel.Battle, message);
        public static void Battle(string format, params object[] args) => WriteFormatToChannel(OutputChannel.Battle, format, args);
        public static void Skill(string message) => WriteToChannel(OutputChannel.Skill, message);
        public static void Skill(string format, params object[] args) => WriteFormatToChannel(OutputChannel.Skill, format, args);
        public static void Damage(string message) => WriteToChannel(OutputChannel.Damage, message);
        public static void Damage(string format, params object[] args) => WriteFormatToChannel(OutputChannel.Damage, format, args);
        public static void Buff(string message) => WriteToChannel(OutputChannel.Buff, message);
        public static void Buff(string format, params object[] args) => WriteFormatToChannel(OutputChannel.Buff, format, args);
        public static void Projectile(string message) => WriteToChannel(OutputChannel.Projectile, message);
        public static void Projectile(string format, params object[] args) => WriteFormatToChannel(OutputChannel.Projectile, format, args);
        public static void Area(string message) => WriteToChannel(OutputChannel.Area, message);
        public static void Area(string format, params object[] args) => WriteFormatToChannel(OutputChannel.Area, format, args);
        public static void Entity(string message) => WriteToChannel(OutputChannel.Entity, message);
        public static void Entity(string format, params object[] args) => WriteFormatToChannel(OutputChannel.Entity, format, args);
        public static void Config(string message) => WriteToChannel(OutputChannel.Config, message);
        public static void Config(string format, params object[] args) => WriteFormatToChannel(OutputChannel.Config, format, args);

        /// <summary>
        /// 属性日志 - 用于调试属性系统
        /// </summary>
        public static void Attribute(string message) => WriteToChannel(OutputChannel.Debug, $"[ATTR] {message}");
        public static void Attribute(string format, params object[] args) => WriteFormatToChannel(OutputChannel.Debug, $"[ATTR] {format}", args);

        /// <summary>
        /// 冷却日志 - 用于调试冷却系统
        /// </summary>
        public static void Cooldown(string message) => WriteToChannel(OutputChannel.Debug, $"[CD] {message}");
        public static void Cooldown(string format, params object[] args) => WriteFormatToChannel(OutputChannel.Debug, $"[CD] {format}", args);

        public static void Debug(string format, params object[] args) => WriteFormatToChannel(OutputChannel.Debug, format, args);
        public static void Warn(string message) => WriteToChannel(OutputChannel.Warning, message);
        public static void Warn(string format, params object[] args) => WriteFormatToChannel(OutputChannel.Warning, format, args);
        public static void Error(string message) => WriteToChannel(OutputChannel.Error, message);
        public static void Error(string format, params object[] args) => WriteFormatToChannel(OutputChannel.Error, format, args);
        public static void Trace(string message) => WriteToChannel(OutputChannel.Trace, message);
        public static void Trace(string format, params object[] args) => WriteFormatToChannel(OutputChannel.Trace, format, args);

        public static void Separator(char c = '=', int length = 60) => _output.WriteSeparator(OutputChannel.System, c, length);
        public static void Title(string title, char c = '=', int width = 60) => _output.WriteTitle(OutputChannel.System, title, c, width);
        public static void Clear() => _output.Clear();
    }
}
