using System;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.AbilityKit.Demo.ET.View
{
    /// <summary>
    /// ActorDamageEvent 事件处理器
    /// 订阅 ET.Logic 发布的事件，更新视图层数据
    /// </summary>
    [Event(SceneType.DemoBattle)]
    public class ActorDamageEventHandler : AEvent<Scene, ActorDamageEvent>
    {
        protected override async ETTask Run(Scene scene, ActorDamageEvent args)
        {
            var listener = scene.GetComponent<ETViewEventListener>();
            if (listener != null)
            {
                var view = listener.GetUnitView(args.ActorId);
                if (view != null)
                {
                    view.UpdateHp(args.CurrentHp, args.MaxHp);
                    view.ShowDamage(args.Damage);
                    return;
                }
            }

            Log.Debug($"[ActorDamageEventHandler] View not found for MobaActorId={args.ActorId}");
        }
    }
}
