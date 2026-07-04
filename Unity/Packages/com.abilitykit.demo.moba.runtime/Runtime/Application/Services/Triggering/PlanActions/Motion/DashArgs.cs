using AbilityKit.Demo.Moba.Services.Motion;

namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    /// <summary>
    /// dash Action 的强类型参数。
    /// </summary>
    public readonly struct DashArgs
    {
        /// <summary>
        /// 冲刺速度，单位为距离/秒。
        /// </summary>
        public readonly float Speed;

        /// <summary>
        /// 冲刺持续时间，单位毫秒。
        /// </summary>
        public readonly float DurationMs;

        /// <summary>
        /// 冲刺方向模式：0=朝技能瞄准方向，1=朝目标方向，2=保持当前朝向。
        /// </summary>
        public readonly int DirectionMode;

        /// <summary>
        /// 优先级；高优先级会打断低优先级运动。
        /// </summary>
        public readonly int Priority;

        /// <summary>
        /// 是否应用到释放者，默认 caster。
        /// </summary>
        public readonly bool ApplyToCaster;

        /// <summary>
        /// 冲刺碰撞到角色后执行的触发计划 ID。
        /// </summary>
        public readonly int HitTriggerPlanId;

        /// <summary>
        /// 位移组 ID；0 表示使用行为默认组。
        /// </summary>
        public readonly int MotionGroupId;

        public readonly MobaMotionContinuousSettings Continuous;

        public DashArgs(float speed, float durationMs, int directionMode = 0, int priority = 10, bool applyToCaster = true, int hitTriggerPlanId = 0, int motionGroupId = 0, MobaMotionContinuousSettings continuous = default)
        {
            Speed = speed;
            DurationMs = durationMs;
            DirectionMode = directionMode;
            Priority = priority;
            ApplyToCaster = applyToCaster;
            HitTriggerPlanId = hitTriggerPlanId;
            MotionGroupId = motionGroupId;
            Continuous = continuous;
        }

        public static DashArgs Default => new DashArgs(0f, 0f, 0, 10, true, 0, 0, MobaMotionContinuousSettings.Empty);
    }
}
