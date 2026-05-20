using System;
using System.Collections.Generic;

namespace AbilityKit.Demo.Moba.Console.Battle.Flow
{
    /// <summary>
    /// 阶段主机
    /// 管理状态切换
    /// </summary>
    public sealed class PhaseHost : IDisposable
    {
        private readonly Dictionary<string, IPhase> _phases = new();
        private readonly List<string> _phaseOrder = new();
        private string _currentPhase;
        private PhaseContext _context;
        private bool _running;

        /// <summary>
        /// 阶段进入事件
        /// </summary>
        public event Action<string>? PhaseEntered;

        /// <summary>
        /// 阶段退出事件
        /// </summary>
        public event Action<string>? PhaseExited;

        /// <summary>
        /// 当前阶段
        /// </summary>
        public string CurrentPhase => _currentPhase;

        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool IsRunning => _running;

        /// <summary>
        /// 注册阶段
        /// </summary>
        public void Register(IPhase phase)
        {
            if (phase == null) return;
            _phases[phase.Name] = phase;
            if (!_phaseOrder.Contains(phase.Name))
            {
                _phaseOrder.Add(phase.Name);
            }
        }

        /// <summary>
        /// 获取阶段
        /// </summary>
        public IPhase? GetPhase(string phaseName)
        {
            return _phases.TryGetValue(phaseName, out var phase) ? phase : null;
        }

        /// <summary>
        /// 设置初始阶段
        /// </summary>
        public void SetInitialPhase(string phaseName)
        {
            if (!_phases.ContainsKey(phaseName))
            {
                Platform.Log.Error($"[PhaseHost] Phase not found: {phaseName}");
                return;
            }
            _currentPhase = phaseName;
        }

        /// <summary>
        /// 启动
        /// </summary>
        public void Start(PhaseContext context)
        {
            _context = context ?? new PhaseContext();
            _context.Root = context?.Root;
            _running = true;

            if (!string.IsNullOrEmpty(_currentPhase) && _phases.TryGetValue(_currentPhase, out var phase))
            {
                Platform.Log.Phase($"[PhaseHost] Starting with phase: {_currentPhase}");
                phase.OnEnter(_context);
                PhaseEntered?.Invoke(_currentPhase);
            }
        }

        /// <summary>
        /// 切换到指定阶段
        /// </summary>
        public void TransitionTo(string phaseName)
        {
            if (!_running)
            {
                Platform.Log.Warn("[PhaseHost] Cannot transition: host is not running");
                return;
            }

            if (!_phases.ContainsKey(phaseName))
            {
                Platform.Log.Error($"[PhaseHost] Phase not found: {phaseName}");
                return;
            }

            var previousPhase = _currentPhase;

            if (_phases.TryGetValue(_currentPhase, out var current))
            {
                Platform.Log.Phase($"[PhaseHost] Exiting phase: {_currentPhase}");
                current.OnExit(_context, phaseName);
                PhaseExited?.Invoke(_currentPhase);
            }

            _currentPhase = phaseName;
            if (_context != null)
            {
                _context.PreviousPhase = previousPhase;
                _context.EnterTime = Environment.TickCount / 1000.0;
            }

            if (_phases.TryGetValue(_currentPhase, out var next))
            {
                Platform.Log.Phase($"[PhaseHost] Entering phase: {_currentPhase}");
                next.OnEnter(_context);
                PhaseEntered?.Invoke(_currentPhase);
            }
        }

        /// <summary>
        /// Tick 流程
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!_running || string.IsNullOrEmpty(_currentPhase)) return;

            if (_phases.TryGetValue(_currentPhase, out var phase))
            {
                phase.OnTick(_context, deltaTime);
            }
        }

        /// <summary>
        /// 停止
        /// </summary>
        public void Stop()
        {
            if (!_running) return;

            if (_phases.TryGetValue(_currentPhase, out var current))
            {
                Platform.Log.Phase($"[PhaseHost] Stopping phase: {_currentPhase}");
                current.OnExit(_context, null);
                PhaseExited?.Invoke(_currentPhase);
            }

            _running = false;
            _currentPhase = null;
        }

        public void Dispose()
        {
            Stop();
            _phases.Clear();
            _phaseOrder.Clear();
            _context = null;
        }
    }
}
