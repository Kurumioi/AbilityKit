using AbilityKit.Ability.FrameSync;
using AbilityKit.Demo.Moba.Share;

namespace ET.Logic
{
    /// <summary>
    /// Session Events SubFeature Component (ET Demo specific)
    ///
    /// Note: This is kept for backward compatibility.
    /// The Coordinator package has generic implementations that should be used instead.
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETSessionEventsSubFeature : Entity, IAwake, IDestroy
    {
        public ETBattleComponent Owner { get; set; }
        public ETBattleSessionHooks Hooks { get; set; }
        public bool FirstFrameReceived { get; set; }
        public bool SessionStartRequested { get; set; }

        public void Awake()
        {
            FirstFrameReceived = false;
            SessionStartRequested = false;
        }

        public void Destroy()
        {
            Owner = null;
            Hooks = null;
        }

        public void OnAttach(ETBattleComponent owner)
        {
            Owner = owner;
            Hooks = null; // Hooks are stored elsewhere
        }
    }

    /// <summary>
    /// Session Plan SubFeature Component
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETSessionPlanSubFeature : Entity, IAwake, IDestroy
    {
        public ETBattleComponent Owner { get; set; }
        public BattleStartPlan Plan { get; set; }

        public void Awake() { }
        public void Destroy() { Owner = null; }

        public void OnAttach(ETBattleComponent owner)
        {
            Owner = owner;
        }

        public void OnPlanBuilt(in BattleStartPlan plan)
        {
            Plan = plan;
        }
    }

    /// <summary>
    /// Session Lifecycle SubFeature Component
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETSessionLifecycleSubFeature : Entity, IAwake, IDestroy
    {
        public ETBattleComponent Owner { get; set; }

        public void Awake() { }
        public void Destroy() { Owner = null; }

        public void OnAttach(ETBattleComponent owner)
        {
            Owner = owner;
        }
    }

    /// <summary>
    /// Session Snapshot Routing SubFeature Component
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETSessionSnapshotRoutingSubFeature : Entity, IAwake, IDestroy
    {
        public ETBattleComponent Owner { get; set; }
        public FrameSnapshotDispatcher Dispatcher { get; set; }

        public void Awake() { }
        public void Destroy() { Owner = null; }

        public void OnAttach(ETBattleComponent owner)
        {
            Owner = owner;
        }
    }

    /// <summary>
    /// Session TickLoop SubFeature Component
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETSessionTickLoopSubFeature : Entity, IAwake, IDestroy
    {
        public ETBattleComponent Owner { get; set; }
        public int TickRate { get; set; } = 30;
        public bool IsRunning { get; set; }

        public void Awake() { }
        public void Destroy() { Owner = null; }

        public void OnAttach(ETBattleComponent owner)
        {
            Owner = owner;
        }

        public void SetTickRate(int rate)
        {
            TickRate = rate > 0 ? rate : 30;
        }
    }

    /// <summary>
    /// Session Replay SubFeature Component
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETSessionReplaySubFeature : Entity, IAwake, IDestroy
    {
        public ETBattleComponent Owner { get; set; }

        public void Awake() { }
        public void Destroy() { Owner = null; }

        public void OnAttach(ETBattleComponent owner)
        {
            Owner = owner;
        }

        public void SetupReplay(in BattleStartPlan plan)
        {
        }
    }
}
