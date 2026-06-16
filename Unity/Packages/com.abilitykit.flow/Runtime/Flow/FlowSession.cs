using System;
using AbilityKit.Ability.Flow.Pooling;

namespace AbilityKit.Ability.Flow
{
    public sealed class FlowSession : IDisposable
    {
        private FlowRunner _runner;
        private bool _disposed;

        public event Action Started;
        public event Action<FlowStatus, FlowStatus> StatusChanged;
        public event Action<FlowStatus> Finished;

        public event Action<Exception> UnhandledException;

        internal FlowSession(bool deferRent)
        {
            _disposed = true;
        }

        public FlowSession()
        {
            ResetForRent(FlowPools.RentRunner());
        }

        public FlowContext Context
        {
            get
            {
                ThrowIfDisposed();
                return _runner.Context;
            }
        }

        public FlowStatus Status
        {
            get
            {
                ThrowIfDisposed();
                return _runner.Status;
            }
        }

        public void Start(IFlowNode root)
        {
            ThrowIfDisposed();
            _runner.Start(
                root,
                onFinished: s => Finished?.Invoke(s),
                onStatusChanged: (prev, next) => StatusChanged?.Invoke(prev, next)
            );
            Started?.Invoke();
        }

        public FlowStatus Step(float deltaTime)
        {
            ThrowIfDisposed();
            return _runner.Step(deltaTime);
        }

        public void Stop()
        {
            ThrowIfDisposed();
            _runner.Stop();
        }

        public void Dispose()
        {
            if (_disposed) return;
            ResetCallbacks();
            FlowPools.ReleaseRunner(_runner);
            _runner = null;
            _disposed = true;
        }

        internal void ResetForRent(FlowRunner runner)
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _runner.UnhandledException += OnRunnerUnhandledException;
            _disposed = false;
        }

        internal void ResetForRelease()
        {
            if (!_disposed)
            {
                Dispose();
                return;
            }

            ResetCallbacks();
            _runner = null;
        }

        private void OnRunnerUnhandledException(Exception ex)
        {
            UnhandledException?.Invoke(ex);
        }

        private void ResetCallbacks()
        {
            Started = null;
            StatusChanged = null;
            Finished = null;
            UnhandledException = null;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(FlowSession));
        }
    }
}
