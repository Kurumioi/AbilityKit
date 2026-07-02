using AbilityKit.Demo.Moba;

namespace AbilityKit.Demo.Moba.Services.Triggering.PlanActions
{
    public readonly struct HealArgs
    {
        public readonly float Amount;
        public readonly DamageType HealType;
        public readonly int ReasonKind;
        public readonly int ReasonParam;
        public readonly MobaActionTargetRequest TargetRequest;

        public HealArgs(float amount, DamageType healType, int reasonKind, int reasonParam, in MobaActionTargetRequest targetRequest)
        {
            Amount = amount;
            HealType = healType;
            ReasonKind = reasonKind;
            ReasonParam = reasonParam;
            TargetRequest = targetRequest;
        }
    }
}
