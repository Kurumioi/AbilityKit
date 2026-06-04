using System;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// ???? System
    /// ?? Moba.Console ??PhaseHost + InMatchPhase
    /// </summary>
    [EntitySystemOf(typeof(ETFlowComponent))]
    [FriendOf(typeof(ETFlowComponent))]
    [FriendOf(typeof(ETBattleComponent))]
    public static partial class ETFlowComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ETFlowComponent self)
        {
            Log.Info("[ETFlow] ETFlowComponent awake");
        }

        /// <summary>
        /// ?????
        /// </summary>
        public static void StartFlow(this ETFlowComponent self, FlowPhase initialPhase)
        {
            self.CurrentPhase = initialPhase;
            self.CurrentStep = FlowStep.None;
            self.StepsCompleted = 0;
            self.IsTransitioning = false;
            self.PhaseTimer = 0f;

            Log.Info($"[ETFlow] Flow started: {initialPhase}");
        }

        /// <summary>
        /// ??????
        /// </summary>
        public static void TransitionTo(this ETFlowComponent self, FlowPhase phase, FlowStep step)
        {
            Log.Info($"[ETFlow] Transition: {self.CurrentPhase}.{self.CurrentStep} -> {phase}.{step}");
            self.CurrentPhase = phase;
            self.CurrentStep = step;
            self.IsTransitioning = false;
            self.PhaseTimer = 0f;
            self.StepsCompleted++;
        }

        /// <summary>
        /// Tick ??
        /// </summary>
        public static void Tick(this ETFlowComponent self, float deltaTime)
        {
            self.PhaseTimer += deltaTime;

            switch (self.CurrentPhase)
            {
                case FlowPhase.None:
                    self.TickNone();
                    break;
                case FlowPhase.Prepare:
                    self.TickPrepare();
                    break;
                case FlowPhase.Connect:
                    self.TickConnect();
                    break;
                case FlowPhase.CreateWorld:
                    self.TickCreateWorld();
                    break;
                case FlowPhase.LoadAssets:
                    self.TickLoadAssets();
                    break;
                case FlowPhase.InMatch:
                    self.TickInMatch();
                    break;
                case FlowPhase.End:
                    self.TickEnd();
                    break;
            }
        }

        private static void TickNone(this ETFlowComponent self)
        {
            if (!self.IsTransitioning)
            {
                self.IsTransitioning = true;
                self.TransitionTo(FlowPhase.Prepare, FlowStep.Prepare_Initialize);
            }
        }

        private static void TickPrepare(this ETFlowComponent self)
        {
            switch (self.CurrentStep)
            {
                case FlowStep.None:
                    self.TransitionTo(FlowPhase.Prepare, FlowStep.Prepare_Initialize);
                    break;

                case FlowStep.Prepare_Initialize:
                    // ????????????
                    if (self.PhaseTimer > 0.5f)
                    {
                        self.TransitionTo(FlowPhase.Connect, FlowStep.Connect_Connect);
                    }
                    break;

                case FlowStep.Connect_Connect:
                    if (self.PhaseTimer > 1f)
                    {
                        self.TransitionTo(FlowPhase.CreateWorld, FlowStep.CreateWorld_CreateEntities);
                    }
                    break;
            }
        }

        private static void TickConnect(this ETFlowComponent self)
        {
            switch (self.CurrentStep)
            {
                case FlowStep.Connect_WaitPlayers:
                    // ????????
                    break;
            }
        }

        private static void TickCreateWorld(this ETFlowComponent self)
        {
            switch (self.CurrentStep)
            {
                case FlowStep.None:
                    self.TransitionTo(FlowPhase.CreateWorld, FlowStep.CreateWorld_CreateEntities);
                    break;

                case FlowStep.CreateWorld_CreateEntities:
                    // ????
                    if (self.PhaseTimer > 0.5f)
                    {
                        self.TransitionTo(FlowPhase.CreateWorld, FlowStep.CreateWorld_RegisterPlayers);
                    }
                    break;

                case FlowStep.CreateWorld_RegisterPlayers:
                    if (self.PhaseTimer > 1f)
                    {
                        self.TransitionTo(FlowPhase.LoadAssets, FlowStep.LoadAssets_LoadResources);
                    }
                    break;
            }
        }

        private static void TickLoadAssets(this ETFlowComponent self)
        {
            switch (self.CurrentStep)
            {
                case FlowStep.None:
                    self.TransitionTo(FlowPhase.LoadAssets, FlowStep.LoadAssets_LoadResources);
                    break;

                case FlowStep.LoadAssets_LoadResources:
                    if (self.PhaseTimer > 0.5f)
                    {
                        self.TransitionTo(FlowPhase.LoadAssets, FlowStep.LoadAssets_NotifyReady);
                    }
                    break;

                case FlowStep.LoadAssets_NotifyReady:
                    if (self.PhaseTimer > 1f)
                    {
                        self.TransitionTo(FlowPhase.InMatch, FlowStep.None);
                    }
                    break;
            }
        }

        private static void TickInMatch(this ETFlowComponent self)
        {
            switch (self.CurrentStep)
            {
                case FlowStep.None:
                    self.TransitionTo(FlowPhase.InMatch, FlowStep.InMatch_StartBattle);
                    break;

                case FlowStep.InMatch_StartBattle:
                    var battleComponent = self.Scene().GetComponent<ETBattleComponent>();
                    if (battleComponent?.BattleDriver is ETMobaBattleDriver driver && !driver.RuntimeGameStarted)
                    {
                        if (!driver.OnAllPlayersReady(driver.PlayerSpawnData))
                        {
                            Log.Warning("[ETFlow] Runtime game start is not ready; stay in InMatch_StartBattle");
                            return;
                        }
                    }

                    battleComponent?.StartBattle();
                    self.TransitionTo(FlowPhase.InMatch, FlowStep.InMatch_BattleLoop);
                    break;

                case FlowStep.InMatch_BattleLoop:
                    // ??????BattleComponent.Update ??
                    break;

                case FlowStep.InMatch_CheckEnd:
                    self.CheckBattleEnd();
                    break;
            }
        }

        private static void TickEnd(this ETFlowComponent self)
        {
            switch (self.CurrentStep)
            {
                case FlowStep.None:
                    self.TransitionTo(FlowPhase.End, FlowStep.End_Cleanup);
                    break;

                case FlowStep.End_Cleanup:
                    if (self.PhaseTimer > 1f)
                    {
                        self.TransitionTo(FlowPhase.End, FlowStep.End_Finished);
                    }
                    break;

                case FlowStep.End_Finished:
                    break;
            }
        }

        private static void CheckBattleEnd(this ETFlowComponent self)
        {
            var battleComponent = self.Scene().GetComponent<ETBattleComponent>();
            battleComponent?.CheckBattleEnd();
        }
    }
}
