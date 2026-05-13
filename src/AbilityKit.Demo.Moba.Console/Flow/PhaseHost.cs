using System;
using System.Collections.Generic;

namespace AbilityKit.Demo.Moba.Console.Flow
{
    /// <summary>
    /// ?????
    /// </summary>
    public sealed class PhaseContext
    {
        /// <summary>
        /// ????
        /// </summary>
        public IModuleContext Root { get; set; }

        /// <summary>
        /// ????
        /// </summary>
        public string PhaseName { get; set; }

        /// <summary>
        /// ????
        /// </summary>
        public string PreviousPhase { get; set; }

        /// <summary>
        /// ????
        /// </summary>
        public double EnterTime { get; set; }

        /// <summary>
        /// ????
        /// </summary>
        public Dictionary<string, object> Data { get; } = new();
    }

    /// <summary>
    /// ????
    /// </summary>
    public interface IPhase
    {
        string Name { get; }
        void OnEnter(PhaseContext context);
        void OnTick(PhaseContext context, float deltaTime);
        void OnExit(PhaseContext context, string nextPhase);
    }

    /// <summary>
    /// ????
    /// </summary>
    public sealed class PhaseHost : IDisposable
    {
        private readonly Dictionary<string, IPhase> _phases = new();
        private readonly List<string> _phaseOrder = new();
        private string _currentPhase;
        private PhaseContext _context;
        private bool _running;

        /// <summary>
        /// ??????
        /// </summary>
        public event Action<string>? PhaseEntered;

        /// <summary>
        /// ??????
        /// </summary>
        public event Action<string>? PhaseExited;

        /// <summary>
        /// ????
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
        /// ??????
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
        /// ??????
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
        /// ????
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
            _context.PreviousPhase = previousPhase;
            _context.EnterTime = Environment.TickCount / 1000.0;

            if (_phases.TryGetValue(_currentPhase, out var next))
            {
                Platform.Log.Phase($"[PhaseHost] Entering phase: {_currentPhase}");
                next.OnEnter(_context);
                PhaseEntered?.Invoke(_currentPhase);
            }
        }

        /// <summary>
        /// Tick ????
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
        /// ??????
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

        /// <summary>
        /// ????????
        /// </summary>
        public string CurrentPhase => _currentPhase;

        /// <summary>
        /// ??????
        /// </summary>
        public bool IsRunning => _running;

        public void Dispose()
        {
            Stop();
            _phases.Clear();
            _phaseOrder.Clear();
            _context = null;
        }
    }
}
