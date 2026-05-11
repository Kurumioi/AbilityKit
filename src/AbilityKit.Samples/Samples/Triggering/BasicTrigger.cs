using System;
using AbilityKit.Samples.Infrastructure;

namespace AbilityKit.Samples.Samples.Triggering
{
    /// <summary>
    /// BasicTrigger - ?????
    /// </summary>
    public sealed class BasicTrigger : SampleBase
    {
        public override string Title => "Basic Trigger";
        public override string Description => "?? Triggering ????????";
        public override SampleCategory Category => SampleCategory.Triggering;

        protected override void OnRun()
        {
            Log("???? (Triggering)");
            Output.Divider();

            Log("?????????????????????");
            Log("");

            Log("????:");
            Output.Bullet("EventKey<T>: ?????");
            Output.Bullet("ITrigger<TArgs, TCtx>: ?????");
            Output.Bullet("TriggerRunner<TCtx>: ??????");
            Output.Bullet("ExecCtx<TCtx>: ?????");

            Output.Divider();

            Log("???????:");
            Log("  [Event] -> [Evaluate] -> [Execute] -> [Done]");
            Log("              |                    |");
            Log("           ????              ????");
            Log("              |");
            Log("           ?????");

            Output.Divider();

            Log("ITrigger ??:");
            Log("  public interface ITrigger<TArgs, TCtx>");
            Log("  {");
            Log("      ITriggerCue? Cue { get; }");
            Log("      bool Evaluate(in TArgs args, in ExecCtx<TCtx> ctx);");
            Log("      void Execute(in TArgs args, in ExecCtx<TCtx> ctx);");
            Log("  }");

            Output.Divider();

            Log("API ????:");
            Output.Bullet("AbilityKit.Triggering.Runtime");
            Output.Bullet("AbilityKit.Triggering.Config.Plans");
        }
    }
}
