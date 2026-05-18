using System;
using AbilityKit.Ability.Share.Impl.Moba.Struct;
using AbilityKit.Core.Math;

namespace AbilityKit.Demo.Moba.Console.Bootstrap
{
    /// <summary>
    /// Console 技能执行器接口
    /// 职责：接收技能释放输入，转发给逻辑层
    /// </summary>
    public interface IConsoleSkillExecutor
    {
        /// <summary>
        /// 按槽位释放技能
        /// </summary>
        bool CastBySlot(int actorId, int slot);

        /// <summary>
        /// 按槽位释放技能（带瞄准信息）
        /// 返回执行结果，包含技能 ID 和目标 ID
        /// </summary>
        SkillCastResult CastBySlot(int actorId, int slot, Vec3 aimPos, Vec3 aimDir);

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
