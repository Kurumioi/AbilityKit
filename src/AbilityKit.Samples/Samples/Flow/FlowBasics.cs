using System;
using AbilityKit.Samples.Infrastructure;

namespace AbilityKit.Samples.Samples.Flow
{
    /// <summary>
    /// FlowBasics - Flow ??
    /// </summary>
    public sealed class FlowBasics : SampleBase
    {
        public override string Title => "Flow Basics";
        public override string Description => "?? Flow ????????????";
        public override SampleCategory Category => SampleCategory.Flow;

        protected override void OnRun()
        {
            Log("Flow ???????");
            Output.Divider();

            Log("Flow ???? Unity coroutine ???????????");
            Log("");

            Log("????:");
            Output.Bullet("FlowHost<TArgs>: ????");
            Output.Bullet("FlowSession: ????");
            Output.Bullet("IFlowNode: ??????");

            Output.Divider();

            Log("????:");
            Output.Bullet("ActionNode: ????");
            Output.Bullet("SequenceNode: ????");
            Output.Bullet("RaceNode: ????");
            Output.Bullet("ParallelAllNode: ????");
            Output.Bullet("IfNode: ????");
            Output.Bullet("WhileNode: ??");
            Output.Bullet("AwaitCallbackNode: ????");

            Output.Divider();

            Log("??:");
            Output.Bullet("?????????");
            Output.Bullet("???????");
            Output.Bullet("????");

            Output.Divider();

            Log("????:");
            Log("  var host = new FlowHost<T>(provider);");
            Log("  var session = host.Start(args);");
            Log("  while (!session.IsDone)");
            Log("  {");
            Log("      session.Tick(deltaTime);");
            Log("  }");
        }
    }
}
