using System;

namespace AbilityKit.Samples.Infrastructure
{
    /// <summary>
    /// 即时执行环境 - 立即执行所有逻辑，无时间延迟
    /// </summary>
    public sealed class InstantEnvironment : ISampleEnvironment
    {
        private float _time;

        public float Time => _time;
        public float DeltaTime => 0;
        public bool IsPaused => false;

        public event Action<float>? OnTick;

        public void Advance(float delta)
        {
            _time += delta;
        }

        public void Pause() { }
        public void Resume() { }
        public void Reset() => _time = 0;
        public void AdvanceTo(float targetTime) => _time = targetTime;

        public void Tick()
        {
            OnTick?.Invoke(0);
        }

        public void ExecuteUntilComplete()
        {
            OnTick?.Invoke(0);
        }
    }
}
