using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AbilityKit.Samples.Common
{
    /// <summary>
    /// 文件日志器 - 将日志写入文本文件
    /// </summary>
    public sealed class FileLogger : ILogger
    {
        private readonly string _filePath;
        private readonly StringBuilder _buffer;
        private readonly List<string> _lines;

        public FileLogger(string filePath)
        {
            _filePath = filePath;
            _buffer = new StringBuilder();
            _lines = new List<string>();

            // 确保目录存在
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public void Info(string message) => Append($"  {message}");
        public void Warn(string message) => Append($"  [WARN] {message}");
        public void Error(string message) => Append($"  [ERR] {message}");

        public void Section(string title)
        {
            Append(string.Empty);
            Append($"=== {title} ===");
        }

        public void Line()
        {
            Append(string.Empty);
        }

        public void Divider()
        {
            Append("  --------------------------------------------------");
        }

        public void Bullet(string text)
        {
            Append($"  • {text}");
        }

        public void Numbered(int num, string text)
        {
            Append($"  {num}. {text}");
        }

        public void KeyValue(string key, string value)
        {
            Append($"  {key}: {value}");
        }

        public void Flush()
        {
            try
            {
                File.WriteAllLines(_filePath, _lines);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to write log file: {ex.Message}");
            }
        }

        private void Append(string text)
        {
            _lines.Add(text);
        }

        /// <summary>
        /// 获取已写入的行数
        /// </summary>
        public int LineCount => _lines.Count;

        /// <summary>
        /// 获取文件路径
        /// </summary>
        public string FilePath => _filePath;
    }
}
