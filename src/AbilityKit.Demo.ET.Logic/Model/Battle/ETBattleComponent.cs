using System;
using ET.AbilityKit.Demo.ET.Share;
using AbilityKit.Demo.Moba.Share;

namespace ET.Logic
{
    /// <summary>
    /// ET-hosted battle component for local MOBA demo scenes.
    /// Stores ET lifecycle state and delegates battle simulation to IBattleDriver.
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETBattleComponent: Entity, IAwake, IUpdate, IDestroy
    {
        // Battle identity.
        public long BattleId { get; set; }
        public long PlayerId { get; set; }
        public long PlayerActorId { get; set; }

        // Battle state.
        public BattleState State { get; set; } = BattleState.Idle;

        // Runtime state exposed by the active battle driver.
        public int CurrentFrame => BattleDriver?.CurrentFrame ?? 0;
        public double LogicTimeSeconds => BattleDriver?.LogicTimeSeconds ?? 0;
        public int TickRate => BattleDriver?.TickRate ?? 30;

        // ET view event sink.
        public IETViewEventSink ViewSink { get; set; }

        // Demo/smoke automation is explicit opt-in. Formal battle startup leaves it disabled.
        public ETBattleAutomationOptions AutomationOptions { get; set; } = ETBattleAutomationOptions.CreateDisabled();

        // ET host component that owns scene/lifecycle context.
        public ETMobaBattleDriver BattleHost { get; set; }

        // Platform-independent battle driver port.
        public IBattleDriver BattleDriver { get; set; }

        public void Awake()
        {
        }

        public void Update(ETBattleComponent self)
        {
        }

        public void OnDestroy(ETBattleComponent self)
        {
        }
    }
}
