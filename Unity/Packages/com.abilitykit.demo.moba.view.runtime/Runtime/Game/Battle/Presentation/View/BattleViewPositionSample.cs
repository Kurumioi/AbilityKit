using UnityEngine;

namespace AbilityKit.Game.Flow
{
    internal readonly struct BattleViewPositionSample
    {
        public BattleViewPositionSample(double time, Vector3 position)
        {
            Time = time;
            Position = position;
        }

        public double Time { get; }
        public Vector3 Position { get; }
    }
}
