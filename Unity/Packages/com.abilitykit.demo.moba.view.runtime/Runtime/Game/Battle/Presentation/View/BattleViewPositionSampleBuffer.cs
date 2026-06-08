using System;
using UnityEngine;

namespace AbilityKit.Game.Flow
{
    internal struct BattleViewPositionSampleBuffer
    {
        private const double TimeEpsilon = 1e-6;

        private BattleViewPositionSampleStorage _samples;

        public void Clear()
        {
            _samples.Clear();
        }

        public void Add(double time, in Vector3 pos)
        {
            var sample = new BattleViewPositionSample(time, pos);

            for (var i = 0; i < _samples.Count; i++)
            {
                var existing = _samples.Get(i);
                if (Math.Abs(existing.Time - time) <= TimeEpsilon)
                {
                    _samples.Set(i, in sample);
                    return;
                }
            }

            if (_samples.Count == 0)
            {
                _samples.AddFirst(in sample);
                return;
            }

            var insertAt = FindInsertIndex(time);
            if (_samples.Count < BattleViewPositionSampleStorage.Capacity)
            {
                _samples.InsertAt(insertAt, in sample);
                return;
            }

            if (insertAt <= 0) return;

            _samples.ShiftLeftAndAppend(in sample);
        }

        public bool TryEvaluate(double time, out Vector3 pos)
        {
            if (_samples.Count <= 0)
            {
                pos = default;
                return false;
            }

            if (_samples.Count == 1)
            {
                pos = _samples.Get(0).Position;
                return true;
            }

            var first = _samples.Get(0);
            if (time <= first.Time)
            {
                pos = first.Position;
                return true;
            }

            var last = _samples.Get(_samples.Count - 1);
            if (time >= last.Time)
            {
                pos = last.Position;
                return true;
            }

            return TryEvaluateBetweenSamples(time, in last, out pos);
        }

        private int FindInsertIndex(double time)
        {
            for (var i = 0; i < _samples.Count; i++)
            {
                if (time < _samples.Get(i).Time)
                {
                    return i;
                }
            }

            return _samples.Count;
        }

        private bool TryEvaluateBetweenSamples(double time, in BattleViewPositionSample fallback, out Vector3 pos)
        {
            for (var i = 0; i < _samples.Count - 1; i++)
            {
                var a = _samples.Get(i);
                var b = _samples.Get(i + 1);
                if (time < a.Time) continue;
                if (time > b.Time) continue;

                pos = Interpolate(in a, in b, time);
                return true;
            }

            pos = fallback.Position;
            return true;
        }

        private static Vector3 Interpolate(
            in BattleViewPositionSample a,
            in BattleViewPositionSample b,
            double time)
        {
            var dt = b.Time - a.Time;
            if (dt <= 0d) return b.Position;

            var t = (float)((time - a.Time) / dt);
            return Vector3.LerpUnclamped(a.Position, b.Position, t);
        }
    }
}
