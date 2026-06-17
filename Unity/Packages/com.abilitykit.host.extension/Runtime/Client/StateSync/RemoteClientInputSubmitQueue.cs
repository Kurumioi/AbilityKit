#nullable enable

using System;
using System.Threading.Tasks;

namespace AbilityKit.Ability.Host.Extensions.Client.StateSync
{
    /// <summary>
    /// Client-side host extension primitive for submitting locally accepted inputs to a remote authority.
    /// It keeps at most one remote request in flight and one latest local result queued for the next submit.
    /// </summary>
    public sealed class RemoteClientInputSubmitQueue<TLocalSubmitResult, TRemoteSubmitResult>
    {
        private readonly Func<TLocalSubmitResult, TimeSpan, Task<TRemoteSubmitResult>> _submitAsync;
        private readonly Func<TRemoteSubmitResult, bool>? _shouldRequestResync;
        private readonly TimeSpan _timeout;
        private Task<TRemoteSubmitResult>? _pending;
        private TLocalSubmitResult _queuedInput;
        private bool _hasQueuedInput;
        private TRemoteSubmitResult _lastResult;
        private Exception? _lastError;
        private long _submittedCount;
        private long _queuedCount;
        private long _replacedCount;
        private long _completedCount;
        private long _failedCount;
        private long _resyncRequestedCount;

        public RemoteClientInputSubmitQueue(
            Func<TLocalSubmitResult, TimeSpan, Task<TRemoteSubmitResult>> submitAsync,
            TimeSpan timeout,
            Func<TRemoteSubmitResult, bool>? shouldRequestResync = null)
        {
            _submitAsync = submitAsync ?? throw new ArgumentNullException(nameof(submitAsync));
            _timeout = timeout;
            _shouldRequestResync = shouldRequestResync;
        }

        public bool HasPending => _pending != null;
        public bool HasQueued => _hasQueuedInput;
        public TRemoteSubmitResult LastResult => _lastResult;
        public Exception? LastError => _lastError;
        public long SubmittedCount => _submittedCount;
        public long QueuedCount => _queuedCount;
        public long ReplacedCount => _replacedCount;
        public long CompletedCount => _completedCount;
        public long FailedCount => _failedCount;
        public long ResyncRequestedCount => _resyncRequestedCount;

        public bool SubmitOrQueue(TLocalSubmitResult local)
        {
            CompleteIfFinished();
            if (_pending == null)
            {
                Start(local);
                return true;
            }

            if (_hasQueuedInput)
            {
                _replacedCount++;
            }
            else
            {
                _queuedCount++;
            }

            _queuedInput = local;
            _hasQueuedInput = true;
            return false;
        }

        public void CompleteIfFinished()
        {
            var pending = _pending;
            if (pending == null || !pending.IsCompleted)
            {
                return;
            }

            _pending = null;
            try
            {
                _lastResult = pending.GetAwaiter().GetResult();
                _lastError = null;
                _completedCount++;
                if (_shouldRequestResync != null && _shouldRequestResync(_lastResult))
                {
                    _resyncRequestedCount++;
                }
            }
            catch (Exception ex)
            {
                _lastError = ex;
                _failedCount++;
            }

            if (_hasQueuedInput)
            {
                var next = _queuedInput;
                _queuedInput = default;
                _hasQueuedInput = false;
                Start(next);
            }
        }

        public void Reset()
        {
            _pending = null;
            _queuedInput = default;
            _hasQueuedInput = false;
            _lastResult = default;
            _lastError = null;
            _submittedCount = 0;
            _queuedCount = 0;
            _replacedCount = 0;
            _completedCount = 0;
            _failedCount = 0;
            _resyncRequestedCount = 0;
        }

        private void Start(TLocalSubmitResult local)
        {
            _lastError = null;
            _pending = _submitAsync(local, _timeout);
            _submittedCount++;
            CompleteIfFinished();
        }
    }
}
