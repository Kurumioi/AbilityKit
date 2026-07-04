using System;

namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    public readonly struct AdvanceGameplayCounterArgs
    {
        public readonly int KeyId;
        public readonly int ScopePayloadFieldId;
        public readonly double Threshold;
        public readonly double Delta;
        public readonly double ResetValue;
        public readonly int TriggerId;

        public AdvanceGameplayCounterArgs(
            int keyId,
            int scopePayloadFieldId,
            double threshold,
            double delta,
            double resetValue,
            int triggerId)
        {
            KeyId = keyId;
            ScopePayloadFieldId = scopePayloadFieldId;
            Threshold = threshold;
            Delta = delta;
            ResetValue = resetValue;
            TriggerId = triggerId;
        }

        public int ResolveScopedKey(int scopeValue)
        {
            unchecked
            {
                return (KeyId * 100000) + scopeValue;
            }
        }
    }
}
