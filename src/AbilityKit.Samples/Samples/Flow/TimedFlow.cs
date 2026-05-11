using System;
using AbilityKit.Samples.Infrastructure;

namespace AbilityKit.Samples.Samples.Flow
{
    /// <summary>
    /// TimedFlow - ????????
    /// </summary>
    public sealed class TimedFlow : SampleBase
    {
        public override string Title => "Timed Flow";
        public override string Description => "????????????";
        public override SampleCategory Category => SampleCategory.Flow;

        private string _state = "Idle";
        private int _tickCount = 0;

        protected override ISampleEnvironment CreateEnvironment()
        {
            // ?????????????
            return SampleEnvironmentFactory.Create(ExecutionMode.Simulated);
        }

        protected override void OnRun()
        {
            Log("????????");
            Output.Divider();

            // ??????
            Environment.OnTick += OnTick;

            Log($"????: {_state}");
            Output.Divider();

            // ?????Idle -> Charging -> Firing -> Cooldown -> Idle
            SimulateStateMachine();

            Environment.OnTick -= OnTick;
        }

        private void OnTick(float delta)
        {
            _tickCount++;
        }

        private void SimulateStateMachine()
        {
            Log("=== ????? ===");

            // Idle ??(0-1?)
            Log("?? Idle ??...");
            _state = "Idle";
            AdvanceTime(1.0f);
            Log($"  ????: {Time:F1}s, ??: {_state}");

            // Charging ??(1-2?)
            Log("?? Charging ??...");
            _state = "Charging";
            AdvanceTime(1.0f);
            Log($"  ????: {Time:F1}s, ??: {_state}");

            // Firing ??(2-3?)
            Log("?? Firing ??...");
            _state = "Firing";
            AdvanceTime(1.0f);
            Log($"  ????: {Time:F1}s, ??: {_state}");

            // Cooldown ??(3-4?)
            Log("?? Cooldown ??...");
            _state = "Cooldown";
            AdvanceTime(1.0f);
            Log($"  ????: {Time:F1}s, ??: {_state}");

            // ?? Idle
            Log("?? Idle ??...");
            _state = "Idle";
            Log($"  ????: {Time:F1}s, ??: {_state}");

            Output.Divider();
            Log($"?????: {_tickCount}");
            Log($"????: {Time:F1}?");
        }
    }
}
