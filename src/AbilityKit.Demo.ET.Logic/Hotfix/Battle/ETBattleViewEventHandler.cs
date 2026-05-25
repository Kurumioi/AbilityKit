using System;
using System.Threading.Tasks;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// Battle view event handler
    /// Subscribes to logic layer events and creates Logic layer units
    ///
    /// Note: View layer events (ActorMove, ActorDamage, ActorDead) are published
    /// to the ET event system for ET.View to handle. This handler only manages
    /// Logic layer unit creation.
    /// </summary>
    [Event(SceneType.DemoBattle)]
    public class ETBattleView_EventHandler: AEvent<Scene, ActorSpawnEvent>
    {
        protected override async ETTask Run(Scene scene, ActorSpawnEvent args)
        {
            // Create Logic layer unit
            var unitComponent = scene.GetComponent<ETUnitComponent>();
            if (unitComponent != null)
            {
                unitComponent.CreateUnit(
                    args.ActorId,
                    args.EntityCode,
                    args.Kind,
                    args.Name,
                    args.X,
                    args.Y,
                    args.MaxHp);
                Log.Info($"[ETBattleView] Logic unit created: {args.Name} ({args.ActorId})");
            }
            else
            {
                Log.Warning($"[ETBattleView] ETUnitComponent not found!");
            }

            await ETTask.CompletedTask;
        }
    }

        /// <summary>
        /// Unit move event handler - publishes to ET event system for ET.View
        /// </summary>
        [Event(SceneType.DemoBattle)]
        public class ETBattleView_ActorMove_Handler: AEvent<Scene, ActorMoveEvent>
        {
            protected override async ETTask Run(Scene scene, ActorMoveEvent args)
            {
                // Update Logic layer unit position directly
                var unitComponent = scene.GetComponent<ETUnitComponent>();
                if (unitComponent != null)
                {
                    var unit = unitComponent.GetUnit(args.ActorId);
                    if (unit != null)
                    {
                        unit.X = args.X;
                        unit.Y = args.Y;
                    }
                }

                // Publish to ET event system for ET.View to handle rendering
                EventSystem.Instance.Publish(scene, args);
                await ETTask.CompletedTask;
            }
        }

    /// <summary>
    /// Unit damage event handler - publishes to ET event system for ET.View
    /// </summary>
    [Event(SceneType.DemoBattle)]
    public class ETBattleView_ActorDamage_Handler: AEvent<Scene, ActorDamageEvent>
    {
        protected override async ETTask Run(Scene scene, ActorDamageEvent args)
        {
            // Publish to ET event system for ET.View to handle rendering
            EventSystem.Instance.Publish(scene, args);
            await ETTask.CompletedTask;
        }
    }

    /// <summary>
    /// Unit dead event handler - publishes to ET event system for ET.View
    /// </summary>
    [Event(SceneType.DemoBattle)]
    public class ETBattleView_ActorDead_Handler: AEvent<Scene, ActorDeadEvent>
    {
        protected override async ETTask Run(Scene scene, ActorDeadEvent args)
        {
            // Publish to ET event system for ET.View to handle rendering
            EventSystem.Instance.Publish(scene, args);
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
