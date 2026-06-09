using AbilityKit.Protocol.Moba;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;

namespace AbilityKit.Demo.Moba.Services
{
    public readonly struct MobaGameStartSpecValidationResult
    {
        public static readonly MobaGameStartSpecValidationResult Success = new MobaGameStartSpecValidationResult(true, null);

        public readonly bool Succeeded;
        public readonly string Message;

        public MobaGameStartSpecValidationResult(bool succeeded, string message)
        {
            Succeeded = succeeded;
            Message = message;
        }

        public static MobaGameStartSpecValidationResult Fail(string message)
        {
            return new MobaGameStartSpecValidationResult(false, message);
        }
    }

    [WorldService(typeof(MobaGameStartSpecService))]
    public sealed class MobaGameStartSpecService : IService
    {
        private MobaGameStartSpec _spec;

        public bool HasSpec { get; private set; }

        public void Set(in MobaGameStartSpec spec)
        {
            var validation = ValidateSpec(in spec);
            if (!validation.Succeeded)
            {
                throw new System.InvalidOperationException("invalid battle game start spec. " + validation.Message);
            }

            _spec = spec;
            HasSpec = true;
        }

        public bool TryGet(out MobaGameStartSpec spec)
        {
            spec = _spec;
            return HasSpec;
        }

        public MobaGameStartSpecValidationResult ValidatePendingSpec()
        {
            if (!HasSpec)
            {
                return MobaGameStartSpecValidationResult.Fail("pending battle game start spec is missing.");
            }

            return ValidateSpec(in _spec);
        }

        public static MobaGameStartSpecValidationResult ValidateSpec(in MobaGameStartSpec spec)
        {
            var enterReq = spec.EnterReq;
            var enterValidation = MobaProtocolValidation.ValidateEnterGameReqEnvelope(in enterReq);
            if (!enterValidation.IsValid)
            {
                return MobaGameStartSpecValidationResult.Fail("enter-game request envelope invalid. " + enterValidation);
            }

            return MobaGameStartSpecValidationResult.Success;
        }

        public void Clear()
        {
            _spec = default;
            HasSpec = false;
        }

        public void Dispose()
        {
            Clear();
        }
    }
}

