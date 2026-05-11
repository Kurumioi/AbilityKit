using System;
using System.Collections.Generic;

namespace AbilityKit.Samples.Infrastructure
{
    /// <summary>
    /// 模拟执行环境 - 按时间步进执行
    /// </summary>
    public sealed class SimulatedEnvironment : ISampleEnvironment
    {
        private float _time;
        private float _deltaTime;
        private float _timeScale = 1f;
        private bool _paused;
        private float _maxSimulateTime = 10f;
        private float _timeStep = 0.016f; // 60 FPS

        public float Time => _time;
        public float DeltaTime => _deltaTime;
        public bool IsPaused => _paused;

        /// <summary>
        /// 时间缩放
        /// </summary>
        public float TimeScale
        {
            get => _timeScale;
            set => _timeScale = System.Math.Max(0f, value);
        }

        /// <summary>
        /// 最大模拟时间（防止无限循环）
        /// </summary>
        public float MaxSimulateTime
        {
            get => _maxSimulateTime;
            set => _maxSimulateTime = System.Math.Max(0, value);
        }

        /// <summary>
        /// 每步时间
        /// </summary>
        public float TimeStep
        {
            get => _timeStep;
            set => _timeStep = System.Math.Max(0.001f, value);
        }

        public event Action<float>? OnTick;

        public void Advance(float delta)
        {
            if (_paused) return;
            _deltaTime = delta * _timeScale;
            _time += _deltaTime;
            OnTick?.Invoke(_deltaTime);
        }

        public void Pause() => _paused = true;
        public void Resume() => _paused = false;
        public void Reset()
        {
            _time = 0;
            _deltaTime = 0;
            _paused = false;
        }

        public void AdvanceTo(float targetTime)
        {
            while (_time < targetTime && _time < _maxSimulateTime)
            {
                Advance(_timeStep);
            }
        }

        public void Tick()
        {
            Advance(_timeStep);
        }

        public void ExecuteUntilComplete()
        {
            var startTime = _time;
            int maxTicks = (int)(_maxSimulateTime / _timeStep);

            for (int i = 0; i < maxTicks; i++)
            {
                if (_paused) break;
                Tick();
            }
        }
    }
}
