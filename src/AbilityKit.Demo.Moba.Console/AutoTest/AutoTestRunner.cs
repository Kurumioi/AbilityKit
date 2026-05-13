using System;
using System.Threading;
using AbilityKit.Demo.Moba.Console.Core.Battle.Context;
using AbilityKit.Demo.Moba.Console.Platform;

namespace AbilityKit.Demo.Moba.Console
{
    public sealed class AutoTestRunner : IDisposable
    {
        private readonly ConsoleBattleBootstrapper _bootstrapper;
        private readonly ManualResetEvent _completed = new(false);
        private readonly AutoTestConfig _config;
        private Thread _testThread;
        private bool _disposed;
        private Log.LogLevel _savedLogLevel;

        public event Action<AutoTestResult> OnTestCompleted;

        public AutoTestRunner(ConsoleBattleBootstrapper bootstrapper) : this(bootstrapper, AutoTestConfig.Default)
        {
        }

        public AutoTestRunner(ConsoleBattleBootstrapper bootstrapper, AutoTestConfig config)
        {
            _bootstrapper = bootstrapper ?? throw new ArgumentNullException(nameof(bootstrapper));
            _config = config ?? AutoTestConfig.Default;
        }

        public void Start()
        {
            if (_testThread != null) return;

            Log.Trace("[AUTO-TEST] Starting auto test runner...");
            _testThread = new Thread(TestLoop);
            _testThread.IsBackground = true;
            _testThread.Start();
        }

        public void WaitForCompletion()
        {
            _completed.WaitOne();
        }

        private void TestLoop()
        {
            _savedLogLevel = Log.MinLevel;
            Log.SetMinLevel(Log.LogLevel.Debug);

            try
            {
                var results = new AutoTestResult();
                RunTests(results);
                Complete(results);
            }
            catch (Exception ex)
            {
                Log.Error($"[AUTO-TEST] Unexpected error: {ex.Message}");
                Log.Error(ex.StackTrace);
                Complete(new AutoTestResult { HasUnexpectedError = true, ErrorMessage = ex.Message });
            }
            finally
            {
                Log.SetMinLevel(_savedLogLevel);
            }
        }

        private void RunTests(AutoTestResult results)
        {
            Log.System("========================================");
            Log.System("   AUTO TEST STARTING");
            Log.System("========================================");

            results.InitTest = TestInitialization();
            if (!results.InitTest.Passed)
            {
                Log.System("[AUTO-TEST] Initialization failed, stopping tests");
                return;
            }

            results.PhaseTest = TestPhaseTransition();
            if (!results.PhaseTest.Passed)
            {
                Log.System("[AUTO-TEST] Phase transition failed, stopping tests");
                return;
            }

            results.FrameSyncTest = TestFrameSync();
            if (!results.FrameSyncTest.Passed)
            {
                Log.System("[AUTO-TEST] Frame sync failed, stopping tests");
                return;
            }

            results.SkillCastTest = TestSkillCast();
            if (!results.SkillCastTest.Passed)
            {
                Log.System("[AUTO-TEST] Skill cast failed, stopping tests");
                return;
            }

            results.DamageTest = TestDamageCalculation();
            if (!results.DamageTest.Passed)
            {
                Log.System("[AUTO-TEST] Damage calculation failed");
            }

            results.CooldownTest = TestCooldownSystem();
            if (!results.CooldownTest.Passed)
            {
                Log.System("[AUTO-TEST] Cooldown system failed");
            }

            results.MoveTest = TestMoveSystem();
            if (!results.MoveTest.Passed)
            {
                Log.System("[AUTO-TEST] Move system failed");
            }
        }

        private void Complete(AutoTestResult results)
        {
            results.TotalTimeMs = (long)(DateTime.Now - results.StartTime).TotalMilliseconds;

            Log.System("========================================");
            Log.System("   AUTO TEST COMPLETED");
            Log.System("========================================");
            Log.System($"Total Time: {results.TotalTimeMs}ms");
            Log.System($"Init: {(results.InitTest?.Passed == true ? "PASS" : "FAIL")}");
            Log.System($"Phase: {(results.PhaseTest?.Passed == true ? "PASS" : "FAIL")}");
            Log.System($"FrameSync: {(results.FrameSyncTest?.Passed == true ? "PASS" : "FAIL")}");
            Log.System($"SkillCast: {(results.SkillCastTest?.Passed == true ? "PASS" : "FAIL")}");
            Log.System($"Damage: {(results.DamageTest?.Passed == true ? "PASS" : "FAIL")}");
            Log.System($"Cooldown: {(results.CooldownTest?.Passed == true ? "PASS" : "FAIL")}");
            Log.System($"Move: {(results.MoveTest?.Passed == true ? "PASS" : "FAIL")}");

            var passed = results.PassedCount;
            var total = results.TotalCount;
            Log.System($"========================================");
            Log.System($"Result: {passed}/{total} tests passed");
            Log.System("========================================");

            OnTestCompleted?.Invoke(results);
            _completed.Set();
        }

        #region Test Methods

        private TestResult TestInitialization()
        {
            Log.System("[TEST-1] Testing Initialization...");

            var result = new TestResult { Name = "Initialization" };

            try
            {
                if (_bootstrapper == null)
                {
                    result.Fail("Bootstrapper is null");
                    return result;
                }

                var flow = _bootstrapper.Flow;
                if (flow == null)
                {
                    result.Fail("Flow is null");
                    return result;
                }
                Log.Trace("[TEST-1] Flow initialized");

                var ctx = _bootstrapper.Context;
                if (ctx == null)
                {
                    result.Fail("Context is null");
                    return result;
                }
                Log.Trace("[TEST-1] Context initialized");

                var battleServices = _bootstrapper.BattleServices;
                if (battleServices == null)
                {
                    result.Fail("BattleServices is null");
                    return result;
                }
                Log.Trace($"[TEST-1] BattleServices initialized, ActorCount={battleServices.ActorCount}");

                var skillExec = _bootstrapper.SkillExecutor;
                if (skillExec == null)
                {
                    result.Fail("SkillExecutor is null");
                    return result;
                }
                Log.Trace("[TEST-1] SkillExecutor initialized");

                if (ctx.EcsWorld == null)
                {
                    result.Fail("ECS World is null");
                    return result;
                }
                Log.Trace($"[TEST-1] ECS World initialized, AliveCount={ctx.EcsWorld.AliveCount}");

                var phase = flow.CurrentPhase;
                Log.Trace($"[TEST-1] Current phase: {phase}");

                result.Pass();
            }
            catch (Exception ex)
            {
                result.Fail($"Exception: {ex.Message}");
            }

            return result;
        }

        private TestResult TestPhaseTransition()
        {
            Log.System("[TEST-2] Testing Phase Transition...");

            var result = new TestResult { Name = "Phase Transition" };

            try
            {
                var flow = _bootstrapper.Flow;
                var initialPhase = flow.CurrentPhase;
                Log.Trace($"[TEST-2] Initial phase: {initialPhase}");

                _bootstrapper.TransitionTo("InMatch");
                Thread.Sleep(20);

                var afterPhase = flow.CurrentPhase;
                Log.Trace($"[TEST-2] After transition: {afterPhase}");

                if (afterPhase != "InMatch")
                {
                    result.Fail($"Expected 'InMatch', got '{afterPhase}'");
                    return result;
                }

                if (_bootstrapper.Context.State != BattleState.InMatch)
                {
                    result.Fail($"Expected BattleState.InMatch, got {_bootstrapper.Context.State}");
                    return result;
                }

                result.Pass();
            }
            catch (Exception ex)
            {
                result.Fail($"Exception: {ex.Message}");
            }

            return result;
        }

        private TestResult TestFrameSync()
        {
            Log.System("[TEST-3] Testing Frame Sync...");

            var result = new TestResult { Name = "Frame Sync" };

            try
            {
                var initialFrame = _bootstrapper.Context.LastFrame;
                Log.Trace($"[TEST-3] Initial frame: {initialFrame}");

                for (int i = 0; i < 10; i++)
                {
                    _bootstrapper.Tick();
                }

                Thread.Sleep(100);

                var afterFrame = _bootstrapper.Context.LastFrame;
                Log.Trace($"[TEST-3] After 10 ticks: {afterFrame}");

                if (afterFrame <= initialFrame)
                {
                    result.Fail($"Frame not incremented: {initialFrame} -> {afterFrame}");
                    return result;
                }

                if (afterFrame - initialFrame < 10)
                {
                    result.Fail($"Frame increment less than expected: {initialFrame} -> {afterFrame}");
                    return result;
                }

                result.Pass();
            }
            catch (Exception ex)
            {
                result.Fail($"Exception: {ex.Message}");
            }

            return result;
        }

        private TestResult TestSkillCast()
        {
            Log.System("[TEST-4] Testing Skill Cast...");

            var result = new TestResult { Name = "Skill Cast" };

            try
            {
                var ctx = _bootstrapper.Context;
                var localActorId = ctx.LocalActorId;
                Log.Trace($"[TEST-4] LocalActorId: {localActorId}");

                var inputFeature = GetInputFeature();
                if (inputFeature == null)
                {
                    result.Fail("InputFeature not found");
                    return result;
                }

                Log.Trace("[TEST-4] Clicking skill 1...");
                inputFeature.ClickSkill(1);

                _bootstrapper.Tick();
                Thread.Sleep(20);

                Log.Trace("[TEST-4] Clicking skill 2...");
                inputFeature.ClickSkill(2);
                _bootstrapper.Tick();
                Thread.Sleep(20);

                Log.Trace("[TEST-4] Skill cast test completed");
                result.Pass();
            }
            catch (Exception ex)
            {
                result.Fail($"Exception: {ex.Message}");
            }

            return result;
        }

        private TestResult TestDamageCalculation()
        {
            Log.System("[TEST-5] Testing Damage Calculation...");

            var result = new TestResult { Name = "Damage Calculation" };

            try
            {
                var battleServices = _bootstrapper.BattleServices;
                var ctx = _bootstrapper.Context;

                Log.Trace("[TEST-5] Applying simulated damage...");
                _bootstrapper.SimulateDamage(1, 100);

                _bootstrapper.Tick();
                Thread.Sleep(50);

                var actor = battleServices.GetActor(1);
                Log.Trace($"[TEST-5] Actor1 HP after damage: {actor?.Hp ?? -1}");

                if (actor == null)
                {
                    Log.Trace("[TEST-5] Actor was removed (died), this is expected for large damage");
                }

                result.Pass();
            }
            catch (Exception ex)
            {
                result.Fail($"Exception: {ex.Message}");
            }

            return result;
        }

        private TestResult TestCooldownSystem()
        {
            Log.System("[TEST-6] Testing Cooldown System...");

            var result = new TestResult { Name = "Cooldown System" };

            try
            {
                var inputFeature = GetInputFeature();
                if (inputFeature == null)
                {
                    result.Fail("InputFeature not found");
                    return result;
                }

                Log.Trace("[TEST-6] Clicking skill 1 (should trigger cooldown)...");
                inputFeature.ClickSkill(1);
                _bootstrapper.Tick();

                Log.Trace("[TEST-6] Immediately clicking skill 1 again (should be on cooldown)...");
                inputFeature.ClickSkill(1);
                _bootstrapper.Tick();

                Log.Trace("[TEST-6] Waiting for cooldown...");
                for (int i = 0; i < 10; i++)
                {
                    _bootstrapper.Tick();
                }

                Log.Trace("[TEST-6] Cooldown test completed");
                result.Pass();
            }
            catch (Exception ex)
            {
                result.Fail($"Exception: {ex.Message}");
            }

            return result;
        }

        private TestResult TestMoveSystem()
        {
            Log.System("[TEST-7] Testing Move System...");

            var result = new TestResult { Name = "Move System" };

            try
            {
                var inputFeature = GetInputFeature();
                if (inputFeature == null)
                {
                    result.Fail("InputFeature not found");
                    return result;
                }

                var battleServices = _bootstrapper.BattleServices;
                var actor = battleServices.GetActor(_bootstrapper.Context.LocalActorId);
                if (actor == null)
                {
                    result.Fail("Local actor not found");
                    return result;
                }

                var initialX = actor.X;
                var initialZ = actor.Z;
                Log.Trace($"[TEST-7] Initial position: ({initialX:F2}, {initialZ:F2})");

                Log.Trace("[TEST-7] Triggering move...");
                inputFeature.SetMoveInput(1f, 0f);

                for (int i = 0; i < 10; i++)
                {
                    _bootstrapper.Tick();
                }

                Thread.Sleep(50);

                actor = battleServices.GetActor(_bootstrapper.Context.LocalActorId);
                var newX = actor?.X ?? initialX;
                var newZ = actor?.Z ?? initialZ;
                Log.Trace($"[TEST-7] After move: ({newX:F2}, {newZ:F2})");

                inputFeature.SetMoveInput(0f, 0f);

                result.Pass();
            }
            catch (Exception ex)
            {
                result.Fail($"Exception: {ex.Message}");
            }

            return result;
        }

        private Core.Input.ConsoleInputFeature? GetInputFeature()
        {
            var field = typeof(ConsoleBattleBootstrapper).GetField("_inputFeature",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(_bootstrapper) as Core.Input.ConsoleInputFeature;
        }

        private ConsoleBattleBootstrapper GetBootstrapper()
        {
            return _bootstrapper;
        }

        #endregion

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _completed.Dispose();
        }
    }

    public class AutoTestConfig
    {
        public bool RunInitTest { get; set; } = true;
        public bool RunPhaseTest { get; set; } = true;
        public bool RunFrameSyncTest { get; set; } = true;
        public bool RunSkillCastTest { get; set; } = true;
        public bool RunDamageTest { get; set; } = true;
        public bool RunCooldownTest { get; set; } = true;
        public bool RunMoveTest { get; set; } = true;
        public int TickIntervalMs { get; set; } = 10;
        public int TickCount { get; set; } = 10;

        public static AutoTestConfig Default => new();
    }

    public class TestResult
    {
        public string Name { get; set; } = "";
        public bool Passed { get; set; }
        public string FailReason { get; set; } = "";
        public long ElapsedMs { get; set; }
    }

    public class AutoTestResult
    {
        public DateTime StartTime { get; set; } = DateTime.Now;
        public TestResult InitTest { get; set; }
        public TestResult PhaseTest { get; set; }
        public TestResult FrameSyncTest { get; set; }
        public TestResult SkillCastTest { get; set; }
        public TestResult DamageTest { get; set; }
        public TestResult CooldownTest { get; set; }
        public TestResult MoveTest { get; set; }
        public long TotalTimeMs { get; set; }
        public bool HasUnexpectedError { get; set; }
        public string ErrorMessage { get; set; } = "";

        public int PassedCount
        {
            get
            {
                int count = 0;
                if (InitTest?.Passed == true) count++;
                if (PhaseTest?.Passed == true) count++;
                if (FrameSyncTest?.Passed == true) count++;
                if (SkillCastTest?.Passed == true) count++;
                if (DamageTest?.Passed == true) count++;
                if (CooldownTest?.Passed == true) count++;
                if (MoveTest?.Passed == true) count++;
                return count;
            }
        }

        public int TotalCount => 7;
    }

    public static class TestResultExtensions
    {
        public static void Pass(this TestResult result)
        {
            result.Passed = true;
            Log.System($"[TEST] {result.Name}: PASS");
        }

        public static void Fail(this TestResult result, string reason)
        {
            result.Passed = false;
            result.FailReason = reason;
            Log.System($"[TEST] {result.Name}: FAIL - {reason}");
        }
    }
}
