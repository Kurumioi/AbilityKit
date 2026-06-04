namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    public readonly struct AddGameplayVarArgs
    {
        public readonly int KeyId;
        public readonly double Delta;

        public AddGameplayVarArgs(int keyId, double delta)
        {
            KeyId = keyId;
            Delta = delta;
        }
    }
}
