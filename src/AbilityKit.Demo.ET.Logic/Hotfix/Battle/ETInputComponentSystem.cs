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
        /// 提交移动输入
        /// </summary>
        public static void SubmitMoveInput(this ETInputComponent self, int frame, long actorId, float x, float y)
        {
            self.AddMoveCommand(frame, (int)actorId, x, y);
            Log.Debug($"[ETInput] Move input: Actor {actorId} -> ({x}, {y}) at frame {frame}");
        }

        /// <summary>
        /// 提交技能输入
        /// </summary>
        public static void SubmitSkillInput(this ETInputComponent self, int frame, long actorId, int skillSlot, float targetX, float targetY)
        {
            self.AddSkillCommand(frame, (int)actorId, skillSlot, targetX, targetY);
            Log.Debug($"[ETInput] Skill input: Actor {actorId} Skill {skillSlot} -> ({targetX}, {targetY}) at frame {frame}");
        }

        /// <summary>
        /// 提交停止输入
        /// </summary>
        public static void SubmitStopInput(this ETInputComponent self, int frame, long actorId)
        {
            self.AddStopCommand(frame, (int)actorId);
            Log.Debug($"[ETInput] Stop input: Actor {actorId} at frame {frame}");
        }

        #endregion

        #region Input Processing

        /// <summary>
        /// 处理输入 - 转发到 Driver
        ///
        /// 设计说明：
        /// - 只负责从缓冲读取输入并转发
        /// - 不做任何业务逻辑处理（伤害、Buff 等）
        /// - 业务逻辑由 moba.core 处理
        /// </summary>
        public static void ProcessInput(this ETInputComponent self, int currentFrame)
        {
            var commands = self.GetInputsForFrame(currentFrame);
            if (commands == null)
            {
                return;
            }

            var unitComponent = self.Scene().GetComponent<ETUnitComponent>();
            if (unitComponent == null)
            {
                Log.Warning("[ETInput] ETUnitComponent not found!");
                return;
            }

            foreach (var cmd in commands)
            {
                switch (cmd)
                {
                    case MoveCommand move:
                        ProcessMoveCommand(self, unitComponent, move);
                        break;
                    case SkillCommand skill:
                        ProcessSkillCommand(self, unitComponent, skill);
                        break;
                    case StopCommand stop:
                        ProcessStopCommand(self, unitComponent, stop);
                        break;
                }
            }

            // Clear processed inputs
            self.ClearProcessedInputs(currentFrame);
        }

        /// <summary>
        /// 处理移动命令 - 设置目标位置
        ///
        /// 说明：设置 TargetX/Y 用于渲染插值，实际移动由 moba.core 计算
        /// </summary>
        private static void ProcessMoveCommand(this ETInputComponent self, ETUnitComponent unitComponent, MoveCommand cmd)
        {
            var unit = unitComponent.GetUnit(cmd.ActorId);
            if (unit == null || unit.IsDead)
            {
                return;
            }

            // 设置目标位置（用于渲染插值参考）
            // 实际位置由快照更新，这里只设置渲染目标
            unit.TargetX = cmd.X;
            unit.TargetY = cmd.Y;

            Log.Debug($"[ETInput] Move command forwarded: Actor {cmd.ActorId} -> ({cmd.X}, {cmd.Y})");
        }

        /// <summary>
        /// 处理技能命令 - 转发到 Driver
        ///
        /// 说明：不做任何技能释放逻辑，只转发命令到 moba.core
        /// </summary>
        private static void ProcessSkillCommand(this ETInputComponent self, ETUnitComponent unitComponent, SkillCommand cmd)
        {
            var unit = unitComponent.GetUnit(cmd.ActorId);
            if (unit == null || unit.IsDead)
            {
                return;
            }

            // 设置技能目标（用于渲染）
            self.CurrentSkillSlot = cmd.SkillSlot;
            self.SkillTargetX = cmd.TargetX;
            self.SkillTargetY = cmd.TargetY;

            Log.Debug($"[ETInput] Skill command forwarded: Actor {cmd.ActorId} Slot={cmd.SkillSlot}");

            // 技能释放逻辑由 moba.core 处理（通过快照更新）
            // 这里只设置渲染状态，不执行任何业务逻辑
        }

        /// <summary>
        /// 处理停止命令
        /// </summary>
        private static void ProcessStopCommand(this ETInputComponent self, ETUnitComponent unitComponent, StopCommand cmd)
        {
            var unit = unitComponent.GetUnit(cmd.ActorId);
            if (unit == null)
                return;

            // 清除移动目标
            unit.TargetX = 0;
            unit.TargetY = 0;

            Log.Debug($"[ETInput] Stop command: Actor {cmd.ActorId}");
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
