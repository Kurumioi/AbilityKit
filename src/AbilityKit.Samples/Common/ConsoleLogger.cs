using System;

namespace AbilityKit.Samples.Common
{
    /// <summary>
    /// 控制台日志器
    /// </summary>
    public sealed class ConsoleLogger : ILogger
    {
        public void Info(string message) => Console.WriteLine($"  {message}");
        public void Warn(string message) => Console.WriteLine($"  [WARN] {message}");
        public void Error(string message) => Console.WriteLine($"  [ERR] {message}");

        public void Section(string title)
        {
            Console.WriteLine();
            Console.WriteLine($"=== {title} ===");
        }

        public void Line()
        {
            Console.WriteLine();
        }

        public void Divider()
        {
            Console.WriteLine("  --------------------------------------------------");
        }

        public void Bullet(string text)
        {
            Console.WriteLine($"  • {text}");
        }

        public void Numbered(int num, string text)
        {
            Console.WriteLine($"  {num}. {text}");
        }

        public void KeyValue(string key, string value)
        {
            Console.WriteLine($"  {key}: {value}");
        }

        public void Flush()
        {
            // Console is always flushed immediately
        }
    }
}
