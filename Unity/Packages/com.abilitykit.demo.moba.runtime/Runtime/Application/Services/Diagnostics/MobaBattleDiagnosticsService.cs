using System;
using System.Collections.Generic;
using System.Diagnostics;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.Host;
using AbilityKit.Ability.Triggering;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Demo.Moba.Config.BattleDemo;
using AbilityKit.Demo.Moba.Config.Core;
using AbilityKit.Demo.Moba.Services.EntityManager;
using AbilityKit.Diagnostics;

namespace AbilityKit.Demo.Moba.Services
{
    public interface IMobaBattleDiagnosticsService
    {
        long GetTimestamp();
        MobaBattleDiagnosticScope Measure(string metricName, double warnThresholdMs = 0d, string context = null);
        void RecordDuration(string metricName, long startTimestamp, double warnThresholdMs = 0d, string context = null);
        void Counter(string counterName, long value = 1L);
        void Gauge(string gaugeName, long value);
        void Sample(string sampleName, double value);
        void Warning(string key, string message, int maxCount = MobaBattleDiagnosticsDefaults.DefaultWarningLimit);
        void Warning(string key, Func<string> messageFactory, int maxCount = MobaBattleDiagnosticsDefaults.DefaultWarningLimit);
        void Exception(string key, Exception exception, string context, int maxCount = MobaBattleDiagnosticsDefaults.DefaultExceptionLimit);
        IReadOnlyList<MobaBattleDiagnosticWarningRecord> GetWarningsSnapshot();
    }

    public readonly struct MobaBattleDiagnosticWarningRecord
    {
        public MobaBattleDiagnosticWarningRecord(string key, string message, int count, bool suppressedAtLimit)
        {
            Key = key ?? string.Empty;
            Message = message ?? string.Empty;
            Count = count;
            SuppressedAtLimit = suppressedAtLimit;
        }

        public string Key { get; }
        public string Message { get; }
        public int Count { get; }
        public bool SuppressedAtLimit { get; }
    }

    public static class MobaBattleDiagnosticsDefaults
    {
        public const int DefaultWarningLimit = 3;
        public const int DefaultExceptionLimit = 3;

        public const double ContinuousTickWarnMs = 1.0d;
        public const double BuffDrainWarnMs = 1.0d;
        public const double DamagePipelineWarnMs = 0.5d;
        public const double DamageStageWarnMs = 0.25d;
        public const double EffectsStepWarnMs = 2.0d;
        public const double SkillPipelineStepWarnMs = 2.0d;
        public const double SkillRunnerStepWarnMs = 0.75d;
    }

    public static class MobaBattleDiagnosticMetric
    {
        public const string ContinuousTick = "moba.continuous.tick";
        public const string BuffDrain = "moba.buff.drain";
        public const string DamagePipeline = "moba.damage.pipeline";
        public const string DamageStage = "moba.damage.stage";
        public const string EffectsStep = "moba.effects.step";
        public const string SkillPipelineStep = "moba.skill.pipeline.step";
        public const string SkillRunnerStep = "moba.skill.runner.step";
        public const string TraceRoots = "moba.trace.roots";
        public const string TraceActiveRoots = "moba.trace.active.roots";
        public const string TraceRetainedRoots = "moba.trace.retained.roots";
        public const string TraceRetainedEndedRoots = "moba.trace.retained.ended.roots";
        public const string TraceStaleRetainedRoots = "moba.trace.retained.stale.roots";
        public const string SkillRuntimeActive = "moba.skill.runtime.active";
        public const string SkillRuntimeWaitingChildren = "moba.skill.runtime.waiting.children";
        public const string SkillRuntimePendingChildren = "moba.skill.runtime.pending.children";
    }

    public readonly struct MobaBattleDiagnosticScope : IDisposable
    {
        private readonly IMobaBattleDiagnosticsService _diagnostics;
        private readonly string _metricName;
        private readonly string _context;
        private readonly long _startTimestamp;
        private readonly double _warnThresholdMs;

        public MobaBattleDiagnosticScope(IMobaBattleDiagnosticsService diagnostics, string metricName, long startTimestamp, double warnThresholdMs, string context)
        {
            _diagnostics = diagnostics;
            _metricName = metricName;
            _startTimestamp = startTimestamp;
            _warnThresholdMs = warnThresholdMs;
            _context = context;
        }

        public void Dispose()
        {
            _diagnostics?.RecordDuration(_metricName, _startTimestamp, _warnThresholdMs, _context);
        }
    }

    public static class MobaDependencyResolveDiagnostics
    {
        public static void LogSkillExecutionDependencies(IWorldResolver services, string owner)
        {
            if (services == null)
            {
                MobaRuntimeLog.Error(MobaRuntimeLogModule.Input, MobaRuntimeLogPurpose.Validation, owner, "Cannot log skill execution dependency diagnostics because resolver is null.");
                return;
            }

            if (services is IWorldServiceContainer container)
            {
                MobaRuntimeLog.Error(
                    MobaRuntimeLogModule.Input,
                    MobaRuntimeLogPurpose.Validation,
                    owner,
                    $"Registered: SkillCastCoordinator={container.IsRegistered(typeof(SkillCastCoordinator))}, IFrameTime={container.IsRegistered(typeof(IFrameTime))}, IUnitResolver={container.IsRegistered(typeof(AbilityKit.Ability.Share.ECS.IUnitResolver))}, IMobaSkillPipelineLibrary={container.IsRegistered(typeof(IMobaSkillPipelineLibrary))}, IWorldClock={container.IsRegistered(typeof(IWorldClock))}, IEventBus={container.IsRegistered(typeof(AbilityKit.Triggering.Eventing.IEventBus))}");

                LogTryResolveFailure<IWorldClock>(services, owner, "IWorldClock");
                LogTryResolveFailure<IFrameTime>(services, owner, "IFrameTime");
                LogTryResolveFailure<AbilityKit.Triggering.Eventing.IEventBus>(services, owner, "IEventBus");
                LogTryResolveFailure<AbilityKit.Ability.Share.ECS.IUnitResolver>(services, owner, "IUnitResolver");
                LogTryResolveFailure<MobaSkillLoadoutService>(services, owner, nameof(MobaSkillLoadoutService));
                LogTryResolveFailure<MobaActorLookupService>(services, owner, nameof(MobaActorLookupService));
                LogTryResolveFailure<IMobaSkillPipelineLibrary>(services, owner, nameof(IMobaSkillPipelineLibrary));
            }

            LogResolveException<IMobaSkillPipelineLibrary>(services, owner, nameof(IMobaSkillPipelineLibrary));
            LogResolveException<MobaConfigDatabase>(services, owner, nameof(MobaConfigDatabase));
            LogResolveException<MobaEffectExecutionService>(services, owner, nameof(MobaEffectExecutionService));
            LogResolveException<AbilityKit.Triggering.Eventing.IEventBus>(services, owner, "IEventBus");
        }

        private static void LogTryResolveFailure<T>(IWorldResolver services, string owner, string name) where T : class
        {
            if (services.TryResolve(typeof(T), out _) == false)
            {
                MobaRuntimeLog.Error(MobaRuntimeLogModule.Input, MobaRuntimeLogPurpose.Validation, owner, $"Resolve check failed: {name}");
            }
        }

        private static void LogResolveException<T>(IWorldResolver services, string owner, string name) where T : class
        {
            try
            {
                services.Resolve<T>();
            }
            catch (Exception ex)
            {
                MobaRuntimeLog.Exception(ex, MobaRuntimeLogModule.Input, MobaRuntimeLogPurpose.Exception, owner, $"{name} resolve failed.");
            }
        }
    }

    [WorldService(typeof(IMobaBattleDiagnosticsService), WorldLifetime.Scoped)]
    [WorldService(typeof(MobaBattleDiagnosticsService), WorldLifetime.Scoped)]
    public sealed class MobaBattleDiagnosticsService : IMobaBattleDiagnosticsService, IService
    {
        private readonly Dictionary<string, int> _warningCounts = new Dictionary<string, int>(64);
        private readonly List<MobaBattleDiagnosticWarningRecord> _warnings = new List<MobaBattleDiagnosticWarningRecord>(32);

        public long GetTimestamp()
        {
            return Stopwatch.GetTimestamp();
        }

        public MobaBattleDiagnosticScope Measure(string metricName, double warnThresholdMs = 0d, string context = null)
        {
            if (string.IsNullOrEmpty(metricName)) return default;
            return new MobaBattleDiagnosticScope(this, metricName, GetTimestamp(), warnThresholdMs, context);
        }

        public void RecordDuration(string metricName, long startTimestamp, double warnThresholdMs = 0d, string context = null)
        {
            if (string.IsNullOrEmpty(metricName) || startTimestamp == 0L) return;

            var elapsedTicks = Stopwatch.GetTimestamp() - startTimestamp;
            if (elapsedTicks < 0L) return;

            var elapsedNs = elapsedTicks * 1000000000L / Stopwatch.Frequency;
            var elapsedMs = elapsedNs / 1000000.0d;
            ProfilerHub.Record(metricName, elapsedNs);
            ProfilerHub.Sample(metricName + ".ms", elapsedMs);

            if (warnThresholdMs > 0d && elapsedMs >= warnThresholdMs)
            {
                Warning(
                    "slow:" + metricName,
                    () =>
                    {
                        var suffix = string.IsNullOrEmpty(context) ? string.Empty : " " + context;
                        return $"[MobaDiagnostics] Slow path {metricName} elapsed={elapsedMs:0.###}ms threshold={warnThresholdMs:0.###}ms.{suffix}";
                    },
                    maxCount: MobaBattleDiagnosticsDefaults.DefaultWarningLimit);
            }
        }

        public void Counter(string counterName, long value = 1L)
        {
            if (string.IsNullOrEmpty(counterName) || value == 0L) return;
            if (value == 1L) ProfilerHub.Increment(counterName);
            else ProfilerHub.Add(counterName, value);
        }

        public void Gauge(string gaugeName, long value)
        {
            if (string.IsNullOrEmpty(gaugeName)) return;
            ProfilerHub.SetGauge(gaugeName, value);
        }

        public void Sample(string sampleName, double value)
        {
            if (string.IsNullOrEmpty(sampleName)) return;
            ProfilerHub.Sample(sampleName, value);
        }

        public void Warning(string key, string message, int maxCount = MobaBattleDiagnosticsDefaults.DefaultWarningLimit)
        {
            if (string.IsNullOrEmpty(message)) return;
            if (!ShouldLog(key, maxCount, out var count, out var suppressedAtLimit)) return;

            RecordWarningSnapshot(key, message, count, suppressedAtLimit);
            AbilityKit.Core.Logging.Log.Warning(message);
            if (suppressedAtLimit)
            {
                var suppressionMessage = $"[MobaDiagnostics] Further diagnostics suppressed for key={key}.";
                RecordWarningSnapshot(key, suppressionMessage, count, true);
                AbilityKit.Core.Logging.Log.Warning(suppressionMessage);
            }
        }

        public void Warning(string key, Func<string> messageFactory, int maxCount = MobaBattleDiagnosticsDefaults.DefaultWarningLimit)
        {
            if (messageFactory == null) return;
            if (!ShouldLog(key, maxCount, out var count, out var suppressedAtLimit)) return;

            var message = messageFactory();
            if (string.IsNullOrEmpty(message)) return;

            RecordWarningSnapshot(key, message, count, suppressedAtLimit);
            AbilityKit.Core.Logging.Log.Warning(message);
            if (suppressedAtLimit)
            {
                var suppressionMessage = $"[MobaDiagnostics] Further diagnostics suppressed for key={key}.";
                RecordWarningSnapshot(key, suppressionMessage, count, true);
                AbilityKit.Core.Logging.Log.Warning(suppressionMessage);
            }
        }

        public void Exception(string key, Exception exception, string context, int maxCount = MobaBattleDiagnosticsDefaults.DefaultExceptionLimit)
        {
            if (exception == null) return;
            if (!ShouldLog(key, maxCount, out _, out var suppressedAtLimit)) return;

            var message = string.IsNullOrEmpty(context) ? exception.Message : context;
            AbilityKit.Core.Logging.Log.Exception(exception, $"[MobaDiagnostics] {message}");
            if (suppressedAtLimit)
            {
                AbilityKit.Core.Logging.Log.Warning($"[MobaDiagnostics] Further exceptions suppressed for key={key}.");
            }
        }

        public IReadOnlyList<MobaBattleDiagnosticWarningRecord> GetWarningsSnapshot()
        {
            return _warnings.ToArray();
        }

        public void Dispose()
        {
            _warningCounts.Clear();
            _warnings.Clear();
        }

        private void RecordWarningSnapshot(string key, string message, int count, bool suppressedAtLimit)
        {
            _warnings.Add(new MobaBattleDiagnosticWarningRecord(key, message, count, suppressedAtLimit));
        }

        private bool ShouldLog(string key, int maxCount, out int count, out bool suppressedAtLimit)
        {
            count = 0;
            suppressedAtLimit = false;
            if (maxCount <= 0)
            {
                count = 1;
                return true;
            }

            if (string.IsNullOrEmpty(key)) key = "default";

            _warningCounts.TryGetValue(key, out count);
            if (count >= maxCount) return false;

            count++;
            _warningCounts[key] = count;
            suppressedAtLimit = count == maxCount;
            return true;
        }
    }
}

