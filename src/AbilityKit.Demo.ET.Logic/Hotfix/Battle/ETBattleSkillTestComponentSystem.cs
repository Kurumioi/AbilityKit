using System;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// ETBattleSkillTestComponent System
    /// 处理所有技能测试业务逻辑
    /// </summary>
    [EntitySystemOf(typeof(ETBattleSkillTestComponent))]
    [FriendOf(typeof(ETBattleSkillTestComponent))]
    [FriendOf(typeof(ETBattleComponent))]
    [FriendOf(typeof(ETInputComponent))]
    [FriendOf(typeof(ETUnitComponent))]
    public static partial class ETBattleSkillTestComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ETBattleSkillTestComponent self)
        {
        }

        [EntitySystem]
        private static void Update(this ETBattleSkillTestComponent self)
        {
            if (!self.IsEnabled)
            {
                return;
            }

            var battleComponent = self.Scene().GetComponent<ETBattleComponent>();
            if (battleComponent == null || battleComponent.State != BattleState.InProgress)
            {
                return;
            }

            var currentFrame = battleComponent.BattleDriver?.CurrentFrame ?? 0;
            self.OnUpdate(currentFrame);
        }

        [EntitySystem]
        private static void Destroy(this ETBattleSkillTestComponent self)
        {
            Log.Info($"[ETBattleSkillTest] Destroyed. Total skill casts: {self.SkillCastCount}");
        }

        /// <summary>
        /// 初始化技能测试
        /// </summary>
        public static void Initialize(this ETBattleSkillTestComponent self, int actorId, string playerId, int skillSlot = 0)
        {
            self.Initialize(actorId, playerId, skillSlot);
            Log.Info($"[ETBattleSkillTest] Initialized for ActorId={actorId}, PlayerId={playerId}, SkillSlot={skillSlot}");
        }

        /// <summary>
        /// 每帧更新 - 检查是否需要释放技能
        /// 注意：命令发送到下一帧 (frame + 1)，这是帧同步系统的标准做法
        /// </summary>
        public static void OnUpdate(this ETBattleSkillTestComponent self, int frame)
        {
            if (!self.IsEnabled)
                return;

            // 每隔指定帧数释放技能
            // 命令发送到下一帧，确保在下一个管线周期被处理
            int targetFrame = frame + 1;
            if (frame - self.LastCastFrame >= self.SkillIntervalFrames)
            {
                CastSkill(self, targetFrame);
                self.LastCastFrame = frame;
            }
        }

        /// <summary>
        /// 释放技能
        /// </summary>
        private static void CastSkill(ETBattleSkillTestComponent self, int targetFrame)
        {
            var inputComponent = self.Scene().GetComponent<ETInputComponent>();
            if (inputComponent == null)
            {
                Log.Warning("[ETBattleSkillTest] ETInputComponent not found!");
                return;
            }

            // 获取单位位置作为技能目标
            float targetX = 0f;
            float targetY = 0f;

            var unitComponent = self.Scene().GetComponent<ETUnitComponent>();
            if (unitComponent != null)
            {
                var unit = ETUnitComponentSystem.GetUnit(unitComponent, self.TestActorId);
                if (unit != null)
                {
                    targetX = unit.X + 5f; // 在单位前方释放
                    targetY = unit.Y;
                }
            }

            // 添加技能命令
            inputComponent.AddSkillCommand(targetFrame, self.TestPlayerId, self.SkillSlot, targetX, targetY);
            self.SkillCastCount++;

            Log.Debug($"[ETBattleSkillTest] Skill cast: Frame={targetFrame}, PlayerId={self.TestPlayerId}, Slot={self.SkillSlot}, Target=({targetX:F2}, {targetY:F2})");
        }

        /// <summary>
        /// 启用技能测试
        /// </summary>
        public static void Enable(this ETBattleSkillTestComponent self)
        {
            self.IsEnabled = true;
            Log.Info("[ETBattleSkillTest] Skill test enabled");
        }

        /// <summary>
        /// 禁用技能测试
        /// </summary>
        public static void Disable(this ETBattleSkillTestComponent self)
        {
            self.IsEnabled = false;
            Log.Info("[ETBattleSkillTest] Skill test disabled");
        }
    }
}
