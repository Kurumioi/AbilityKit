using System;
using AbilityKit.Samples.Infrastructure;
using AbilityKit.Samples.Samples.Foundation;
using AbilityKit.Samples.Samples.Triggering;
using AbilityKit.Samples.Samples.Tags;
using AbilityKit.Samples.Samples.Modifiers;
using AbilityKit.Samples.Samples.Flow;
using AbilityKit.Samples.Samples.Pipeline;
using AbilityKit.Samples.Samples.StateMachine;
using AbilityKit.Samples.Samples.Demo;
using SamplesNs_Tags = AbilityKit.Samples.Samples.Tags;

namespace AbilityKit.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            var runner = new SampleRunner();

            // 注册所有示例
            RegisterSamples(runner);

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

        static void RegisterSamples(SampleRunner runner)
        {
            // Foundation
            runner.Register<HelloWorld>();
            runner.Register<EventSystem>();
            runner.Register<ObjectPool>();
            runner.Register<MarkerRegistry>();

            // Triggering
            runner.Register<BasicTrigger>();
            runner.Register<TriggerWithCondition>();
            runner.Register<TriggerWithBlackboard>();

            // Tags
            runner.Register<SamplesNs_Tags.GameplayTags>();
            runner.Register<TagRequirements>();
            runner.Register<TagStack>();

            // Modifiers
            runner.Register<ModifierBasics>();
            runner.Register<AttributeModifiers>();

            // Flow
            runner.Register<FlowBasics>();
            runner.Register<SequenceAndRace>();
            runner.Register<TimedFlow>();

            // Pipeline
            runner.Register<PipelineBasics>();

            // StateMachine
            runner.Register<HFSMBasics>();
            runner.Register<HFSMWithActions>();

            // Demo
            runner.Register<TowerDefense>();
            runner.Register<RPGBattle>();
            runner.Register<TimedTowerDefense>();
        }
    }
}
