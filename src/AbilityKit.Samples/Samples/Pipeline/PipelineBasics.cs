using System;
using AbilityKit.Samples.Infrastructure;

namespace AbilityKit.Samples.Samples.Pipeline
{
    /// <summary>
    /// PipelineBasics - Pipeline ??
    /// </summary>
    public sealed class PipelineBasics : SampleBase
    {
        public override string Title => "Pipeline Basics";
        public override string Description => "?? AbilityKit.Pipeline ??????";
        public override SampleCategory Category => SampleCategory.Pipeline;

        protected override void OnRun()
        {
            Log("???(Pipeline) ??");
            Output.Divider();

            Log("Pipeline ?????????????????");
            Log("");

            Log("????:");
            Output.Bullet("AbilityPipeline<TCtx>: ?????");
            Output.Bullet("AbilityPipelinePhaseBase<TCtx>: ????");
            Output.Bullet("IAbilityPipelineContext: ???????");
            Output.Bullet("AbilityPipelinePhaseResult: ????");

            Output.Divider();

            Log("????:");
            Output.Bullet("????: ? OnExecute ?????");
            Output.Bullet("????: ? OnUpdate ???????");
            Output.Bullet("????: If/All/Any ????");
            Output.Bullet("????: Sequence/Parallel/Repeat");

            Output.Divider();

            Log("??????:");
            Log("  Run() -> Phase1 -> Phase2 -> Phase3 -> Complete/Failure");
            Log("              |         |         |");
            Log("           Success    Success   Success");
            Log("                         |");
            Log("                      Failure -> ?????");

            Output.Divider();

            Log("??????:");
            Log("  [PreCheck] -> [CheckCost] -> [CastTime] -> [ApplyEffect]");
            Log("       |            |              |              |");
            Log("    ??         ??           ??           ??");
        }
    }
}
