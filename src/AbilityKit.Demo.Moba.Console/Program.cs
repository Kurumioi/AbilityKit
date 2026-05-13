using System;
using System.Threading;
using AbilityKit.Demo.Moba.Console.Battle;
using AbilityKit.Demo.Moba.Console.Platform;

namespace AbilityKit.Demo.Moba.Console
{
    internal sealed class Program
    {
        private static readonly ManualResetEvent _running = new(true);
        private static ConsoleBattleBootstrapper _bootstrapper;

        private static void Main(string[] args)
        {
            System.Console.OutputEncoding = System.Text.Encoding.UTF8;

            // ????????????????????
            Log.SetMinLevel(Log.LogLevel.System);

            Log.System("========================================");
            Log.System("   AbilityKit MOBA Console Demo");
            Log.System("========================================");

            try
            {
                _bootstrapper = new ConsoleBattleBootstrapper();
                _bootstrapper.Initialize();
                _bootstrapper.Start();
                _bootstrapper.SetupBattle();

                using var testRunner = new AutoTestRunner(_bootstrapper);
                testRunner.OnTestCompleted += OnTestCompleted;

                Log.System("");
                Log.System("Starting automated tests...");
                Log.System("");

                testRunner.Start();

                var gameThread = new Thread(GameLoop);
                gameThread.IsBackground = true;
                gameThread.Start();

                testRunner.WaitForCompletion();

                Log.System("");
                Log.System("All tests completed. Press Enter to exit...");
                System.Console.ReadLine();
            }
            catch (Exception ex)
            {
                Log.Error($"Fatal error: {ex.Message}");
                Log.Error(ex.StackTrace);
                System.Console.ReadLine();
            }
            finally
            {
                _running.Reset();
                _bootstrapper?.Stop();
                Log.System("Goodbye!");
            }
        }

        private static void OnTestCompleted(AutoTestResult results)
        {
            Log.System("");
            if (results.HasUnexpectedError)
            {
                Log.Error($"Test failed with unexpected error: {results.ErrorMessage}");
            }
            else if (results.PassedCount == results.TotalCount)
            {
                Log.System("ALL TESTS PASSED!");
            }
            else
            {
                Log.System($"SOME TESTS FAILED: {results.PassedCount}/{results.TotalCount} passed");
            }
        }

        private static void GameLoop()
        {
            while (_running.WaitOne(33))
            {
                if (_bootstrapper == null) continue;
                try
                {
                    _bootstrapper.Tick();
                }
                catch
                {
                }
            }
        }
    }
}
