using System;
using AbilityKit.Ability.Share.Impl.Moba.Struct;
using AbilityKit.Core.Math;

namespace AbilityKit.Demo.Moba.Console.Bootstrap
{
    /// <summary>
    /// Console 技能执行器接口
    /// 从配置数据库读取技能配置并执行
    /// </summary>
    public interface IConsoleSkillExecutor
    {
        /// <summary>
        /// 按槽位释放技能
        /// </summary>
        bool CastBySlot(int actorId, int slot);

        /// <summary>
        /// 按槽位释放技能（带瞄准信息）
        /// 返回执行结果，由调用者处理事件分发
        /// </summary>
        SkillCastResult CastBySlot(int actorId, int slot, Vec3 aimPos, Vec3 aimDir);

        /// <summary>
        /// 计算最终伤害（由 Simulation 层调用）
        /// </summary>
        DamageExecuteResult CalculateDamage(int casterId, int targetId, int skillId,
            float baseDamage, float targetCurrentHp, float targetMaxHp);

        /// <summary>
        /// 帧推进
        /// </summary>
        void Step(float deltaTime);

        /// <summary>
        /// 初始化执行器
        /// </summary>
        void Initialize();
    }
}
