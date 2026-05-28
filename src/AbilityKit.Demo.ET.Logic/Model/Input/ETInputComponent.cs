using System;
using System.Collections.Generic;

namespace ET.Logic
{
    /// <summary>
    /// 输入组件 - 管理输入缓冲
    /// 与 Moba.Console 的 ConsoleInputFeature 类似
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETInputComponent: Entity, IAwake
    {
        // 输入缓冲 (帧号 -> 命令列表)
        private readonly Dictionary<int, List<object>> _inputBuffer = new Dictionary<int, List<object>>();

        // 当前移动方向（用于 Stop 命令检测）
        public float LastMoveDx { get; set; }
        public float LastMoveDz { get; set; }
        public int CurrentSkillSlot { get; set; } = -1;
        public float SkillTargetX { get; set; }
        public float SkillTargetY { get; set; }

        public void Awake()
        {
        }

        /// <summary>
        /// 添加移动命令
        /// 发送移动方向向量 (dx, dz)，不是目标坐标
        /// </summary>
        public void AddMoveCommand(int frame, string playerId, float dx, float dz)
        {
            if (!_inputBuffer.TryGetValue(frame, out var commands))
            {
                commands = new List<object>();
                _inputBuffer[frame] = commands;
            }
            commands.Add(new MoveCommand(frame, playerId, dx, dz));

            // 更新当前移动方向
            LastMoveDx = dx;
            LastMoveDz = dz;
        }

        /// <summary>
        /// 添加技能命令
        /// </summary>
        public void AddSkillCommand(int frame, string playerId, int skillSlot, float targetX, float targetY)
        {
            if (!_inputBuffer.TryGetValue(frame, out var commands))
            {
                commands = new List<object>();
                _inputBuffer[frame] = commands;
            }
            commands.Add(new SkillCommand(frame, playerId, skillSlot, targetX, targetY));
        }

        /// <summary>
        /// 添加停止命令
        /// </summary>
        public void AddStopCommand(int frame, string playerId)
        {
            if (!_inputBuffer.TryGetValue(frame, out var commands))
            {
                commands = new List<object>();
                _inputBuffer[frame] = commands;
            }
            commands.Add(new StopCommand(frame, playerId));

            // 清除移动方向
            LastMoveDx = 0;
            LastMoveDz = 0;
        }

        /// <summary>
        /// 获取指定帧的输入
        /// </summary>
        public List<object>? GetInputsForFrame(int frame)
        {
            return _inputBuffer.TryGetValue(frame, out var commands) ? commands : null;
        }

        /// <summary>
        /// 清除已处理的输入
        /// </summary>
        public void ClearProcessedInputs(int upToFrame)
        {
            var framesToRemove = new List<int>();
            foreach (var frame in _inputBuffer.Keys)
            {
                if (frame <= upToFrame)
                    framesToRemove.Add(frame);
            }
            foreach (var frame in framesToRemove)
            {
                _inputBuffer.Remove(frame);
            }
        }
    }
}
