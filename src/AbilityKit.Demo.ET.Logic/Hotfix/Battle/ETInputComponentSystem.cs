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
        public static void SubmitMoveInput(this ETInputComponent self, int frame, long actorId, float dx, float dz)
        {
            // 将 ActorId 转换为 PlayerId 字符串
            string playerId = actorId.ToString();
            self.AddMoveCommand(frame, playerId, dx, dz);
            Log.Debug($"[ETInput] Move input: Actor {actorId} Dir=({dx}, {dz}) at frame {frame}");
        }

        /// <summary>
        /// 提交技能输入
        /// </summary>
        public static void SubmitSkillInput(this ETInputComponent self, int frame, long actorId, int skillSlot, float targetX, float targetY)
        {
            // 将 ActorId 转换为 PlayerId 字符串
            string playerId = actorId.ToString();
            self.AddSkillCommand(frame, playerId, skillSlot, targetX, targetY);
            Log.Debug($"[ETInput] Skill input: Actor {actorId} Skill {skillSlot} -> ({targetX}, {targetY}) at frame {frame}");
        }

        /// <summary>
        /// 提交停止输入
        /// </summary>
        public static void SubmitStopInput(this ETInputComponent self, int frame, long actorId)
        {
            // 将 ActorId 转换为 PlayerId 字符串
            string playerId = actorId.ToString();
            self.AddStopCommand(frame, playerId);
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
        /// 处理移动命令 - 转发到 BattleDriver
        ///
        /// 说明：移动命令是方向向量 (dx, dz)，转发给 moba.core 处理
        /// </summary>
        private static void ProcessMoveCommand(this ETInputComponent self, ETUnitComponent unitComponent, MoveCommand cmd)
        {
            // 将 PlayerId 字符串转换回 ActorId 用于单位查找
            int actorId = DeterministicHash.StringToActorId(cmd.PlayerId);
            var unit = unitComponent.GetUnit(actorId);
            if (unit == null || unit.IsDead)
            {
                Log.Warning($"[ETInput] Move: Unit not found or dead, PlayerId={cmd.PlayerId}, ActorId={actorId}");
                return;
            }

            // 转发到 BattleDriver（通过 BattleComponent）
            var scene = self.Scene();
            var battleComponent = scene?.GetComponent<ETBattleComponent>();
            if (battleComponent != null)
            {
                ETBattleDriverBridge.SubmitMoveInput(battleComponent, actorId, cmd.Dx, cmd.Dz);
                Log.Info($"[ETInput] Move forwarded to Driver: PlayerId={cmd.PlayerId}, ActorId={actorId}, Dir=({cmd.Dx}, {cmd.Dz})");
            }
            else
            {
                Log.Warning("[ETInput] BattleComponent not found, cannot forward move!");
            }
        }

        /// <summary>
        /// 处理技能命令 - 转发到 Driver
        ///
        /// 说明：不做任何技能释放逻辑，只转发命令到 moba.core
        /// </summary>
        private static void ProcessSkillCommand(this ETInputComponent self, ETUnitComponent unitComponent, SkillCommand cmd)
        {
            // 将 PlayerId 字符串转换回 ActorId 用于单位查找
            int actorId = DeterministicHash.StringToActorId(cmd.PlayerId);
            var unit = unitComponent.GetUnit(actorId);
            if (unit == null || unit.IsDead)
            {
                return;
            }

            // 设置技能目标（用于渲染）
            self.CurrentSkillSlot = cmd.SkillSlot;
            self.SkillTargetX = cmd.TargetX;
            self.SkillTargetY = cmd.TargetY;

            Log.Debug($"[ETInput] Skill command forwarded: PlayerId={cmd.PlayerId}, ActorId={actorId}, Slot={cmd.SkillSlot}");

            // 技能释放逻辑由 moba.core 处理（通过快照更新）
        }

        /// <summary>
        /// 处理停止命令
        /// </summary>
        private static void ProcessStopCommand(this ETInputComponent self, ETUnitComponent unitComponent, StopCommand cmd)
        {
            // 将 PlayerId 字符串转换回 ActorId 用于单位查找
            int actorId = DeterministicHash.StringToActorId(cmd.PlayerId);
            var unit = unitComponent.GetUnit(actorId);
            if (unit == null)
                return;

            // 清除移动方向
            self.LastMoveDx = 0;
            self.LastMoveDz = 0;

            Log.Debug($"[ETInput] Stop command: PlayerId={cmd.PlayerId}, ActorId={actorId}");
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
