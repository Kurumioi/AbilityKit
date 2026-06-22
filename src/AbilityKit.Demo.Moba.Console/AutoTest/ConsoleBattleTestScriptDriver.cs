using System;
using System.Threading;
using AbilityKit.Demo.Moba.Console.Platform;
using AbilityKit.Demo.Moba.Testing;

namespace AbilityKit.Demo.Moba.Console.AutoTest
{
    /// <summary>
    /// Console adapter for the shared platform-neutral battle test runner.
    /// The script runner owns step duration/tick semantics; this driver only maps a single tick to Console input/runtime.
    /// </summary>
    public sealed class ConsoleBattleTestScriptDriver : IBattleTestScriptDriver, IBattleTestScriptDriverLifecycle
    {
        private readonly ConsoleBattleBootstrapper _bootstrapper;
        private readonly AutoTestInputFeature _autoInput;
        private readonly AutoTestConfig _config;
        private int _tickCount;

        public ConsoleBattleTestScriptDriver(
            ConsoleBattleBootstrapper bootstrapper,
            AutoTestInputFeature autoInput,
            AutoTestConfig config)
        {
            _bootstrapper = bootstrapper ?? throw new ArgumentNullException(nameof(bootstrapper));
            _autoInput = autoInput ?? throw new ArgumentNullException(nameof(autoInput));
            _config = config ?? AutoTestConfig.Default;
        }

        public int TickCount => _tickCount;

        public void BeginScript(BattleTestScript script)
        {
            _tickCount = 0;
            _autoInput.Start();
            Log.Trace($"[AUTO-TEST] Shared runner script started: {script?.Name}");
        }

        public void Apply(BattleTestStep step)
        {
            _autoInput.Apply(step);
        }

        public void Tick()
        {
            _bootstrapper.Tick();
            _tickCount++;

            if (_config.TickIntervalMs > 0)
            {
                Thread.Sleep(_config.TickIntervalMs);
            }
        }

        public void EndScript(BattleTestScript script, BattleTestScriptRunResult result)
        {
            _autoInput.Stop();
            Log.Trace($"[AUTO-TEST] Shared runner script ended: {script?.Name}, completed={result?.Completed}, ticks={_tickCount}");
        }
    }
}
