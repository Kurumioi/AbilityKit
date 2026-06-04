namespace AbilityKit.Demo.Moba.Gameplay
{
    public sealed class MobaTimeLimitRule : IMobaGameplayRule
    {
        private readonly float _durationSeconds;
        private readonly string _onExpiredEvent;
        private MobaGameplayService _gameplay;
        private float _elapsedSeconds;
        private bool _expired;

        public MobaTimeLimitRule(float durationSeconds, string onExpiredEvent)
        {
            _durationSeconds = durationSeconds > 0f ? durationSeconds : 0f;
            _onExpiredEvent = string.IsNullOrEmpty(onExpiredEvent) ? "gameplay.time_expired" : onExpiredEvent;
        }

        public string RuleId => "time_limit";

        public void Start(MobaGameplayService gameplay)
        {
            _gameplay = gameplay;
            _elapsedSeconds = 0f;
            _expired = _durationSeconds <= 0f;
        }

        public void Tick(float deltaTime)
        {
            if (_gameplay == null || _expired || deltaTime <= 0f)
            {
                return;
            }

            _elapsedSeconds += deltaTime;
            if (_elapsedSeconds < _durationSeconds)
            {
                return;
            }

            _expired = true;
            _gameplay.PublishGameplayEvent(_onExpiredEvent, "time_expired");
        }

        public void Stop()
        {
            _gameplay = null;
            _expired = true;
        }
    }
}
