using ET.AbilityKit.Demo.ET.Share;

namespace ET.AbilityKit.Demo.ET.View
{
    /// <summary>
    /// 单位视图组件系统
    /// 负责管理单位视图组件的表现逻辑
    /// </summary>
    public static class ETUnitViewComponentSystem
    {
        /// <summary>
        /// 处理单位生成
        /// </summary>
        public static void OnActorSpawn(this ETUnitViewComponent self, ActorSpawnEvent evt)
        {
            self.UnitId = evt.ActorId;
            self.MobaActorId = evt.MobaActorId;
            self.Name = evt.Name;
            self.X = evt.X;
            self.Y = evt.Y;
            self.MaxHp = evt.MaxHp;
            self.CurrentHp = evt.MaxHp;
            self.EntityCode = evt.EntityCode;
            self.IsDead = false;
            self.IsVisible = true;

            Log.Info($"[ETUnitView] Actor spawned: {evt.Name} (ET={evt.ActorId}, Moba={evt.MobaActorId}) at ({evt.X}, {evt.Y})");
        }

        /// <summary>
        /// 处理单位死亡
        /// </summary>
        public static void OnActorDead(this ETUnitViewComponent self, ActorDeadEvent evt)
        {
            self.OnDead();
            Log.Info($"[ETUnitView] Actor dead: MobaActorId={evt.ActorId}, Killer: {evt.KillerId}");
        }

        /// <summary>
        /// 处理伤害事件
        /// </summary>
        public static void OnActorDamage(this ETUnitViewComponent self, ActorDamageEvent evt)
        {
            self.CurrentHp = evt.CurrentHp;
            self.ShowDamage(evt.Damage);

            if (self.CurrentHp <= 0)
            {
                self.OnDead();
            }

            Log.Info($"[ETUnitView] Actor damaged: MobaActorId={evt.ActorId}, Damage: {evt.Damage}, CurrentHP: {evt.CurrentHp}/{evt.MaxHp}");
        }

        /// <summary>
        /// 处理移动事件
        /// </summary>
        public static void OnActorMove(this ETUnitViewComponent self, ActorMoveEvent evt)
        {
            self.UpdatePosition(evt.X, evt.Y);
        }

        /// <summary>
        /// 处理属性变化
        /// </summary>
        public static void OnAttributeChange(this ETUnitViewComponent self, ActorAttributeChangeEvent evt)
        {
            var attributeName = evt.AttributeName;
            var oldValue = evt.OldValue;
            var newValue = evt.NewValue;

            switch (attributeName)
            {
                case "Hp":
                    var hpChange = newValue - oldValue;
                    if (hpChange < 0)
                    {
                        self.ShowDamage(Math.Abs(hpChange));
                    }
                    else if (hpChange > 0)
                    {
                        self.ShowHeal(hpChange);
                    }
                    self.CurrentHp = newValue;
                    break;

                case "MaxHp":
                    self.MaxHp = newValue;
                    break;

                case "Attack":
                    Log.Info($"[ETUnitView] {self.Name} attack changed from {oldValue} to {newValue}");
                    break;

                case "Defense":
                    Log.Info($"[ETUnitView] {self.Name} defense changed from {oldValue} to {newValue}");
                    break;

                case "MoveSpeed":
                    Log.Info($"[ETUnitView] {self.Name} move speed changed from {oldValue} to {newValue}");
                    break;

                default:
                    Log.Info($"[ETUnitView] {self.Name} {attributeName} changed from {oldValue} to {newValue}");
                    break;
            }
        }

        /// <summary>
        /// 处理特效播放
        /// </summary>
        public static void OnVfxSpawn(this ETUnitViewComponent self, VfxSpawnEvent evt)
        {
            self.PlayVfx(evt.VfxId, 1f);
        }

        /// <summary>
        /// 处理飘字事件
        /// </summary>
        public static void OnFloatingText(this ETUnitViewComponent self, FloatingTextEvent evt)
        {
            switch (evt.TextType)
            {
                case "damage":
                    Log.Info($"[ETUnitView] {self.Name} floating text: -{evt.Text}");
                    break;
                case "heal":
                    Log.Info($"[ETUnitView] {self.Name} floating text: +{evt.Text}");
                    break;
                case "miss":
                    Log.Info($"[ETUnitView] {self.Name} floating text: Miss");
                    break;
                default:
                    Log.Info($"[ETUnitView] {self.Name} floating text: {evt.Text}");
                    break;
            }
        }
    }
}
