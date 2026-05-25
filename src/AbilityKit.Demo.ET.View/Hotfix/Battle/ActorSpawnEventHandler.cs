using System;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.AbilityKit.Demo.ET.View
{
    /// <summary>
    /// ActorSpawnEvent 事件处理器
    /// 订阅 ET.Logic 发布的事件，创建视图层单位
    /// </summary>
    [Event(SceneType.DemoBattle)]
    public class ActorSpawnEventHandler : AEvent<Scene, ActorSpawnEvent>
    {
        protected override async ETTask Run(Scene scene, ActorSpawnEvent args)
        {
            // Get or create view event listener
            var listener = scene.GetComponent<ETViewEventListener>();
            if (listener == null)
            {
                listener = scene.AddComponent<ETViewEventListener>();
            }

            // Create unit view
            var view = new ETUnitViewComponent
            {
                UnitId = args.ActorId,
                MobaActorId = args.MobaActorId,
                Name = args.Name,
                X = args.X,
                Y = args.Y,
                CurrentHp = args.MaxHp,
                MaxHp = args.MaxHp,
                EntityCode = args.EntityCode,
                IsDead = false,
                IsVisible = true
            };

            // Add to listener
            listener.AddUnitView(args.MobaActorId, view);

            Log.Info($"[ActorSpawnEventHandler] Unit spawned in view: {args.Name} (MobaActorId={args.MobaActorId}) at ({args.X}, {args.Y})");
        }
    }
}
