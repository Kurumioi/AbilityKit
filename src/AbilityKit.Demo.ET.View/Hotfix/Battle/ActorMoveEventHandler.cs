using System;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.AbilityKit.Demo.ET.View
{
    /// <summary>
    /// ActorMoveEvent 事件处理器
    /// 订阅 ET.Logic 发布的事件，更新视图层数据
    /// </summary>
    [Event(SceneType.DemoBattle)]
    public class ActorMoveEventHandler : AEvent<Scene, ActorMoveEvent>
    {
        protected override async ETTask Run(Scene scene, ActorMoveEvent args)
        {
            var listener = scene.GetComponent<ETViewEventListener>();
            if (listener != null)
            {
                var view = listener.GetUnitView(args.ActorId);
                if (view != null)
                {
                    view.UpdatePosition(args.X, args.Y);
                    return;
                }
            }

            Log.Debug($"[ActorMoveEventHandler] View not found for MobaActorId={args.ActorId}");
        }
    }
}
