using System;
using AbilityKit.Ability.FrameSync;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.DI;
using AbilityKit.Ability.World.Services.Attributes;
using AbilityKit.Core.Eventing;
using AbilityKit.Core.Logging;
using AbilityKit.Demo.Moba.Config.BattleDemo.MO;
using AbilityKit.Demo.Moba.Gameplay.Triggering;
using AbilityKit.Triggering.Eventing;

namespace AbilityKit.Demo.Moba.Gameplay
{
    [WorldService(typeof(MobaGameplayService), WorldLifetime.Scoped)]
    public sealed class MobaGameplayService : IService
    {
        [WorldInject(required: false)] private IFrameTime _frameTime;
        [WorldInject(required: false)] private IWorldClock _clock;
        [WorldInject(required: false)] private IEventBus _eventBus;
        [WorldInject(required: false)] private IMobaGameplayEventSink _eventSink;
        [WorldInject(required: false)] private MobaGameplayConfigService _gameplayConfigs;
        [WorldInject(required: false)] private MobaGameplayTriggerBindingService _triggerBindings;
 
        private MobaGameplayPhase _phase = MobaGameplayPhase.NotStarted;
        private float _elapsedSeconds;
        private MobaGameplayResult _lastResult;
        private int _currentGameplayId;
        private GameplayMO _currentGameplay;
 
        public MobaGameplayPhase Phase => _phase;

        public bool IsRunning => _phase == MobaGameplayPhase.Running;

        public float ElapsedSeconds => _elapsedSeconds;

        public MobaGameplayResult LastResult => _lastResult;

        public int CurrentGameplayId => _currentGameplayId;

        public GameplayMO CurrentGameplay => _currentGameplay;
 
        public void StartDefault()
        {
            if (_gameplayConfigs == null)
            {
                Log.Error("[MobaGameplayService] default gameplay start failed: config service missing");
                return;
            }

            Start(_gameplayConfigs.ResolveDefaultGameplayId());
        }
 
        public void Start()
        {
            StartDefault();
        }

        public void Start(int gameplayId)
        {
            if (_phase == MobaGameplayPhase.Running)
            {
                return;
            }

            var gameplay = ResolveGameplay(gameplayId);
            if (gameplay == null)
            {
                Log.Error($"[MobaGameplayService] gameplay start failed: missing config. gameplayId={gameplayId}");
                return;
            }

            _triggerBindings?.Bind(gameplay);
 
            _elapsedSeconds = 0f;
            _lastResult = default;
            _currentGameplayId = gameplay.Id;
            _currentGameplay = gameplay;
            _phase = MobaGameplayPhase.Running;
 
            var frame = GetCurrentFrame();
            Publish(GameplayTriggerEvents.Started, new GameplayLifecycleEventArgs(frame, 0f, 0f, null));
            _eventSink?.OnGameplayStarted(frame);
            Log.Info($"[MobaGameplayService] gameplay started. gameplayId={gameplay.Id}, frame={frame}");
        }

        public void Tick(float deltaTime)
        {
            if (_phase != MobaGameplayPhase.Running || deltaTime <= 0f)
            {
                return;
            }

            _elapsedSeconds += deltaTime;
            Publish(GameplayTriggerEvents.Tick, new GameplayLifecycleEventArgs(GetCurrentFrame(), _elapsedSeconds, deltaTime, null));
        }

        public bool End(string reason, int winTeamId = 0)
        {
            if (_phase == MobaGameplayPhase.Ended || _phase == MobaGameplayPhase.Ending)
            {
                return false;
            }

            _phase = MobaGameplayPhase.Ending;
            var result = new MobaGameplayResult(reason, winTeamId, GetCurrentFrame(), _elapsedSeconds);
            _lastResult = result;

            Publish(GameplayTriggerEvents.Ended, new GameplayLifecycleEventArgs(result.EndFrame, result.ElapsedSeconds, 0f, result.Reason, result.WinTeamId));
            _eventSink?.OnGameplayEnded(in result);

            _triggerBindings?.Unbind();
            _phase = MobaGameplayPhase.Ended;
            Log.Info($"[MobaGameplayService] gameplay ended. gameplayId={_currentGameplayId}, reason={result.Reason}, winTeamId={result.WinTeamId}, frame={result.EndFrame}, elapsed={result.ElapsedSeconds:F3}");
            return true;
        }

        public void PublishGameplayEvent(string eventName, string reason = null)
        {
            if (_phase != MobaGameplayPhase.Running)
            {
                return;
            }

            Publish(eventName, new GameplayLifecycleEventArgs(GetCurrentFrame(), _elapsedSeconds, 0f, reason));
        }

        public void Reset()
        {
            _triggerBindings?.Unbind();
            _elapsedSeconds = 0f;
            _lastResult = default;
            _currentGameplayId = 0;
            _currentGameplay = null;
            _phase = MobaGameplayPhase.NotStarted;
        }

        public void Dispose()
        {
            Reset();
        }

        private GameplayMO ResolveGameplay(int gameplayId)
        {
            if (_gameplayConfigs != null && _gameplayConfigs.TryGetGameplay(gameplayId, out var gameplay))
            {
                return gameplay;
            }

            return null;
        }

        private int GetCurrentFrame()
        {
            if (_frameTime != null) return _frameTime.Frame.Value;
            throw new InvalidOperationException("MobaGameplayService requires IFrameTime for gameplay lifecycle frames.");
        }

        private void Publish(string eventName, GameplayLifecycleEventArgs args)
        {
            if (_eventBus == null || string.IsNullOrEmpty(eventName))
            {
                return;
            }

            var key = GameplayTriggerEvents.GetKey(eventName);
            _eventBus.Publish(key, in args);
        }
    }
}
