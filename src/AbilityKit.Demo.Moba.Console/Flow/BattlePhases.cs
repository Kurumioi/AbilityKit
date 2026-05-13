using System;

namespace AbilityKit.Demo.Moba.Console.Flow
{
    /// <summary>
    /// ?????Idle?
    /// </summary>
    public sealed class IdlePhase : IPhase
    {
        public string Name => "Idle";

        public void OnEnter(PhaseContext context)
        {
            Platform.Log.Phase("[Idle] Entered Idle phase");
            Platform.Log.Trace("[TRACE] IdlePhase.OnEnter");
        }

        public void OnTick(PhaseContext context, float deltaTime)
        {
        }

        public void OnExit(PhaseContext context, string nextPhase)
        {
            Platform.Log.Phase($"[Idle] Exiting to {nextPhase}");
            Platform.Log.Trace($"[TRACE] IdlePhase.OnExit -> {nextPhase}");
        }
    }

    /// <summary>
    /// ????
    /// </summary>
    public sealed class PreparePhase : IPhase
    {
        public string Name => "Prepare";

        public void OnEnter(PhaseContext context)
        {
            Platform.Log.Phase("[Prepare] Entered Prepare phase");
            Platform.Log.Trace("[TRACE] PreparePhase.OnEnter");
        }

        public void OnTick(PhaseContext context, float deltaTime)
        {
        }

        public void OnExit(PhaseContext context, string nextPhase)
        {
            Platform.Log.Phase($"[Prepare] Exiting to {nextPhase}");
            Platform.Log.Trace($"[TRACE] PreparePhase.OnExit -> {nextPhase}");
        }
    }

    /// <summary>
    /// ????
    /// </summary>
    public sealed class ConnectPhase : IPhase
    {
        public string Name => "Connect";

        public void OnEnter(PhaseContext context)
        {
            Platform.Log.Phase("[Connect] Entered Connect phase");
            Platform.Log.Trace("[TRACE] ConnectPhase.OnEnter");
        }

        public void OnTick(PhaseContext context, float deltaTime)
        {
        }

        public void OnExit(PhaseContext context, string nextPhase)
        {
            Platform.Log.Phase($"[Connect] Exiting to {nextPhase}");
            Platform.Log.Trace($"[TRACE] ConnectPhase.OnExit -> {nextPhase}");
        }
    }

    /// <summary>
    /// ?????????
    /// </summary>
    public sealed class CreateOrJoinWorldPhase : IPhase
    {
        public string Name => "CreateOrJoinWorld";

        public void OnEnter(PhaseContext context)
        {
            Platform.Log.Phase("[CreateOrJoinWorld] Entered CreateOrJoinWorld phase");
            Platform.Log.Trace("[TRACE] CreateOrJoinWorldPhase.OnEnter");
        }

        public void OnTick(PhaseContext context, float deltaTime)
        {
        }

        public void OnExit(PhaseContext context, string nextPhase)
        {
            Platform.Log.Phase($"[CreateOrJoinWorld] Exiting to {nextPhase}");
            Platform.Log.Trace($"[TRACE] CreateOrJoinWorldPhase.OnExit -> {nextPhase}");
        }
    }

    /// <summary>
    /// ??????
    /// </summary>
    public sealed class LoadAssetsPhase : IPhase
    {
        public string Name => "LoadAssets";

        public void OnEnter(PhaseContext context)
        {
            Platform.Log.Phase("[LoadAssets] Entered LoadAssets phase");
            Platform.Log.Trace("[TRACE] LoadAssetsPhase.OnEnter");
        }

        public void OnTick(PhaseContext context, float deltaTime)
        {
        }

        public void OnExit(PhaseContext context, string nextPhase)
        {
            Platform.Log.Phase($"[LoadAssets] Exiting to {nextPhase}");
            Platform.Log.Trace($"[TRACE] LoadAssetsPhase.OnExit -> {nextPhase}");
        }
    }

    /// <summary>
    /// ?????
    /// </summary>
    public sealed class InMatchPhase : IPhase
    {
        private readonly IBattleFlow _flow;
        private readonly IBattleFlowEvents _events;
        private double _battleTime;

        public InMatchPhase(IBattleFlow flow, IBattleFlowEvents events)
        {
            _flow = flow;
            _events = events;
        }

        public string Name => "InMatch";

        public void OnEnter(PhaseContext context)
        {
            _battleTime = 0;
            Platform.Log.Phase("[InMatch] Entered InMatch phase - Battle started!");
            Platform.Log.Trace("[TRACE] InMatchPhase.OnEnter - BATTLE STARTED");
            Platform.Log.Title("BATTLE STARTED");
            _events.BattleStarted?.Invoke();
        }

        public void OnTick(PhaseContext context, float deltaTime)
        {
            _battleTime += deltaTime;
        }

        public void OnExit(PhaseContext context, string nextPhase)
        {
            Platform.Log.Phase($"[InMatch] Exiting to {nextPhase}");
            Platform.Log.Trace($"[TRACE] InMatchPhase.OnExit -> {nextPhase}");
            _events.BattleEnded?.Invoke();
        }

        public void EndBattle()
        {
            Platform.Log.Title("BATTLE ENDED");
            Platform.Log.Battle($"Total battle time: {_battleTime:F0}s");
            _flow?.TransitionTo("End");
        }

        public double BattleTime => _battleTime;
    }

    /// <summary>
    /// ????
    /// </summary>
    public sealed class EndPhase : IPhase
    {
        public string Name => "End";

        public void OnEnter(PhaseContext context)
        {
            Platform.Log.Phase("[End] Entered End phase");
            Platform.Log.Trace("[TRACE] EndPhase.OnEnter");
            Platform.Log.Title("BATTLE RESULTS");
        }

        public void OnTick(PhaseContext context, float deltaTime)
        {
        }

        public void OnExit(PhaseContext context, string nextPhase)
        {
            Platform.Log.Phase($"[End] Exiting to {nextPhase}");
            Platform.Log.Trace($"[TRACE] EndPhase.OnExit -> {nextPhase}");
        }
    }
}
