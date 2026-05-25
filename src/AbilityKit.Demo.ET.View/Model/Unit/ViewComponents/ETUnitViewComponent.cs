namespace ET.AbilityKit.Demo.ET.View
{
    /// <summary>
    /// 单位视图组件
    /// 负责渲染单个单位的表现
    /// </summary>
    public class ETUnitViewComponent
    {
        /// <summary>
        /// ET 实体 ID
        /// </summary>
        public long UnitId { get; set; }

        /// <summary>
        /// 逻辑层（MobaCore）的 ActorId
        /// 用于与逻辑层交互
        /// </summary>
        public int MobaActorId { get; set; }

        /// <summary>
        /// 单位名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// X 坐标
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// Y 坐标
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// 当前 HP
        /// </summary>
        public float CurrentHp { get; set; }

        /// <summary>
        /// 最大 HP
        /// </summary>
        public float MaxHp { get; set; }

        /// <summary>
        /// HP 条宽度
        /// </summary>
        public float HpBarWidth { get; set; } = 50f;

        /// <summary>
        /// 是否可见
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// 是否死亡
        /// </summary>
        public bool IsDead { get; set; } = false;

        /// <summary>
        /// 旋转角度
        /// </summary>
        public float Rotation { get; set; }

        /// <summary>
        /// 实体代码
        /// </summary>
        public int EntityCode { get; set; }

        /// <summary>
        /// 更新位置
        /// </summary>
        public void UpdatePosition(float x, float y)
        {
            X = x;
            Y = y;
            Log.Info($"[ETUnitView] Unit {Name} ({UnitId}) position updated to ({X}, {Y})");
        }

        /// <summary>
        /// 更新旋转
        /// </summary>
        public void UpdateRotation(float rotation)
        {
            Rotation = rotation;
            Log.Info($"[ETUnitView] Unit {Name} ({UnitId}) rotation updated to {Rotation}");
        }

        /// <summary>
        /// 更新 HP
        /// </summary>
        public void UpdateHp(float currentHp, float maxHp)
        {
            CurrentHp = currentHp;
            MaxHp = maxHp;
            Log.Info($"[ETUnitView] Unit {Name} ({UnitId}) HP updated to {CurrentHp}/{MaxHp}");
        }

        /// <summary>
        /// 单位死亡
        /// </summary>
        public void OnDead()
        {
            IsDead = true;
            IsVisible = false;
            Log.Info($"[ETUnitView] Unit {Name} ({UnitId}) is dead, hiding view");
        }

        /// <summary>
        /// 单位复活
        /// </summary>
        public void OnRevive()
        {
            IsDead = false;
            IsVisible = true;
            Log.Info($"[ETUnitView] Unit {Name} ({UnitId}) revived");
        }

        /// <summary>
        /// 显示伤害飘字
        /// </summary>
        public void ShowDamage(float damage)
        {
            Log.Info($"[ETUnitView] Unit {Name} ({UnitId}) shows damage: -{damage}");
        }

        /// <summary>
        /// 显示治疗飘字
        /// </summary>
        public void ShowHeal(float healAmount)
        {
            Log.Info($"[ETUnitView] Unit {Name} ({UnitId}) shows heal: +{healAmount}");
        }

        /// <summary>
        /// 播放特效
        /// </summary>
        public void PlayVfx(string vfxId, float duration)
        {
            Log.Info($"[ETUnitView] Unit {Name} ({UnitId}) plays VFX: {vfxId} for {duration}s");
        }

        /// <summary>
        /// 播放音效
        /// </summary>
        public void PlaySfx(string sfxId)
        {
            Log.Info($"[ETUnitView] Unit {Name} ({UnitId}) plays SFX: {sfxId}");
        }
    }
}
