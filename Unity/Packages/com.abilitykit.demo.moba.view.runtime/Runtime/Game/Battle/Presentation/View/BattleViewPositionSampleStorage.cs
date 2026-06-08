namespace AbilityKit.Game.Flow
{
    internal struct BattleViewPositionSampleStorage
    {
        public const int Capacity = 4;

        private BattleViewPositionSample _s0;
        private BattleViewPositionSample _s1;
        private BattleViewPositionSample _s2;
        private BattleViewPositionSample _s3;

        public int Count { get; private set; }

        public void Clear()
        {
            _s0 = default;
            _s1 = default;
            _s2 = default;
            _s3 = default;
            Count = 0;
        }

        public BattleViewPositionSample Get(int index)
        {
            switch (index)
            {
                case 0: return _s0;
                case 1: return _s1;
                case 2: return _s2;
                case 3: return _s3;
                default: return default;
            }
        }

        public void Set(int index, in BattleViewPositionSample sample)
        {
            switch (index)
            {
                case 0: _s0 = sample; break;
                case 1: _s1 = sample; break;
                case 2: _s2 = sample; break;
                case 3: _s3 = sample; break;
            }
        }

        public void AddFirst(in BattleViewPositionSample sample)
        {
            Set(0, in sample);
            Count = 1;
        }

        public void InsertAt(int index, in BattleViewPositionSample sample)
        {
            if (Count >= Capacity) return;

            for (var i = Count; i > index; i--)
            {
                var prev = Get(i - 1);
                Set(i, in prev);
            }

            Set(index, in sample);
            Count++;
        }

        public void ShiftLeftAndAppend(in BattleViewPositionSample sample)
        {
            for (var i = 0; i < Capacity - 1; i++)
            {
                var next = Get(i + 1);
                Set(i, in next);
            }

            Set(Capacity - 1, in sample);
            Count = Capacity;
        }
    }
}
