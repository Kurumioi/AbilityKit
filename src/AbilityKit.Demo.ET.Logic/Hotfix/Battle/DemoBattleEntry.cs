using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// 视图事件 Sink 实现
    /// 将 AbilityKit 事件桥接到 ET 事件系统，由 ET.View 处理渲染
    /// </summary>
    public class ETViewEventSink: IETViewEventSink
    {
        private readonly Scene _scene;

        public ETViewEventSink(Scene scene)
        {
            _scene = scene;
        }

        public void OnActorSpawn(ActorSpawnEvent evt)
        {
            EventSystem.Instance.Publish(_scene, evt);
        }

        public void OnActorDead(ActorDeadEvent evt)
        {
            EventSystem.Instance.Publish(_scene, evt);
        }

        public void OnActorMove(ActorMoveEvent evt)
        {
            EventSystem.Instance.Publish(_scene, evt);
        }

        public void OnActorDamage(ActorDamageEvent evt)
        {
            EventSystem.Instance.Publish(_scene, evt);
        }

        public void OnActorAttributeChange(ActorAttributeChangeEvent evt) => _ = evt;

        public void OnSkillCast(SkillCastEvent evt) => _ = evt;

        public void OnSkillHit(SkillHitEvent evt) => _ = evt;

        public void OnVfxSpawn(VfxSpawnEvent evt) => _ = evt;

        public void OnFloatingText(FloatingTextEvent evt) => _ = evt;

        public void OnBattleStart(BattleStartEvent evt)
        {
            EventSystem.Instance.Publish(_scene, evt);
        }

        public void OnBattleEnd(BattleEndEvent evt)
        {
            EventSystem.Instance.Publish(_scene, evt);
        }

        public void OnFrameTick(FrameTickEvent evt) => _ = evt;

        public void OnFrameSyncComplete(int frame) => _ = frame;
    }
}
