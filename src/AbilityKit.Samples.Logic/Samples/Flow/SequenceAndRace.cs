using System;
using AbilityKit.Samples.Abstractions;

namespace AbilityKit.Samples.Logic.Samples.Flow
{
    /// <summary>
    /// SequenceAndRace - Sequence 与 Race 示例
    /// </summary>
    [Sample]
    public sealed class SequenceAndRace : SampleBase
    {
        public override string Title => "Sequence and Race";
        public override string Description => "Flow 中的 Sequence 与 Race 组合器";
        public override SampleCategory Category => SampleCategory.Flow;

        protected override void OnRun()
        {
            Log("Sequence 与 Race 组合器");
            Output.Divider();

            Log("1. SequenceNode (顺序执行):");
            Log("   预期结果:");
            Log("   [Step1] -> [Step2] -> [Step3] -> Done");
            Output.Bullet("子节点按顺序依次执行");
            Output.Bullet("只有当前节点成功才执行下一个节点");

            Output.Divider();

            Log("2. RaceNode (竞速执行):");
            Log("   预期结果:");
            Log("   [A] ---win---> Done");
            Log("   [B]");
            Log("   [C]");
            Output.Bullet("子节点并行执行");
            Output.Bullet("最先完成的节点决定结果");
            Output.Bullet("取消其他节点");

            Output.Divider();

            Log("3. ParallelAllNode (全部完成):");
            Log("   预期结果:");
            Log("   [A] ---+");
            Log("   [B] ---+----> Done");
            Log("   [C] ---+");
            Output.Bullet("等待所有节点完成");
            Output.Bullet("收集所有节点的结果");

            Output.Divider();

            Log("总结:");
            Output.Bullet("Sequence: 顺序 -> 顺序 -> 顺序");
            Output.Bullet("Race: 并行 -> 竞速 -> 获胜");
            Output.Bullet("ParallelAll: 并行 -> 等待 -> 全部");
        }
    }
}
