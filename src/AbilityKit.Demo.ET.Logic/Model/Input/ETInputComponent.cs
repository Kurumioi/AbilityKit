using System;
using System.Collections.Generic;

namespace ET.Logic
{
    /// <summary>
    /// ???? - ??????
    /// ?? Moba.Console ?ConsoleInputFeature
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ETInputComponent: Entity, IAwake
    {
        // ???? (?? -> ????)
        private Dictionary<int, List<object>> _inputBuffer = new Dictionary<int, List<object>>();

        // ??????
        public float MoveTargetX { get; set; }
        public float MoveTargetY { get; set; }
        public int CurrentSkillSlot { get; set; } = -1;
        public float SkillTargetX { get; set; }
        public float SkillTargetY { get; set; }

        public void Awake()
        {
        }

        /// <summary>
        /// ?????????
        /// </summary>
        public void AddMoveCommand(int frame, int actorId, float x, float y)
        {
            if (!_inputBuffer.TryGetValue(frame, out var commands))
            {
                commands = new List<object>();
                _inputBuffer[frame] = commands;
            }
            commands.Add(new MoveCommand(frame, actorId, x, y));
        }

        /// <summary>
        /// ?????????
        /// </summary>
        public void AddSkillCommand(int frame, int actorId, int skillSlot, float targetX, float targetY)
        {
            if (!_inputBuffer.TryGetValue(frame, out var commands))
            {
                commands = new List<object>();
                _inputBuffer[frame] = commands;
            }
            commands.Add(new SkillCommand(frame, actorId, skillSlot, targetX, targetY));
        }

        /// <summary>
        /// ?????????
        /// </summary>
        public void AddStopCommand(int frame, int actorId)
        {
            if (!_inputBuffer.TryGetValue(frame, out var commands))
            {
                commands = new List<object>();
                _inputBuffer[frame] = commands;
            }
            commands.Add(new StopCommand(frame, actorId));
        }

        /// <summary>
        /// ????????
        /// </summary>
        public List<object>? GetInputsForFrame(int frame)
        {
            return _inputBuffer.TryGetValue(frame, out var commands) ? commands : null;
        }

        /// <summary>
        /// ????????
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

    /// <summary>
    /// ????
    /// </summary>
    public sealed record MoveCommand(int Frame, int ActorId, float X, float Y);

    /// <summary>
    /// ????
    /// </summary>
    public sealed record SkillCommand(int Frame, int ActorId, int SkillSlot, float TargetX, float TargetY);

    /// <summary>
    /// ????
    /// </summary>
    public sealed record StopCommand(int Frame, int ActorId);
}
