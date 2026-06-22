using System;
using System.Collections.Generic;
using AbilityKit.Protocol.Shooter;

namespace AbilityKit.Demo.Shooter.Runtime
{
    public sealed class ShooterInputFrameBuffer
    {
        private static readonly IReadOnlyDictionary<int, ShooterPlayerCommand> EmptyFrameCommands = new Dictionary<int, ShooterPlayerCommand>(0);

        private readonly Dictionary<int, Dictionary<int, ShooterPlayerCommand>> _frames = new Dictionary<int, Dictionary<int, ShooterPlayerCommand>>();
        private readonly Dictionary<int, ShooterPlayerCommand> _latest = new Dictionary<int, ShooterPlayerCommand>();
        private int _oldestRetainedFrame;

        public int OldestRetainedFrame => _oldestRetainedFrame;

        public IReadOnlyDictionary<int, ShooterPlayerCommand> LatestCommands => _latest;

        public void Clear()
        {
            _frames.Clear();
            _latest.Clear();
            _oldestRetainedFrame = 0;
        }

        public void SetCommand(int frame, in ShooterPlayerCommand command)
        {
            SubmitCommand(frame, in command);
        }

        public void SubmitCommand(int frame, in ShooterPlayerCommand command)
        {
            if (frame < _oldestRetainedFrame)
            {
                frame = _oldestRetainedFrame;
            }

            if (!_frames.TryGetValue(frame, out var commands))
            {
                commands = new Dictionary<int, ShooterPlayerCommand>();
                _frames[frame] = commands;
            }

            commands[command.PlayerId] = command;
            _latest[command.PlayerId] = command;
        }

        public bool TryGetLatestCommand(int playerId, out ShooterPlayerCommand command)
        {
            return _latest.TryGetValue(playerId, out command);
        }

        public bool TryGetCommand(int frame, int playerId, out ShooterPlayerCommand command)
        {
            command = default;
            return _frames.TryGetValue(frame, out var commands) && commands.TryGetValue(playerId, out command);
        }

        public IReadOnlyDictionary<int, ShooterPlayerCommand> GetFrameCommandsOrEmpty(int frame)
        {
            return _frames.TryGetValue(frame, out var commands) ? commands : EmptyFrameCommands;
        }

        public bool TryGetFrameCommands(int frame, out IReadOnlyDictionary<int, ShooterPlayerCommand> commands)
        {
            if (_frames.TryGetValue(frame, out var frameCommands))
            {
                commands = frameCommands;
                return true;
            }

            commands = EmptyFrameCommands;
            return false;
        }

        public int CopyFrameCommands(int frame, List<ShooterPlayerCommand> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            destination.Clear();
            if (!_frames.TryGetValue(frame, out var commands))
            {
                return 0;
            }

            foreach (var kv in commands)
            {
                destination.Add(kv.Value);
            }

            destination.Sort(static (left, right) => left.PlayerId.CompareTo(right.PlayerId));
            return destination.Count;
        }

        public int CopyRetainedFrameNumbers(List<int> destination, int startFrameInclusive = 0, int endFrameExclusive = int.MaxValue)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            destination.Clear();
            foreach (var kv in _frames)
            {
                if (kv.Key >= startFrameInclusive && kv.Key < endFrameExclusive)
                {
                    destination.Add(kv.Key);
                }
            }

            destination.Sort();
            return destination.Count;
        }

        public void SetLatestCommand(in ShooterPlayerCommand command)
        {
            _latest[command.PlayerId] = command;
        }

        public bool TryConsumeLatestFire(int frame, int playerId, out ShooterPlayerCommand command)
        {
            if (!_latest.TryGetValue(playerId, out command) || !command.Fire)
            {
                return false;
            }

            command.Fire = false;
            _latest[playerId] = command;
            if (_frames.TryGetValue(frame, out var frameCommands) && frameCommands.ContainsKey(playerId))
            {
                frameCommands[playerId] = command;
            }

            return true;
        }

        public bool RemoveLatestCommand(int playerId)
        {
            return _latest.Remove(playerId);
        }

        public void TrimBefore(int frame)
        {
            if (frame <= _oldestRetainedFrame)
            {
                return;
            }

            var removed = new List<int>();
            foreach (var kv in _frames)
            {
                if (kv.Key < frame)
                {
                    removed.Add(kv.Key);
                }
            }

            for (var i = 0; i < removed.Count; i++)
            {
                _frames.Remove(removed[i]);
            }

            _oldestRetainedFrame = frame;
        }
    }
}
