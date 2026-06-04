using System;
using AbilityKit.Core.Continuous;
using AbilityKit.Demo.Moba.Services;

namespace AbilityKit.Demo.Moba.Services.Projectile.Launch
{
    public sealed class MobaProjectileLaunchContinuous : IMobaTickableContinuous, IMobaContinuousRuntimeDebugSource, IMobaContextSourceProvider
    {
        private readonly MobaProjectileLaunchConfig _config;
        private readonly IMobaProjectileLaunchExecutor _executor;
        private MobaProjectileLaunchResult _result;
        private bool _started;
        private bool _stopped;

        public MobaProjectileLaunchContinuous(in MobaProjectileLaunchRequest request, IMobaProjectileLaunchExecutor executor)
        {
            Request = request;
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
            _config = new MobaProjectileLaunchConfig(this);
        }

        public MobaProjectileLaunchRequest Request { get; }
        public MobaProjectileLaunchResult Result => _result;
        public int CasterActorId => Request.CasterActorId;
        public int LauncherActorId => _result.LauncherActorId;
        public int LauncherId => Request.LauncherId;
        public int ProjectileId => Request.ProjectileId;

        public IContinuousConfig Config => _config;
        public ContinuousState State { get; private set; } = ContinuousState.Inactive;
        public bool IsActive => State == ContinuousState.Active;
        public bool IsTerminated => State == ContinuousState.Expired || State == ContinuousState.Aborted;
        public bool IsPaused => State == ContinuousState.Paused;
        public float ElapsedSeconds { get; private set; }

        public event Action<IContinuous, ContinuousEndReason> OnEnded;

        public void Activate()
        {
            if (State == ContinuousState.Active) return;
            if (IsTerminated) return;

            State = ContinuousState.Activating;
            if (!_started)
            {
                _started = true;
                var request = Request;
                if (!_executor.TryStartLaunch(in request, out _result) || !_result.Success)
                {
                    State = ContinuousState.Aborted;
                    OnEnded?.Invoke(this, ContinuousEndReason.Interrupted);
                    return;
                }
            }

            State = ContinuousState.Active;
        }

        public void Pause()
        {
            if (State != ContinuousState.Active) return;
            State = ContinuousState.Paused;
        }

        public void Resume()
        {
            if (State != ContinuousState.Paused) return;
            State = ContinuousState.Active;
        }

        public void TickManaged(float deltaTimeSeconds)
        {
            if (!IsActive || deltaTimeSeconds <= 0f) return;

            ElapsedSeconds += deltaTimeSeconds;
            if (_started && _executor.IsLaunchComplete(in _result))
            {
                End(ContinuousEndReason.Completed);
            }
        }

        public void End(ContinuousEndReason reason)
        {
            if (IsTerminated) return;

            StopLaunch(reason);
            State = reason == ContinuousEndReason.Completed ? ContinuousState.Expired : ContinuousState.Aborted;
            OnEnded?.Invoke(this, reason);
        }

        public void Abort(string reason)
        {
            End(ContinuousEndReason.Interrupted);
        }

        public bool TryGetRuntimeDebugInfo(out MobaContinuousRuntimeDebugInfo info)
        {
            TryGetContextSource(out var source);
            var sourceContextId = source.SourceContextId != 0 ? source.SourceContextId : Request.SourceContext.SourceContextId;
            info = new MobaContinuousRuntimeDebugInfo(
                "ProjectileLaunch",
                LauncherId,
                CasterActorId,
                0,
                sourceContextId,
                source.ParentContextId,
                source.RootContextId,
                source.OwnerContextId,
                Request.SourceContext.SkillRuntimeHandle,
                source);
            return CasterActorId > 0 || LauncherId > 0 || ProjectileId > 0 || sourceContextId != 0;
        }

        public bool TryGetContextSource(out MobaContextSourceView source)
        {
            if (Request.SourceContext.TryGetContextSource(out source))
            {
                return source.IsValid;
            }

            source = default;
            return false;
        }

        private void StopLaunch(ContinuousEndReason reason)
        {
            if (_stopped) return;
            _stopped = true;

            if (_result.Success)
            {
                _executor.StopLaunch(in _result, reason);
            }
        }

        private sealed class MobaProjectileLaunchConfig : IContinuousConfig, IDurationConfig
        {
            private readonly MobaProjectileLaunchContinuous _runtime;

            public MobaProjectileLaunchConfig(MobaProjectileLaunchContinuous runtime)
            {
                _runtime = runtime;
                DurationSeconds = runtime.Request.DurationMs > 0 ? runtime.Request.DurationMs / 1000f : (float?)null;
            }

            public string Id => $"projectile_launch:{_runtime.CasterActorId}:{_runtime.LauncherId}:{_runtime.ProjectileId}:{_runtime.GetHashCode()}";
            public long OwnerId => _runtime.CasterActorId;
            public bool CanBeInterrupted => true;
            public float? DurationSeconds { get; }
        }
    }
}
