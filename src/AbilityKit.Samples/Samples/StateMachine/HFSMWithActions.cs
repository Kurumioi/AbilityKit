using System;
using AbilityKit.Samples.Infrastructure;

namespace AbilityKit.Samples.Samples.StateMachine
{
    /// <summary>
    /// HFSMWithActions - HFSM ???
    /// </summary>
    public sealed class HFSMWithActions : SampleBase
    {
        public override string Title => "HFSM with Actions";
        public override string Description => "?? HFSM ???????";
        public override SampleCategory Category => SampleCategory.StateMachine;

        protected override void OnRun()
        {
            Log("HFSM ???????");
            Output.Divider();

            Log("????:");
            Log("  public class MyState : State");
            Log("  {");
            Log("      public override void OnEnter()");
            Log("      {");
            Log("          // ?????");
            Log("      }");
            Log("");
            Log("      public override void OnLogic()");
            Log("      {");
            Log("          // ????");
            Log("      }");
            Log("");
            Log("      public override void OnExit()");
            Log("      {");
            Log("          // ?????");
            Log("      }");
            Log("  }");

            Output.Divider();

            Log("???????:");
            Output.Bullet("Idle: OnEnter -> ??????");
            Output.Bullet("       OnLogic -> ??????");
            Output.Bullet("       ???? -> ??? Chase");
            Output.Bullet("");
            Output.Bullet("Chase: OnEnter -> ??????");
            Output.Bullet("       OnLogic -> ????");
            Output.Bullet("       ?? < ???? -> ??? Attack");
            Output.Bullet("");
            Output.Bullet("Attack: OnEnter -> ??????");
            Output.Bullet("        OnUpdate -> ??????");
            Output.Bullet("        ???? -> ??? Idle ??? Attack");
        }
    }
}
