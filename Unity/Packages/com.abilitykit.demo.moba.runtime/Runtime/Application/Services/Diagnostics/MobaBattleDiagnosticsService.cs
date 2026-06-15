using System;
using System.Collections.Generic;
using System.Diagnostics;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;
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
        void Exception(string key, Exception exception, string context, int maxCount = MobaBattleDiagnosticsDefaults.DefaultExceptionLimit);
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

    [WorldService(typeof(IMobaBattleDiagnosticsService), WorldLifetime.Scoped)]
    [WorldService(typeof(MobaBattleDiagnosticsService), WorldLifetime.Scoped)]
    public sealed class MobaBattleDiagnosticsService : IMobaBattleDiagnosticsService, IService
    {
        private readonly Dictionary<string, int> _warningCounts = new Dictionary<string, int>(64);

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
                var suffix = string.IsNullOrEmpty(context) ? string.Empty : " " + context;
                Warning(
                    "slow:" + metricName,
                    $"[MobaDiagnostics] Slow path {metricName} elapsed={elapsedMs:0.###}ms threshold={warnThresholdMs:0.###}ms.{suffix}",
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
            if (!ShouldLog(key, maxCount, out var suppressedAtLimit)) return;

            AbilityKit.Core.Logging.Log.Warning(message);
            if (suppressedAtLimit)
            {
                AbilityKit.Core.Logging.Log.Warning($"[MobaDiagnostics] Further diagnostics suppressed for key={key}.");
            }
        }

        public void Exception(string key, Exception exception, string context, int maxCount = MobaBattleDiagnosticsDefaults.DefaultExceptionLimit)
        {
            if (exception == null) return;
            if (!ShouldLog(key, maxCount, out var suppressedAtLimit)) return;

            var message = string.IsNullOrEmpty(context) ? exception.Message : context;
            AbilityKit.Core.Logging.Log.Exception(exception, $"[MobaDiagnostics] {message}");
            if (suppressedAtLimit)
            {
                AbilityKit.Core.Logging.Log.Warning($"[MobaDiagnostics] Further exceptions suppressed for key={key}.");
            }
        }

        public void Dispose()
        {
            _warningCounts.Clear();
        }

        private bool ShouldLog(string key, int maxCount, out bool suppressedAtLimit)
        {
            suppressedAtLimit = false;
            if (maxCount <= 0) return true;
            if (string.IsNullOrEmpty(key)) key = "default";

            _warningCounts.TryGetValue(key, out var count);
            if (count >= maxCount) return false;

            count++;
            _warningCounts[key] = count;
            suppressedAtLimit = count == maxCount;
            return true;
        }
    }
}
