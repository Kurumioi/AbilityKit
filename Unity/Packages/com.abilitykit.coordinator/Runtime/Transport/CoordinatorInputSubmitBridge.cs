#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;

namespace AbilityKit.Coordinator
{
    /// <summary>
    /// 通过 SessionCoordinator 桥接一次已接受的本地输入，使 transport 可以复用 coordinator 侧输入路由。
    /// </summary>
    public sealed class CoordinatorInputSubmitBridge<TLocalSubmitResult, TRemoteSubmitResult>
    {
        private readonly Func<TLocalSubmitResult, PlayerInput> _createInput;
        private readonly Func<TLocalSubmitResult, PlayerInput, TLocalSubmitResult> _bindInput;
        private readonly Func<TLocalSubmitResult, TimeSpan?, CancellationToken, Task<TRemoteSubmitResult>> _submitAsync;
        private readonly object _sync = new();
        private TLocalSubmitResult _pendingLocal;
        private TimeSpan? _pendingTimeout;
        private CancellationToken _pendingCancellationToken;
        private bool _hasPendingLocal;
        private Task<TRemoteSubmitResult>? _pendingTask;

        public CoordinatorInputSubmitBridge(
            Func<TLocalSubmitResult, PlayerInput> createInput,
            Func<TLocalSubmitResult, PlayerInput, TLocalSubmitResult> bindInput,
            Func<TLocalSubmitResult, TimeSpan?, CancellationToken, Task<TRemoteSubmitResult>> submitAsync)
        {
            _createInput = createInput ?? throw new ArgumentNullException(nameof(createInput));
            _bindInput = bindInput ?? throw new ArgumentNullException(nameof(bindInput));
            _submitAsync = submitAsync ?? throw new ArgumentNullException(nameof(submitAsync));
            _pendingLocal = default!;
        }

        public Task<TRemoteSubmitResult> SubmitViaCoordinatorAsync(
            ISessionCoordinator coordinator,
            TLocalSubmitResult local,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            if (coordinator == null) throw new ArgumentNullException(nameof(coordinator));

            var input = _createInput(local);
            lock (_sync)
            {
                if (_hasPendingLocal)
                {
                    throw new InvalidOperationException("Coordinator input submit bridge already has a pending local input.");
                }

                _pendingLocal = local;
                _pendingTimeout = timeout;
                _pendingCancellationToken = cancellationToken;
                _hasPendingLocal = true;
                _pendingTask = null;
            }

            try
            {
                coordinator.SubmitLocalInput(input);

                Task<TRemoteSubmitResult>? task;
                lock (_sync)
                {
                    task = _pendingTask;
                    _pendingTask = null;
                    _hasPendingLocal = false;
                    _pendingTimeout = null;
                    _pendingCancellationToken = default;
                }

                return task ?? Task.FromException<TRemoteSubmitResult>(
                    new InvalidOperationException("Coordinator did not submit input through the configured transport."));
            }
            catch
            {
                Reset();
                throw;
            }
        }

        public bool TrySubmit(PlayerInput input)
        {
            TLocalSubmitResult local;
            TimeSpan? timeout;
            CancellationToken cancellationToken;
            lock (_sync)
            {
                if (!_hasPendingLocal)
                {
                    return false;
                }

                local = _bindInput(_pendingLocal, input);
                timeout = _pendingTimeout;
                cancellationToken = _pendingCancellationToken;
                _hasPendingLocal = false;
                _pendingTimeout = null;
                _pendingCancellationToken = default;
            }

            var task = _submitAsync(local, timeout, cancellationToken);
            lock (_sync)
            {
                _pendingTask = task;
            }

            return true;
        }

        public void Reset()
        {
            lock (_sync)
            {
                _pendingLocal = default!;
                _pendingTimeout = null;
                _pendingCancellationToken = default;
                _hasPendingLocal = false;
                _pendingTask = null;
            }
        }
    }
}
