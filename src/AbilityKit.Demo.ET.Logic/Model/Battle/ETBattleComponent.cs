using System;
using ET.AbilityKit.Demo.ET.Share;
using AbilityKit.Demo.Moba.Share;

namespace ET.Logic
{
    /// <summary>
    /// 用于本地 MOBA Demo 场景的 ET 承载战斗组件。
    /// 存储 ET 生命周期状态，并将战斗模拟委托给 IBattleDriver。
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETBattleComponent: Entity, IAwake, IUpdate, IDestroy
    {
        // 战斗标识。
        public long BattleId { get; set; }
        public long PlayerId { get; set; }
        public long PlayerActorId { get; set; }

        // 战斗状态。
        public BattleState State { get; set; } = BattleState.Idle;

        // 活动战斗驱动器暴露的运行时状态。
        public int CurrentFrame => BattleDriver?.CurrentFrame ?? 0;
        public double LogicTimeSeconds => BattleDriver?.LogicTimeSeconds ?? 0;
        public int TickRate => BattleDriver?.TickRate ?? 30;

        // ET 视图事件接收器。
        public IETViewEventSink ViewSink { get; set; }

        // Demo/冒烟自动化需要显式启用。正式战斗启动时保持禁用。
        public ETBattleAutomationOptions AutomationOptions { get; set; } = ETBattleAutomationOptions.CreateDisabled();

        // 持有场景/生命周期上下文的 ET 宿主组件。
        public ETMobaBattleDriver BattleHost { get; set; }

        // 平台无关的战斗驱动端口。
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
