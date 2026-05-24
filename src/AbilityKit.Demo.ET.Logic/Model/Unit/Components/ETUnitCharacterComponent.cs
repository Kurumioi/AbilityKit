namespace ET.Logic
{
    /// <summary>
    /// 单位角色属性组件
    /// 存储实体的战斗属性
    /// </summary>
    public class ETUnitCharacterComponent : Entity, IAwake
    {
        public float Hp { get; set; } = 100f;
        public float MaxHp { get; set; } = 100f;
        public float Attack { get; set; } = 10f;
        public float Defense { get; set; } = 5f;
        public float MoveSpeed { get; set; } = 5f;

        /// <summary>
        /// 是否死亡
        /// </summary>
        public bool IsDead => Hp <= 0;

        public void Awake()
        {
        }
    }
}
