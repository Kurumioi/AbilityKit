using System;
using AbilityKit.Samples.Infrastructure;

namespace AbilityKit.Samples.Samples.StateMachine
{
    /// <summary>
    /// HFSMBasics - HFSM ??
    /// </summary>
    public sealed class HFSMBasics : SampleBase
    {
        public override string Title => "HFSM Basics";
        public override string Description => "?? UnityHFSM ???????";
        public override SampleCategory Category => SampleCategory.StateMachine;

        protected override void OnRun()
        {
            Log("??????? (HFSM)");
            Output.Divider();

            Log("HFSM ? UnityHFSM ???????????");
            Log("");

            Log("????:");
            Output.Bullet("StateMachine: ?????");
            Output.Bullet("State: ????");
            Output.Bullet("Transition: ????");
            Output.Bullet("Hierarchy: ????");

            Output.Divider();

            Log("??????:");
            Output.Bullet("OnEnter(): ????");
            Output.Bullet("OnLogic(): ????");
            Output.Bullet("OnUpdate(): ????");
            Output.Bullet("OnExit(): ????");

            Output.Divider();

            Log("????:");
            Log("  var fsm = new StateMachine();");
            Log("  fsm.AddState(\"Idle\", new State());");
            Log("  fsm.AddState(\"Move\", new State());");
            Log("  fsm.AddTransition(\"Idle\", \"Move\", t => /*??*/);");
            Log("  fsm.SetStartState(\"Idle\");");
            Log("  fsm.Init();");
            Log("  fsm.OnLogic();");

            Output.Divider();

            Log("????:");
            Output.Bullet("??????????");
            Output.Bullet("?? OnEnter/OnExit ??");
            Output.Bullet("??????????");
        }
    }
}
