using System;
using AbilityKit.Samples.Infrastructure;
using AbilityKit.Samples.Logic;

namespace AbilityKit.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            var runner = new SampleRunner();

            // 自动注册所有带 [Sample] 标记的示例
            RegisterSamplesWithAttribute(runner);

            // 打印表头
            runner.PrintHeader();

            // 主循环
            bool running = true;
            while (running)
            {
                runner.PrintMenu();

                Console.Write("选择示例 (Q 退出): ");

                var input = Console.ReadLine();

                if (string.IsNullOrEmpty(input))
                    continue;

                if (input.Equals("Q", StringComparison.OrdinalIgnoreCase) ||
                    input.Equals("Quit", StringComparison.OrdinalIgnoreCase))
                {
                    running = false;
                    continue;
                }

                if (int.TryParse(input, out int index))
                {
                    runner.Run(index);
                }
                else
                {
                    Console.WriteLine("无效输入，请输入数字或 Q 退出");
                }
            }

            Console.WriteLine("\n再见!");
        }

        /// <summary>
        /// 使用 Attribute 自动注册所有示例
        /// </summary>
        static void RegisterSamplesWithAttribute(SampleRunner runner)
        {
            // 初始化 SampleRegistry（扫描所有带 [Sample] 标记的类型）
            SampleRegistry.Instance.Initialize();

            // 遍历所有注册的示例类型，创建实例并注册
            foreach (var sampleType in SampleRegistry.Instance.GetAllSampleTypes())
            {
                try
                {
                    var instance = SampleRegistry.Instance.CreateInstance(sampleType);
                    if (instance != null)
                    {
                        runner.Register(instance);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WARN] 注册示例 {sampleType.Name} 失败: {ex.Message}");
                }
            }
        }

    }
}
