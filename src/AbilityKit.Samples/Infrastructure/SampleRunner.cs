using System;
using System.Collections.Generic;
using System.Linq;
using AbilityKit.Samples.Abstractions;

namespace AbilityKit.Samples.Infrastructure
{
    /// <summary>
    /// 示例运行器 - 控制台视图层
    /// </summary>
    public sealed class SampleRunner
    {
        private readonly List<SampleInfo> _samples = new();
        private readonly Dictionary<SampleCategory, List<SampleInfo>> _grouped = new();

        public IReadOnlyList<SampleInfo> AllSamples => _samples;
        public IReadOnlyDictionary<SampleCategory, List<SampleInfo>> Grouped => _grouped;

        /// <summary>
        /// 注册示例
        /// </summary>
        public void Register(ISample sample)
        {
            var info = new SampleInfo
            {
                Index = _samples.Count,
                Title = sample.Title,
                Description = sample.Description,
                Category = sample.Category,
                Factory = () => sample
            };

            _samples.Add(info);

            if (!_grouped.ContainsKey(sample.Category))
            {
                _grouped[sample.Category] = new List<SampleInfo>();
            }
            _grouped[sample.Category].Add(info);
        }

        /// <summary>
        /// 运行指定索引的示例
        /// </summary>
        public bool Run(int index)
        {
            if (index < 0 || index >= _samples.Count)
            {
                Console.Error.WriteLine($"[ERR] Invalid index: {index}");
                return false;
            }

            var sample = _samples[index];
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine($"运行: {sample.Title}");
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine();

            try
            {
                sample.Factory().Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine();
            Console.WriteLine("───────────────────────────────────────────────────────────");
            Console.WriteLine();

            return true;
        }

        /// <summary>
        /// 打印菜单
        /// </summary>
        public void PrintMenu()
        {
            Console.WriteLine();

            var sortedCategories = _grouped.Keys
                .OrderBy(k => (int)k)
                .ToList();

            foreach (var category in sortedCategories)
            {
                var samples = _grouped[category];
                if (samples.Count == 0) continue;

                Console.WriteLine($"── {category.GetDisplayName()} ──");
                foreach (var sample in samples)
                {
                    Console.WriteLine($"  [{sample.Index:D2}] {sample.Title}");
                }
            }

            Console.WriteLine();
        }

        /// <summary>
        /// 打印表头
        /// </summary>
        public void PrintHeader()
        {
            Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║           AbilityKit.Samples - 示例程序               ║");
            Console.WriteLine("║  纯逻辑示例，展示 AbilityKit 框架的核心功能           ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        }
    }

    /// <summary>
    /// 示例信息
    /// </summary>
    public sealed class SampleInfo
    {
        public int Index { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public SampleCategory Category { get; init; }
        public Func<ISample> Factory { get; init; } = () => throw new InvalidOperationException();
    }
}
