using System;
using System.Collections.Generic;
using AbilityKit.Core.Continuous;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.GameplayTags;

namespace AbilityKit.Demo.Moba.Services.Projectile.Launch
{
    public sealed class MobaProjectileLaunchContinuous : MobaContinuousRuntimeBase, IMobaTickableContinuous, IMobaContinuousRuntimeDebugSource, IMobaContextSourceProvider
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

        public override IContinuousConfig Config => _config;

        protected override bool OnActivating()
        {
            if (_started) return true;

            _started = true;
            var request = Request;
            var started = _executor.TryStartLaunch(in request, out _result);
            if (!started || !_result.Success)
            {
                AbilityKit.Core.Logging.Log.Warning($"[MobaProjectileLaunchContinuous] activation failed. casterActorId={Request.CasterActorId} launcherId={Request.LauncherId} projectileId={Request.ProjectileId} started={started} error={_result.Error ?? "<none>"}");
            }

            return started && _result.Success;
        }

        public void TickManaged(float deltaTimeSeconds)
        {
            if (!IsActive || deltaTimeSeconds <= 0f) return;

            AdvanceElapsed(deltaTimeSeconds);
            if (_started && _executor.IsLaunchComplete(in _result))
            {
                End(ContinuousEndReason.Completed);
            }
        }

        protected override void OnEnding(ContinuousEndReason reason)
        {
            StopLaunch(reason);
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

        private sealed class MobaProjectileLaunchConfig : MobaContinuousConfigBase
        {
            private readonly MobaProjectileLaunchContinuous _runtime;

            public MobaProjectileLaunchConfig(MobaProjectileLaunchContinuous runtime)
                : base(
                    runtime != null && runtime.Request.DurationMs > 0 ? runtime.Request.DurationMs / 1000f : 0f,
                    new ContinuousTagRequirements(),
                    Array.Empty<IMobaContinuousModifierSpec>())
            {
                _runtime = runtime;
            }

            public override string Id => $"projectile_launch:{_runtime.CasterActorId}:{_runtime.LauncherId}:{_runtime.ProjectileId}:{_runtime.GetHashCode()}";
            public override long OwnerId => _runtime.CasterActorId;
            public override bool CanBeInterrupted => true;
            public override int OwnerActorId => _runtime.CasterActorId;
            public override int ModifierSourceId => _runtime.GetHashCode();
            public override GameplayTagSource TagSource => GameplayTagSource.System;
            public override IReadOnlyList<int> IntervalEffectIds => Array.Empty<int>();
        }
    }
}
