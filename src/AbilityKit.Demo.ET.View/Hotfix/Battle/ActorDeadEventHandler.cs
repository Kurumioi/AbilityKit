using System;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.AbilityKit.Demo.ET.View
{
    /// <summary>
    /// ActorDeadEvent 事件处理器
    /// 订阅 ET.Logic 发布的事件，更新视图层数据
    /// </summary>
    [Event(SceneType.DemoBattle)]
    public class ActorDeadEventHandler : AEvent<Scene, ActorDeadEvent>
    {
        protected override async ETTask Run(Scene scene, ActorDeadEvent args)
        {
            var listener = scene.GetComponent<ETViewEventListener>();
            if (listener != null)
            {
                var view = listener.GetUnitView(args.ActorId);
                if (view != null)
                {
                    view.OnDead();
                    return;
                }
            }

            Log.Debug($"[ActorDeadEventHandler] View not found for MobaActorId={args.ActorId}");
        }
    }
}
