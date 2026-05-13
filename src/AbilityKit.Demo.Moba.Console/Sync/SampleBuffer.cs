using System;
using AbilityKit.Core.Math;

namespace AbilityKit.Demo.Moba.Console.Sync
{
    /// <summary>
    /// 时间采样缓冲区
    /// 用于帧间插值
    /// 固定容量 Ring Buffer，按时间排序
    /// </summary>
    public class SampleBuffer
    {
        private const int Capacity = 4;
        private const double TimeEpsilon = 0.0001;

        private readonly Sample[] _samples = new Sample[Capacity];
        private int _count;
        private int _head;

        public void Add(double time, in Vec3 pos)
        {
            var s = new Sample { Time = time, Pos = pos };

            for (var i = 0; i < _count; i++)
            {
                if (Math.Abs(_samples[i].Time - time) <= TimeEpsilon)
                {
                    _samples[i] = s;
                    return;
                }
            }

            var insertAt = _count;
            for (var i = 0; i < _count; i++)
            {
                if (time < _samples[i].Time)
                {
                    insertAt = i;
                    break;
                }
            }

            if (_count < Capacity)
            {
                for (var i = _count; i > insertAt; i--)
                {
                    _samples[i] = _samples[i - 1];
                }
                _samples[insertAt] = s;
                _count++;
            }
            else
            {
                for (var i = Capacity - 1; i > insertAt; i--)
                {
                    _samples[i] = _samples[i - 1];
                }
                _samples[insertAt] = s;
            }
        }

        public bool TryEvaluate(double time, out Vec3 pos)
        {
            if (_count == 0)
            {
                pos = default;
                return false;
            }

            if (_count == 1)
            {
                pos = _samples[0].Pos;
                return true;
            }

            var first = _samples[0];
            if (time <= first.Time)
            {
                pos = first.Pos;
                return true;
            }

            var last = _samples[_count - 1];
            if (time >= last.Time)
            {
                pos = last.Pos;
                return true;
            }

            for (var i = 0; i < _count - 1; i++)
            {
                var a = _samples[i];
                var b = _samples[i + 1];
                if (time < a.Time) continue;
                if (time > b.Time) continue;

                var t = (float)((time - a.Time) / (b.Time - a.Time));
                pos = LerpUnclamped(a.Pos, b.Pos, t);
                return true;
            }

            pos = last.Pos;
            return true;
        }

        public int Count => _count;

        public void Clear()
        {
            _count = 0;
        }

        private static Vec3 LerpUnclamped(Vec3 a, Vec3 b, float t)
        {
            return new Vec3(
                a.X + (b.X - a.X) * t,
                a.Y + (b.Y - a.Y) * t,
                a.Z + (b.Z - a.Z) * t);
        }

        private struct Sample
        {
            public double Time;
            public Vec3 Pos;
        }
    }
}
