using System;
using System.Threading;
using AbilityKit.Ability.Host.Framework;

namespace AbilityKit.Ability.Host.Builder.Components
{
    /// <summary>
    /// 固定步长时间驱动
    /// 使用定时器按固定帧率驱动 HostRuntime
    /// </summary>
    public sealed class FixedStepTimeDriver : ITimeDriver
    {
        private HostRuntime _runtime;
        private HostRuntimeOptions _options;
        private Timer _timer;
        private int _frameRate = 30;
        private bool _isRunning;

        public bool IsRunning => _isRunning;

        public int FrameRate
        {
            get => _frameRate;
            set => _frameRate = Math.Max(1, value);
        }

        public void Attach(HostRuntime runtime, HostRuntimeOptions options)
        {
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public void Detach()
        {
            Stop();
            _runtime = null;
            _options = null;
        }

        public void Start()
        {
            if (_isRunning) return;
            if (_runtime == null) return;

            var interval = 1000.0 / _frameRate;
            _timer = new Timer(Tick, null, (long)interval, (long)interval);
            _isRunning = true;
        }

        public void Stop()
        {
            _timer?.Dispose();
            _timer = null;
            _isRunning = false;
        }

        private void Tick(object state)
        {
            if (_runtime == null) return;

            try
            {
                _runtime.Tick(1.0f / _frameRate);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FixedStepTimeDriver] Tick exception: {ex}");
            }
        }
    }
}
