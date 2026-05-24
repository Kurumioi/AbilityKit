namespace ET.Logic
{
    /// <summary>
    /// 单位变换组件
    /// 存储实体的位置、朝向和移动目标
    /// </summary>
    public class ETUnitTransformComponent : Entity, IAwake
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Rotation { get; set; }

        /// <summary>
        /// 移动目标位置
        /// </summary>
        public float TargetX { get; set; }
        public float TargetY { get; set; }

        /// <summary>
        /// 是否正在移动
        /// </summary>
        public bool IsMoving => System.Math.Abs(TargetX) > 0.001f || System.Math.Abs(TargetY) > 0.001f;

        public void Awake()
        {
        }
    }
}
