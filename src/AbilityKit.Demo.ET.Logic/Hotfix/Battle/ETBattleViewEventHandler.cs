using System;
using System.Threading.Tasks;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// Battle view event handler
    /// Subscribes to logic layer events and creates Logic layer units
    ///
    /// Design:
    /// - These handlers receive events that were published by ETBattleViewEventSink
    /// - Handlers should ONLY update Logic layer data, NOT re-publish events
    /// - View layer receives events directly from ETBattleViewEventSink
    /// - Re-publishing events would cause View layer to receive duplicates
    /// </summary>
    [Event(SceneType.DemoBattle)]
    public class ETBattleView_EventHandler: AEvent<Scene, ActorSpawnEvent>
    {
        protected override async ETTask Run(Scene scene, ActorSpawnEvent args)
        {
            // Note: ETUnit creation is handled by TriggerEnterGameSnapshot in ETMobaBattleDriver
            // This handler only handles view-layer concerns, not Logic layer unit creation
            // ActorSpawnEvent is now only used for view rendering (ETUnitViewComponent)
            Log.Debug($"[ETBattleView] ActorSpawnEvent received: {args.Name} (ActorId={args.ActorId}) - view layer rendering handled elsewhere");

            await ETTask.CompletedTask;
        }
    }

    /// <summary>
    /// Unit move event handler
    /// Updates Logic layer unit position (DO NOT re-publish - View receives from Sink directly)
    /// </summary>
    [Event(SceneType.DemoBattle)]
    public class ETBattleView_ActorMove_Handler: AEvent<Scene, ActorMoveEvent>
    {
        private static int _loggedMoveCount;

        protected override async ETTask Run(Scene scene, ActorMoveEvent args)
        {
            // Update Logic layer unit position only
            // DO NOT re-publish - View layer receives ActorMoveEvent from ETBattleViewEventSink directly
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
    /// Unit damage event handler
    /// DO NOT re-publish - View receives from Sink directly
    /// </summary>
    [Event(SceneType.DemoBattle)]
    public class ETBattleView_ActorDamage_Handler: AEvent<Scene, ActorDamageEvent>
    {
        protected override async ETTask Run(Scene scene, ActorDamageEvent args)
        {
            // Update Logic layer unit HP
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
            // DO NOT re-publish - View layer receives ActorDamageEvent from ETBattleViewEventSink directly
            await ETTask.CompletedTask;
        }
    }

    /// <summary>
    /// Unit dead event handler
    /// DO NOT re-publish - View receives from Sink directly
    /// </summary>
    [Event(SceneType.DemoBattle)]
    public class ETBattleView_ActorDead_Handler: AEvent<Scene, ActorDeadEvent>
    {
        protected override async ETTask Run(Scene scene, ActorDeadEvent args)
        {
            // Update Logic layer unit state
            var unitComponent = scene.GetComponent<ETUnitComponent>();
            if (unitComponent != null)
            {
                var unit = unitComponent.GetUnit(args.ActorId);
                if (unit != null)
                {
                    unit.Hp = 0;
                }
            }
            // DO NOT re-publish - View layer receives ActorDeadEvent from ETBattleViewEventSink directly
            await ETTask.CompletedTask;
        }
    }

    /// <summary>
    /// Battle start event handler
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
    /// Battle end event handler
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
