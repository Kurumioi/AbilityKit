namespace AbilityKit.Demo.Moba.Services
{
    internal sealed class BuffApplyRequestBuilder
    {
        private int _targetActorId;
        private int _buffId;
        private int _sourceActorId;
        private int _durationOverrideMs;
        private BuffOriginContext _origin;

        private BuffApplyRequestBuilder()
        {
            Reset();
        }

        public static BuffApplyRequestBuilder Create()
        {
            return new BuffApplyRequestBuilder();
        }

        public BuffApplyRequestBuilder Reset()
        {
            _targetActorId = 0;
            _buffId = 0;
            _sourceActorId = 0;
            _durationOverrideMs = 0;
            _origin = default;
            return this;
        }

        public BuffApplyRequestBuilder WithTarget(int targetActorId)
        {
            _targetActorId = targetActorId;
            return this;
        }

        public BuffApplyRequestBuilder WithBuff(int buffId)
        {
            _buffId = buffId;
            return this;
        }

        public BuffApplyRequestBuilder WithSource(int sourceActorId)
        {
            _sourceActorId = sourceActorId;
            return this;
        }

        public BuffApplyRequestBuilder WithDurationOverride(int durationOverrideMs)
        {
            _durationOverrideMs = durationOverrideMs;
            return this;
        }

        public BuffApplyRequestBuilder WithOrigin(in BuffOriginContext origin)
        {
            _origin = origin;
            return this;
        }

        public BuffApplyRequest Build()
        {
            return new BuffApplyRequest
            {
                TargetActorId = _targetActorId,
                BuffId = _buffId,
                SourceActorId = _sourceActorId,
                DurationOverrideMs = _durationOverrideMs,
                Origin = _origin,
            };
        }
    }

    internal sealed class BuffRemoveRequestBuilder
    {
        private int _targetActorId;
        private int _buffId;
        private int _sourceActorId;
        private AbilityKit.Trace.TraceLifecycleReason _reason;

        private BuffRemoveRequestBuilder()
        {
            Reset();
        }

        public static BuffRemoveRequestBuilder Create()
        {
            return new BuffRemoveRequestBuilder();
        }

        public BuffRemoveRequestBuilder Reset()
        {
            _targetActorId = 0;
            _buffId = 0;
            _sourceActorId = 0;
            _reason = AbilityKit.Trace.TraceLifecycleReason.None;
            return this;
        }

        public BuffRemoveRequestBuilder WithTarget(int targetActorId)
        {
            _targetActorId = targetActorId;
            return this;
        }

        public BuffRemoveRequestBuilder WithBuff(int buffId)
        {
            _buffId = buffId;
            return this;
        }

        public BuffRemoveRequestBuilder WithSource(int sourceActorId)
        {
            _sourceActorId = sourceActorId;
            return this;
        }

        public BuffRemoveRequestBuilder WithReason(AbilityKit.Trace.TraceLifecycleReason reason)
        {
            _reason = reason;
            return this;
        }

        public BuffRemoveRequest Build()
        {
            return new BuffRemoveRequest
            {
                TargetActorId = _targetActorId,
                BuffId = _buffId,
                SourceActorId = _sourceActorId,
                Reason = _reason,
            };
        }
    }
}
