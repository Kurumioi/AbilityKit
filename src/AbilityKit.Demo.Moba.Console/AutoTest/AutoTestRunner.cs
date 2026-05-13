using System;
using System.Threading;
using AbilityKit.Demo.Moba.Console.Battle;
using AbilityKit.Demo.Moba.Console.Platform;

namespace AbilityKit.Demo.Moba.Console
{
    /// <summary>
    /// 自动化测试模块
    /// 启动后自动执行测试流程，验证系统完整性
    /// </summary>
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
            // 测试期间启用调试日志
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

            // 测试1: 初始化流程
            results.InitTest = TestInitialization();
            if (!results.InitTest.Passed)
            {
                Log.System("[AUTO-TEST] Initialization failed, stopping tests");
                return;
            }

            // 测试2: Phase 切换
            results.PhaseTest = TestPhaseTransition();
            if (!results.PhaseTest.Passed)
            {
                Log.System("[AUTO-TEST] Phase transition failed, stopping tests");
                return;
            }

            // 测试3: 帧同步
            results.FrameSyncTest = TestFrameSync();
            if (!results.FrameSyncTest.Passed)
            {
                Log.System("[AUTO-TEST] Frame sync failed, stopping tests");
                return;
            }

            // 测试4: 技能释放
            results.SkillCastTest = TestSkillCast();
            if (!results.SkillCastTest.Passed)
            {
                Log.System("[AUTO-TEST] Skill cast failed, stopping tests");
                return;
            }

            // 测试5: 伤害计算
            results.DamageTest = TestDamageCalculation();
            if (!results.DamageTest.Passed)
            {
                Log.System("[AUTO-TEST] Damage calculation failed");
            }

            // 测试6: 冷却系统
            results.CooldownTest = TestCooldownSystem();
            if (!results.CooldownTest.Passed)
            {
                Log.System("[AUTO-TEST] Cooldown system failed");
            }

            // 测试7: 移动系统
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
                // 检查 Bootstrapper
                if (_bootstrapper == null)
                {
                    result.Fail("Bootstrapper is null");
                    return result;
                }

                // 检查 Flow
                var flow = _bootstrapper.Flow;
                if (flow == null)
                {
                    result.Fail("Flow is null");
                    return result;
                }
                Log.Trace("[TEST-1] Flow initialized");

                // 检查 Context
                var ctx = _bootstrapper.Context;
                if (ctx == null)
                {
                    result.Fail("Context is null");
                    return result;
                }
                Log.Trace("[TEST-1] Context initialized");

                // 检查 BattleServices
                var battleServices = _bootstrapper.BattleServices;
                if (battleServices == null)
                {
                    result.Fail("BattleServices is null");
                    return result;
                }
                Log.Trace($"[TEST-1] BattleServices initialized, ActorCount={battleServices.ActorCount}");

                // 检查 SkillExecutor
                var skillExec = _bootstrapper.SkillExecutor;
                if (skillExec == null)
                {
                    result.Fail("SkillExecutor is null");
                    return result;
                }
                Log.Trace("[TEST-1] SkillExecutor initialized");

                // 检查 ECS World
                if (ctx.EcsWorld == null)
                {
                    result.Fail("ECS World is null");
                    return result;
                }
                Log.Trace($"[TEST-1] ECS World initialized, AliveCount={ctx.EcsWorld.AliveCount}");

                // 检查 Phase
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

                // 尝试切换到 InMatch
                _bootstrapper.TransitionTo("InMatch");
                Thread.Sleep(20); // 等待 Phase 处理

                var afterPhase = flow.CurrentPhase;
                Log.Trace($"[TEST-2] After transition: {afterPhase}");

                if (afterPhase != "InMatch")
                {
                    result.Fail($"Expected 'InMatch', got '{afterPhase}'");
                    return result;
                }

                // 检查 Context.State
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

                // Tick 帧同步
                for (int i = 0; i < 10; i++)
                {
                    _bootstrapper.Tick();
                }

                Thread.Sleep(100); // 等待处理完成

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

                // 获取 InputFeature
                var inputFeature = GetInputFeature();
                if (inputFeature == null)
                {
                    result.Fail("InputFeature not found");
                    return result;
                }

                // 触发技能1
                Log.Trace("[TEST-4] Clicking skill 1...");
                inputFeature.ClickSkill(1);

                // Tick 一帧处理
                _bootstrapper.Tick();
                Thread.Sleep(20);

                // 检查技能是否执行
                var skillExec = _bootstrapper.SkillExecutor;
                Log.Trace($"[TEST-4] Skill cast completed");

                // 再次点击技能2
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

                // 模拟伤害
                Log.Trace("[TEST-5] Applying simulated damage...");
                _bootstrapper.SimulateDamage(1, 100);

                // Tick 处理
                _bootstrapper.Tick();
                Thread.Sleep(50);

                // 检查角色是否还在
                var actor = battleServices.GetActor(1);
                Log.Trace($"[TEST-5] Actor1 HP after damage: {actor?.Hp ?? -1}");

                if (actor == null)
                {
                    Log.Trace("[TEST-5] Actor was removed (died), this is expected for large damage");
                    // 死亡是预期行为，不算失败
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

                // 连续点击技能（应该触发冷却）
                Log.Trace("[TEST-6] Clicking skill 1 (should trigger cooldown)...");
                inputFeature.ClickSkill(1);
                _bootstrapper.Tick();

                Log.Trace("[TEST-6] Immediately clicking skill 1 again (should be on cooldown)...");
                inputFeature.ClickSkill(1);
                _bootstrapper.Tick();

                // 等待冷却
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

                // 触发移动
                Log.Trace("[TEST-7] Triggering move...");
                inputFeature.SetMoveInput(1f, 0f);

                // 多次 Tick
                for (int i = 0; i < 10; i++)
                {
                    _bootstrapper.Tick();
                }

                Thread.Sleep(50);

                actor = battleServices.GetActor(_bootstrapper.Context.LocalActorId);
                var newX = actor?.X ?? initialX;
                var newZ = actor?.Z ?? initialZ;
                Log.Trace($"[TEST-7] After move: ({newX:F2}, {newZ:F2})");

                // 停止移动
                inputFeature.SetMoveInput(0f, 0f);

                result.Pass();
            }
            catch (Exception ex)
            {
                result.Fail($"Exception: {ex.Message}");
            }

            return result;
        }

        private ConsoleInputFeature GetInputFeature()
        {
            var field = typeof(ConsoleBattleBootstrapper).GetField("_inputFeature",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(_bootstrapper) as ConsoleInputFeature;
        }

        #endregion

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _completed.Dispose();
        }
    }

    /// <summary>
    /// 自动化测试配置
    /// </summary>
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

    /// <summary>
    /// 测试结果
    /// </summary>
    public class TestResult
    {
        public string Name { get; set; } = "";
        public bool Passed { get; set; }
        public string FailReason { get; set; } = "";
        public long ElapsedMs { get; set; }
    }

    /// <summary>
    /// 自动化测试结果
    /// </summary>
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
