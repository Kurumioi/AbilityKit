using System;
using System.Threading;
using AbilityKit.Demo.Moba.Console.Battle.Context;
using AbilityKit.Demo.Moba.Console.Platform;
using AbilityKit.Demo.Moba.Console.Battle.Flow;
using AbilityKit.Demo.Moba.Testing;

namespace AbilityKit.Demo.Moba.Console.AutoTest
{
    /// <summary>
    /// 自动测试运行器
    /// 使用 AutoTestInputFeature 向帧同步提供输入
    /// 不持有 EventBus，只负责管理测试流程
    /// </summary>
    public sealed class AutoTestRunner : IDisposable
    {
        private readonly ConsoleBattleBootstrapper _bootstrapper;
        private readonly AutoTestConfig _config;
        private readonly AutoTestInputFeature _autoInput;
        private readonly ModuleHost<ConsoleBattleContext, IGameModule<ConsoleBattleContext>> _moduleHost;
        private BattleTestScript? _activeScript;
        private BattleTestScriptRunResult? _lastScriptResult;
        private Thread _testThread;
        private bool _disposed;
        private Log.LogLevel _savedLogLevel;
        private AutoTestInputFeature? _previousInputFeature;

        public event Action<AutoTestResult> OnTestCompleted;

        public AutoTestRunner(ConsoleBattleBootstrapper bootstrapper) : this(bootstrapper, AutoTestConfig.Default)
        {
        }

        public AutoTestRunner(ConsoleBattleBootstrapper bootstrapper, AutoTestConfig config)
        {
            _bootstrapper = bootstrapper ?? throw new ArgumentNullException(nameof(bootstrapper));
            _config = config ?? AutoTestConfig.Default;
            _autoInput = new AutoTestInputFeature();
            _moduleHost = new ModuleHost<ConsoleBattleContext, IGameModule<ConsoleBattleContext>>();
            _moduleHost.Add(_autoInput);
        }

        /// <summary>
        /// 启用调试日志
        /// </summary>
        public void EnableDebugLogging()
        {
            Log.SetMinLevel(Log.LogLevel.Trace);
            Log.View("Debug logging enabled");
        }

        /// <summary>
        /// 禁用调试日志
        /// </summary>
        public void DisableDebugLogging()
        {
            Log.SetMinLevel(Log.LogLevel.Battle);
            Log.View("Debug logging disabled");
        }

        /// <summary>
        /// 获取自动输入特征
        /// </summary>
        public AutoTestInputFeature AutoInput => _autoInput;

        /// <summary>
        /// 运行指定的测试场景
        /// </summary>
        public void RunScenario(IBattleTestScenario scenario)
        {
            if (scenario == null) return;

            RunScript(scenario.CreateScript());
        }

        /// <summary>
        /// 运行共享平台无关测试脚本。
        /// </summary>
        public void RunScript(BattleTestScript script)
        {
            if (script == null) return;

            _activeScript = script;
            _lastScriptResult = null;
            RunAutoTest();
        }

        /// <summary>
        /// 运行完整测试
        /// </summary>
        public void RunAutoTest()
        {
            if (_testThread != null) return;

            Log.System("========================================");
            Log.System("   AUTO TEST STARTING");
            Log.System("========================================");

            _savedLogLevel = Log.MinLevel;
            Log.SetMinLevel(Log.LogLevel.Debug);

            _testThread = new Thread(TestLoop);
            _testThread.IsBackground = true;
            _testThread.Start();
        }

        public void WaitForCompletion()
        {
            _testThread?.Join();
        }

        private void TestLoop()
        {
            try
            {
                // 1. 替换输入模块
                SwitchToAutoInput();

                // 2. 运行测试
                var results = new AutoTestResult();
                RunTests(results);

                // 3. 恢复原始输入
                RestoreInput();

                // 4. 完成
                Complete(results);
            }
            catch (Exception ex)
            {
                Log.Error($"[AUTO-TEST] Unexpected error: {ex.Message}");
                RestoreInput();
                Complete(new AutoTestResult { HasUnexpectedError = true, ErrorMessage = ex.Message });
            }
            finally
            {
                Log.SetMinLevel(_savedLogLevel);
            }
        }

        private void SwitchToAutoInput()
        {
            // 记录当前步骤
            Log.Trace("[AUTO-TEST] Switching to auto input...");

            // 通过 Bootstrapper 设置自动输入特征（这样 Tick 才会被调用）
            _bootstrapper.SetAutoTestInput(_autoInput);

            Log.Trace("[AUTO-TEST] Auto input activated");
        }

        private void RestoreInput()
        {
            Log.Trace("[AUTO-TEST] Restoring original input...");

            _autoInput.Stop();

            // 通过 Bootstrapper 清除自动输入特征
            _bootstrapper.SetAutoTestInput(null);

            Log.Trace("[AUTO-TEST] Original input restored");
        }

        private void RunTests(AutoTestResult results)
        {
            // 等待进入战斗状态
            _bootstrapper.TransitionTo("InMatch");
            Thread.Sleep(50);

            // 执行共享平台无关测试脚本。步骤持续时间/逐 tick 语义由 BattleTestScriptRunner 统一维护。
            // AutoTestInputFeature 不再注册本地步骤队列，避免 FeatureHost 旧 Tick 路径和共享 runner 双重消费脚本。
            var script = _activeScript ?? BattleTestScenarioLibrary.CreateFullBattle();
            var totalSteps = script.Steps.Count;
            var driver = new ConsoleBattleTestScriptDriver(_bootstrapper, _autoInput, _config);
            _lastScriptResult = new BattleTestScriptRunner().Run(script, driver);
            results.ScriptResult = _lastScriptResult;

            if (!_lastScriptResult.Completed)
            {
                Log.Warn($"[AUTO-TEST] Script failed: {_lastScriptResult.ErrorMessage}");
            }

            // 运行固定测试
            results.InitTest = TestInitialization();
            results.PhaseTest = TestPhaseTransition();

            Log.System($"========================================");
            Log.System($"Auto test completed: {totalSteps} steps, {_lastScriptResult?.TickCount ?? 0} ticks");
            Log.System($"========================================");
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

            var passed = results.PassedCount;
            var total = results.TotalCount;
            Log.System($"Result: {passed}/{total} tests passed");
            Log.System("========================================");

            OnTestCompleted?.Invoke(results);
            _testThread = null;
        }

        #region Basic Tests

        private TestResult TestInitialization()
        {
            Log.System("[TEST] Testing Initialization...");

            var result = new TestResult { Name = "Initialization" };

            try
            {
                if (_bootstrapper == null)
                {
                    result.Fail("Bootstrapper is null");
                    return result;
                }

                if (_bootstrapper.Flow == null)
                {
                    result.Fail("Flow is null");
                    return result;
                }

                if (_bootstrapper.Context == null)
                {
                    result.Fail("Context is null");
                    return result;
                }

                if (_bootstrapper.Context.EcsWorld == null)
                {
                    result.Fail("EcsWorld is null");
                    return result;
                }

                if (_bootstrapper.BattleView == null)
                {
                    result.Fail("BattleView is null");
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

        private TestResult TestPhaseTransition()
        {
            Log.System("[TEST] Testing Phase Transition...");

            var result = new TestResult { Name = "Phase Transition" };

            try
            {
                var flow = _bootstrapper.Flow;
                var initialPhase = flow.CurrentPhase;
                Log.Trace($"[TEST] Initial phase: {initialPhase}");

                _bootstrapper.TransitionTo("InMatch");
                Thread.Sleep(50);

                var afterPhase = flow.CurrentPhase;
                Log.Trace($"[TEST] After transition: {afterPhase}");

                if (afterPhase != "InMatch")
                {
                    result.Fail($"Expected 'InMatch', got '{afterPhase}'");
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

        #endregion

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _autoInput.Stop();
            _moduleHost?.Dispose();

            Log.Trace("[AUTO-TEST] AutoTestRunner disposed");
        }
    }

    /// <summary>
    /// 自动测试配置
    /// </summary>
    public class AutoTestConfig
    {
        public bool RunInitTest { get; set; } = true;
        public bool RunPhaseTest { get; set; } = true;
        public int TickIntervalMs { get; set; } = 10;
        public int TimeoutTicks { get; set; } = 500;

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
    /// 自动测试结果
    /// </summary>
    public class AutoTestResult
    {
        public DateTime StartTime { get; set; } = DateTime.Now;
        public TestResult InitTest { get; set; }
        public TestResult PhaseTest { get; set; }
        public BattleTestScriptRunResult ScriptResult { get; set; }
        public long TotalTimeMs { get; set; }
        public bool HasUnexpectedError { get; set; }
        public string ErrorMessage { get; set; } = "";

        public int PassedCount
        {
            get
            {
                int count = 0;
                if (ScriptResult?.Completed == true) count++;
                if (InitTest?.Passed == true) count++;
                if (PhaseTest?.Passed == true) count++;
                return count;
            }
        }

        public int TotalCount => 3;
    }

    /// <summary>
    /// 测试结果扩展方法
    /// </summary>
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
