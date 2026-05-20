using System;
using AbilityKit.Demo.Moba.Console.Battle.Context;
using AbilityKit.Demo.Moba.Console.Platform;
using AbilityKit.Demo.Moba.Console.View;

namespace AbilityKit.Demo.Moba.Console.Battle.Game
{
    /// <summary>
    /// BattleSession 阶段 Feature
    /// 管理战斗会话生命周期
    /// </summary>
    public sealed class ConsoleBattleSessionFeature : IGamePhaseFeature
    {
        private readonly IBattleBootstrapper? _bootstrapper;
        private bool _sessionStarted;
        private bool _firstFrameReceived;

        public event Action? SessionStarted;
        public event Action? FirstFrameReceived;
        public event Action? SessionFailed;

        public ConsoleBattleSessionFeature(IBattleBootstrapper? bootstrapper = null)
        {
            _bootstrapper = bootstrapper;
        }

        public void OnAttach(in ConsoleGamePhaseContext ctx)
        {
            _sessionStarted = false;
            _firstFrameReceived = false;
            Platform.Log.System("[BattleSessionFeature] Attached");

            if (_bootstrapper != null)
            {
                try
                {
                    _bootstrapper.Initialize();
                    _bootstrapper.SetupBattle();
                    _sessionStarted = true;
                    SessionStarted?.Invoke();
                    Platform.Log.System("[BattleSessionFeature] Session started");
                }
                catch (Exception ex)
                {
                    Platform.Log.Error($"[BattleSessionFeature] Session failed: {ex.Message}");
                    SessionFailed?.Invoke();
                }
            }
        }

        public void OnDetach(in ConsoleGamePhaseContext ctx)
        {
            Platform.Log.System("[BattleSessionFeature] Detached");
            _bootstrapper?.Stop();
        }

        public void Tick(in ConsoleGamePhaseContext ctx, float deltaTime)
        {
            if (_bootstrapper != null && _sessionStarted)
            {
                _bootstrapper.Tick(deltaTime);

                if (ctx.BattleContext?.LastFrame > 0 && !_firstFrameReceived)
                {
                    _firstFrameReceived = true;
                    FirstFrameReceived?.Invoke();
                }
            }
        }
    }
}
