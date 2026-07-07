using System;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// 战斗视图事件处理器。
    /// 订阅逻辑层事件并创建 Logic 层单位。
    ///
    /// 设计：
    /// - 这些处理器接收由 ETBattleViewEventSink 发布的事件。
    /// - 处理器只更新 Logic 层数据，不重新发布事件。
    /// - View 层直接从 ETBattleViewEventSink 接收事件。
    /// - 重新发布事件会导致 View 层收到重复事件。
    /// </summary>
    [Event(SceneType.DemoBattle)]
    public class ETBattleView_EventHandler: AEvent<Scene, ActorSpawnEvent>
    {
        protected override async ETTask Run(Scene scene, ActorSpawnEvent args)
        {
            // 注意：ETUnit 创建由 ETMobaBattleDriver 中的 TriggerEnterGameSnapshot 处理。
            // 该处理器只处理视图层关注点，不处理 Logic 层单位创建。
            // ActorSpawnEvent 当前只用于视图渲染（ETUnitViewComponent）。
            Log.Debug($"[ETBattleView] ActorSpawnEvent received: {args.Name} (ActorId={args.ActorId}) - view layer rendering handled elsewhere");

            await ETTask.CompletedTask;
        }
    }

    /// <summary>
    /// 单位移动事件处理器。
    /// 更新 Logic 层单位位置（不要重新发布，View 直接从 Sink 接收）。
    /// </summary>
    [Event(SceneType.DemoBattle)]
    public class ETBattleView_ActorMove_Handler: AEvent<Scene, ActorMoveEvent>
    {
        private static int _loggedMoveCount;

        protected override async ETTask Run(Scene scene, ActorMoveEvent args)
        {
            // 只更新 Logic 层单位位置。
            // 不要重新发布，View 层会直接从 ETBattleViewEventSink 接收 ActorMoveEvent。
            var unitComponent = scene.GetComponent<ETUnitComponent>();
            if (unitComponent != null)
            {
                var unit = unitComponent.GetUnit(args.ActorId);
                if (unit != null)
                {
                    unit.X = args.X;
                    unit.Y = args.Y;

                    _loggedMoveCount++;
                    if (_loggedMoveCount <= 5 || _loggedMoveCount % 60 == 0)
                    {
                        Log.Info($"[ETBattleView] Unit position updated: ActorId={args.ActorId}, X={args.X:F3}, Y={args.Y:F3}, Count={_loggedMoveCount}");
                    }
                }
            }
            await ETTask.CompletedTask;
        }
    }

    /// <summary>
    /// 单位伤害事件处理器。
    /// 不要重新发布，View 直接从 Sink 接收。
    /// </summary>
    [Event(SceneType.DemoBattle)]
    public class ETBattleView_ActorDamage_Handler: AEvent<Scene, ActorDamageEvent>
    {
        protected override async ETTask Run(Scene scene, ActorDamageEvent args)
        {
            // 更新 Logic 层单位生命值。
            var unitComponent = scene.GetComponent<ETUnitComponent>();
            if (unitComponent != null)
            {
                var unit = unitComponent.GetUnit(args.ActorId);
                if (unit != null)
                {
                    unit.Hp = args.CurrentHp;
                    unit.MaxHp = args.MaxHp;
                }
            }
            // 不要重新发布，View 层会直接从 ETBattleViewEventSink 接收 ActorDamageEvent。
            await ETTask.CompletedTask;
        }
    }

    /// <summary>
    /// 单位死亡事件处理器。
    /// 不要重新发布，View 直接从 Sink 接收。
    /// </summary>
    [Event(SceneType.DemoBattle)]
    public class ETBattleView_ActorDead_Handler: AEvent<Scene, ActorDeadEvent>
    {
        protected override async ETTask Run(Scene scene, ActorDeadEvent args)
        {
            // 更新 Logic 层单位状态。
            var unitComponent = scene.GetComponent<ETUnitComponent>();
            if (unitComponent != null)
            {
                var unit = unitComponent.GetUnit(args.ActorId);
                if (unit != null)
                {
                    unit.Hp = 0;
                }
            }
            // 不要重新发布，View 层会直接从 ETBattleViewEventSink 接收 ActorDeadEvent。
            await ETTask.CompletedTask;
        }
    }

    /// <summary>
    /// 战斗开始事件处理器。
    /// </summary>
    [Event(SceneType.DemoBattle)]
    public class ETBattleView_BattleStart_Handler: AEvent<Scene, BattleStartEvent>
    {
        protected override async ETTask Run(Scene scene, BattleStartEvent args)
        {
            Log.Info($"[ETBattleView] Battle start: {args.BattleId}");
            await ETTask.CompletedTask;
        }
    }

    /// <summary>
    /// 战斗结束事件处理器。
    /// </summary>
    [Event(SceneType.DemoBattle)]
    public class ETBattleView_BattleEnd_Handler: AEvent<Scene, BattleEndEvent>
    {
        protected override async ETTask Run(Scene scene, BattleEndEvent args)
        {
            Log.Info($"[ETBattleView] Battle end: {args.BattleId}, Victory={args.IsVictory}");
            await ETTask.CompletedTask;
        }
    }
}
