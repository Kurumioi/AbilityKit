using System;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// Input component System
    /// 对应 Moba.Console ConsoleInputFeature
    ///
    /// 设计说明：
    /// - 作为状态同步客户端，只负责输入采集和转发
    /// - 不做任何游戏逻辑处理
    /// - 所有业务逻辑由 moba.core 处理
    /// </summary>
    [EntitySystemOf(typeof(ETInputComponent))]
    [FriendOf(typeof(ETInputComponent))]
    [FriendOf(typeof(ETUnitComponent))]
    [FriendOf(typeof(ETUnit))]
    public static partial class ETInputComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ETInputComponent self)
        {
            Log.Info("[ETInput] ETInputComponent awake");
        }

        #region Input Submission

        /// <summary>
        /// 提交移动输入 - 发送方向向量 (dx, dz)
        /// </summary>
        public static void SubmitMoveInput(this ETInputComponent self, int frame, string playerId, float dx, float dz)
        {
            self.AddMoveCommand(frame, playerId, dx, dz);
            Log.Debug($"[ETInput] Move input: PlayerId={playerId} Dir=({dx}, {dz}) at frame {frame}");
        }

        /// <summary>
        /// 提交技能输入
        /// </summary>
        public static void SubmitSkillInput(this ETInputComponent self, int frame, string playerId, int skillSlot, float targetX, float targetY)
        {
            self.AddSkillCommand(frame, playerId, skillSlot, targetX, targetY);
            Log.Debug($"[ETInput] Skill input: PlayerId={playerId} Skill {skillSlot} -> ({targetX}, {targetY}) at frame {frame}");
        }

        /// <summary>
        /// 提交停止输入
        /// </summary>
        public static void SubmitStopInput(this ETInputComponent self, int frame, string playerId)
        {
            self.AddStopCommand(frame, playerId);
            Log.Debug($"[ETInput] Stop input: PlayerId={playerId} at frame {frame}");
        }

        #endregion

        #region ❌ 已删除的业务逻辑

        // ❌ 技能冷却检查 - 由 moba.core 处理
        // ❌ 范围查询 - 由 moba.core 处理
        // ❌ 伤害计算 - 由 moba.core 处理
        // ❌ 冷却设置 - 由 moba.core 处理

        #endregion
    }
}
