using System.Collections.Generic;

namespace AbilityKit.Ability.Host.Extensions.Server.BattleHost
{
    public readonly struct BattleInputDrainResult<TInput>
    {
        public readonly int Frame;
        public readonly IReadOnlyList<TInput> Inputs;

        public BattleInputDrainResult(int frame, IReadOnlyList<TInput> inputs)
        {
            Frame = frame;
            Inputs = inputs;
        }

        public int Count => Inputs?.Count ?? 0;
    }

    public interface IBattleInputBuffer<TInput>
    {
        int PendingFrameCount { get; }

        bool Enqueue(int frame, TInput input);

        BattleInputDrainResult<TInput> Drain(int frame);

        void ClearFrame(int frame);

        void ClearBefore(int frame);

        void Clear();
    }

    public sealed class BattleInputBuffer<TInput> : IBattleInputBuffer<TInput>
    {
        private readonly Dictionary<int, List<TInput>> _inputsByFrame = new Dictionary<int, List<TInput>>();
        private readonly int _initialFrameCapacity;

        public BattleInputBuffer(int initialFrameCapacity = 8)
        {
            _initialFrameCapacity = initialFrameCapacity > 0 ? initialFrameCapacity : 8;
        }

        public int PendingFrameCount => _inputsByFrame.Count;

        public bool Enqueue(int frame, TInput input)
        {
            if (frame < 0)
            {
                return false;
            }

            if (!_inputsByFrame.TryGetValue(frame, out var list))
            {
                list = new List<TInput>(_initialFrameCapacity);
                _inputsByFrame[frame] = list;
            }

            list.Add(input);
            return true;
        }

        public BattleInputDrainResult<TInput> Drain(int frame)
        {
            if (!_inputsByFrame.TryGetValue(frame, out var list) || list == null || list.Count == 0)
            {
                _inputsByFrame.Remove(frame);
                return new BattleInputDrainResult<TInput>(frame, System.Array.Empty<TInput>());
            }

            _inputsByFrame.Remove(frame);
            return new BattleInputDrainResult<TInput>(frame, list);
        }

        public void ClearFrame(int frame)
        {
            _inputsByFrame.Remove(frame);
        }

        public void ClearBefore(int frame)
        {
            if (_inputsByFrame.Count == 0)
            {
                return;
            }

            var keysToRemove = new List<int>();
            foreach (var pair in _inputsByFrame)
            {
                if (pair.Key < frame)
                {
                    keysToRemove.Add(pair.Key);
                }
            }

            for (int i = 0; i < keysToRemove.Count; i++)
            {
                _inputsByFrame.Remove(keysToRemove[i]);
            }
        }

        public void Clear()
        {
            _inputsByFrame.Clear();
        }
    }
}
