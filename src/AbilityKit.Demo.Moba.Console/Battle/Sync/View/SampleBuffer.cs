using System;

namespace AbilityKit.Demo.Moba.Console.Battle.Sync.View;

/// <summary>
/// 固定容量的环形缓冲，用于视图插值采样
/// 从 moba.view.runtime 的 SampleBuffer 简化而来
/// </summary>
public sealed class SampleBuffer
{
    private const int Capacity = 4;

    private readonly Sample[] _samples = new Sample[Capacity];
    private int _count;
    private int _head;

    public int Count => _count;

    /// <summary>
    /// 添加采样点（按时间顺序插入）
    /// </summary>
    public void Add(double time, float x, float y, float z)
    {
        var pos = new Sample
        {
            Time = time,
            X = x,
            Y = y,
            Z = z
        };

        // 如果缓冲区已满，替换最老的
        if (_count >= Capacity)
        {
            _head = (_head + 1) % Capacity;
        }
        else
        {
            _count++;
        }

        var insertIndex = (_head + _count - 1) % Capacity;
        _samples[insertIndex] = pos;
    }

    /// <summary>
    /// 尝试在指定时间进行线性插值
    /// </summary>
    /// <param name="time">要插值的时间</param>
    /// <param name="x">输出 X 坐标</param>
    /// <param name="y">输出 Y 坐标</param>
    /// <param name="z">输出 Z 坐标</param>
    /// <returns>是否成功插值</returns>
    public bool TryEvaluate(double time, out float x, out float y, out float z)
    {
        x = y = z = 0;

        if (_count == 0) return false;

        // 找到时间最近的两个采样点
        Sample? before = null;
        Sample? after = null;

        int head = _head;
        for (int i = 0; i < _count; i++)
        {
            var sample = _samples[(head + i) % Capacity];
            if (sample.Time <= time)
            {
                before = sample;
                if (i + 1 < _count)
                {
                    after = _samples[(head + i + 1) % Capacity];
                }
                break;
            }
            after = sample;
        }

        // 如果没有找到之前的点，使用最早的
        if (before == null)
        {
            before = _samples[head];
        }

        // 如果没有找到之后的点，使用最晚的
        if (after == null)
        {
            var lastIndex = (_head + _count - 1) % Capacity;
            after = _samples[lastIndex];
        }

        // 单点情况
        if (before.Value.Time == after.Value.Time)
        {
            x = before.Value.X;
            y = before.Value.Y;
            z = before.Value.Z;
            return true;
        }

        // 线性插值
        var t = (time - before.Value.Time) / (after.Value.Time - before.Value.Time);
        x = before.Value.X + (float)(t * (after.Value.X - before.Value.X));
        y = before.Value.Y + (float)(t * (after.Value.Y - before.Value.Y));
        z = before.Value.Z + (float)(t * (after.Value.Z - before.Value.Z));

        return true;
    }

    /// <summary>
    /// 清除所有采样点
    /// </summary>
    public void Clear()
    {
        _count = 0;
        _head = 0;
    }

    private struct Sample
    {
        public double Time;
        public float X;
        public float Y;
        public float Z;
    }
}

/// <summary>
/// 实体插值状态
/// </summary>
internal sealed class EntityInterpolationState
{
    public int ActorId { get; set; }
    public SampleBuffer PositionBuffer { get; } = new();
    public SampleBuffer RotationBuffer { get; } = new();
    public float CurrentX { get; set; }
    public float CurrentY { get; set; }
    public float CurrentZ { get; set; }
    public float CurrentRotation { get; set; }
    public float RenderX { get; set; }
    public float RenderY { get; set; }
    public float RenderZ { get; set; }
    public float RenderRotation { get; set; }
    public bool IsDead { get; set; }
}
