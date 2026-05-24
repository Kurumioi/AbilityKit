namespace ET.Logic
{
    /// <summary>
    /// ETUnitCharacterComponent System
    /// 管理单位角色属性
    ///
    /// Design:
    /// - HP 等属性由 moba.core 快照同步而来
    /// - 只提供只读访问和快照同步设置
    /// - 不实现客户端伤害计算逻辑
    /// </summary>
    [EntitySystemOf(typeof(ETUnitCharacterComponent))]
    [FriendOf(typeof(ETUnitCharacterComponent))]
    [FriendOf(typeof(ETUnit))]
    public static partial class ETUnitCharacterComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ETUnitCharacterComponent self)
        {
        }

        /// <summary>
        /// 初始化角色属性
        /// </summary>
        public static void Initialize(
            this ETUnitCharacterComponent self,
            float maxHp,
            float attack,
            float defense,
            float moveSpeed)
        {
            self.MaxHp = maxHp;
            self.Hp = maxHp;
            self.Attack = attack;
            self.Defense = defense;
            self.MoveSpeed = moveSpeed;
        }

        /// <summary>
        /// 设置 HP（从快照同步）
        /// </summary>
        public static void SetHp(this ETUnitCharacterComponent self, float hp)
        {
            self.Hp = hp;
        }

        /// <summary>
        /// 获取 HP 百分比
        /// </summary>
        public static float GetHpPercent(this ETUnitCharacterComponent self)
        {
            if (self.MaxHp <= 0)
                return 0;
            return self.Hp / self.MaxHp;
        }

        /// <summary>
        /// 检查是否死亡
        /// </summary>
        public static bool IsDead(this ETUnitCharacterComponent self)
        {
            return self.Hp <= 0;
        }

        /// <summary>
        /// 获取拥有者单位
        /// </summary>
        public static ETUnit? GetOwner(this ETUnitCharacterComponent self)
        {
            return self.Parent as ETUnit;
        }
    }
}
