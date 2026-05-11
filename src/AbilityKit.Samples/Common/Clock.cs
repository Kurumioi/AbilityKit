using System;

namespace AbilityKit.Samples.Common
{
    /// <summary>
    /// 模拟时钟
    /// </summary>
    public sealed class Clock
    {
        private float _time;
        private float _deltaTime;
        private bool _paused;

        public float Time => _time;
        public float DeltaTime => _deltaTime;
        public bool IsPaused => _paused;

        public event Action<float>? OnTick;

        public void Pause() => _paused = true;
        public void Resume() => _paused = false;
        public void Reset()
        {
            _time = 0f;
            _deltaTime = 0f;
            _paused = false;
        }

        public void Advance(float delta)
        {
            if (_paused) return;
            _deltaTime = delta;
            _time += delta;
            OnTick?.Invoke(_deltaTime);
        }
    }
}
