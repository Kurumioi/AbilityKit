using System;
using ET.AbilityKit.Demo.ET.Share;

namespace ET.Logic
{
    /// <summary>
    /// 战斗自动测试组件 System
    /// 处理所有自动测试业务逻辑
    ///
    /// 设计说明：
    /// - AutoTest 生成移动方向向量 (dx, dz)，不是目标坐标
    /// - 与 moba.view 的 BattleInputFeature 保持一致
    /// </summary>
    [EntitySystemOf(typeof(ETBattleAutoTestComponent))]
    [FriendOf(typeof(ETBattleAutoTestComponent))]
    [FriendOf(typeof(ETInputComponent))]
    public static partial class ETBattleAutoTestComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ETBattleAutoTestComponent self)
        {
        }

        [EntitySystem]
        private static void Update(this ETBattleAutoTestComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this ETBattleAutoTestComponent self)
        {
            Log.Info($"[ETBattleAutoTest] Destroyed. Total move commands: {self.MoveCommandCount}, Distance: {self.MoveDistance:F2}");
        }

        /// <summary>
        /// 初始化自动测试
        /// </summary>
        public static void Initialize(this ETBattleAutoTestComponent self, long actorId, string playerId, float startX, float startZ)
        {
            self.Initialize((int)actorId, playerId, startX, startZ);
            Log.Info($"[ETBattleAutoTest] Initialized for ActorId={actorId}, PlayerId={playerId}, StartPos=({startX}, {startZ})");
        }

        /// <summary>
        /// 每帧更新 - 发送自动移动命令
        /// 命令发送到下一帧 (frame + 1)，确保在下一个管线周期被 ProcessETInputPhase 正确处理
        ///
        /// 帧号流程:
        /// 1. AutoTest.OnUpdate(frame) 生成命令到 buffer[frame + 1]
        /// 2. PreTickPhase 递增 ctx.CurrentFrame++
        /// 3. ProcessETInputPhase 从 buffer[ctx.CurrentFrame] 读取，此时 buffer[frame + 1] 有数据
        /// </summary>
        public static void OnUpdate(this ETBattleAutoTestComponent self, int frame)
        {
            if (!self.IsEnabled)
                return;

            // 命令发送到下一帧，确保在管线处理时正确读取
            int targetFrame = frame + 1;
            if (frame % self.MoveIntervalFrames == 0)
            {
                Log.Debug($"[ETBattleAutoTest] Frame={frame}: Generating move command for PlayerId={self.TestPlayerId} to Frame={targetFrame}");
                SendMoveCommand(self, targetFrame);
            }
        }

        /// <summary>
        /// 发送移动命令 - 发送方向向量，不是目标坐标
        /// 与 moba.view BattleInputFeature 保持一致
        /// </summary>
        private static void SendMoveCommand(ETBattleAutoTestComponent self, int frame)
        {
            // 检查是否到达边界，如果是则反转方向
            CheckBoundaryAndFlipDirection(self);

            // 获取移动方向
            float dx = self.MoveDirX;
            float dz = self.MoveDirZ;

            // 获取输入组件并发送命令
            var inputComponent = self.Scene().GetComponent<ETInputComponent>();
            if (inputComponent != null)
            {
                // 发送方向向量（dx, dz）
                inputComponent.AddMoveCommand(frame, self.TestPlayerId, dx, dz);
                self.MoveCommandCount++;

                Log.Info($"[ETBattleAutoTest] Move command: Frame={frame}, PlayerId={self.TestPlayerId}, Dir=({dx:F2}, {dz:F2})");
            }
            else
            {
                Log.Warning("[ETBattleAutoTest] ETInputComponent not found!");
            }
        }

        /// <summary>
        /// 检查边界并翻转移动方向
        /// </summary>
        private static void CheckBoundaryAndFlipDirection(ETBattleAutoTestComponent self)
        {
            bool needFlip = false;
            float flipX = self.MoveDirX;
            float flipZ = self.MoveDirZ;

            // 检查 X 边界
            if (self.CurrentX <= self.MinX || self.CurrentX >= self.MaxX)
            {
                needFlip = true;
                flipX = -flipX;
            }

            // 检查 Z 边界
            if (self.CurrentY <= self.MinZ || self.CurrentY >= self.MaxZ)
            {
                needFlip = true;
                flipZ = -flipZ;
            }

            // 如果需要翻转方向
            if (needFlip)
            {
                // 归一化方向向量（如果需要）
                float len = (float)Math.Sqrt(flipX * flipX + flipZ * flipZ);
                if (len > 0.001f)
                {
                    flipX /= len;
                    flipZ /= len;
                }
                else
                {
                    // 默认方向：向 X 正方向
                    flipX = 1f;
                    flipZ = 0f;
                }

                self.MoveDirX = flipX;
                self.MoveDirZ = flipZ;
                Log.Info($"[ETBattleAutoTest] Direction flipped: Dir=({flipX:F2}, {flipZ:F2})");
            }
        }

        /// <summary>
        /// 启用自动测试
        /// </summary>
        public static void Enable(this ETBattleAutoTestComponent self)
        {
            self.IsEnabled = true;
            Log.Info("[ETBattleAutoTest] Auto test enabled");
        }

        /// <summary>
        /// 禁用自动测试
        /// </summary>
        public static void Disable(this ETBattleAutoTestComponent self)
        {
            self.IsEnabled = false;
            Log.Info("[ETBattleAutoTest] Auto test disabled");
        }

        /// <summary>
        /// 重置统计
        /// </summary>
        public static void ResetStats(this ETBattleAutoTestComponent self)
        {
            Log.Info($"[ETBattleAutoTest] Stats reset. Previous: Commands={self.MoveCommandCount}, Distance={self.MoveDistance:F2}");
            self.MoveCommandCount = 0;
            self.MoveDistance = 0f;
        }
    }
}
