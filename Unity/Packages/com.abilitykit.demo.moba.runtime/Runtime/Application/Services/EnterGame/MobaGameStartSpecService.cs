using AbilityKit.Ability.Host.Extensions.Moba.CreateWorld;
using AbilityKit.Protocol.Moba;
using AbilityKit.Ability.World.Services;
using AbilityKit.Ability.World.Services.Attributes;

namespace AbilityKit.Demo.Moba.Services
{
    public readonly struct MobaBattleStartPlanValidationResult
    {
        public static readonly MobaBattleStartPlanValidationResult Success = new MobaBattleStartPlanValidationResult(true, null);

        public readonly bool Succeeded;
        public readonly string Message;

        public MobaBattleStartPlanValidationResult(bool succeeded, string message)
        {
            Succeeded = succeeded;
            Message = message;
        }

        public static MobaBattleStartPlanValidationResult Fail(string message)
        {
            return new MobaBattleStartPlanValidationResult(false, message);
        }
    }

    [WorldService(typeof(MobaGameStartSpecService))]
    public sealed class MobaGameStartSpecService : IService
    {
        private MobaBattleStartPlan _plan;
        private MobaGameStartSpec _spec;

        public bool HasPlan { get; private set; }
        public bool HasSpec { get; private set; }

        public void SetPlan(in MobaBattleStartPlan plan)
        {
            var validation = ValidatePlan(in plan);
            if (!validation.Succeeded)
            {
                throw new System.InvalidOperationException("invalid battle start plan. " + validation.Message);
            }

            _plan = plan;
            _spec = plan.ToGameStartSpec();
            HasPlan = true;
            HasSpec = true;
        }

        public void Set(in MobaGameStartSpec spec)
        {
            SetPlan(MobaBattleStartPlan.FromEnterReq(in spec.EnterReq));
        }

        public bool TryGetPlan(out MobaBattleStartPlan plan)
        {
            plan = _plan;
            return HasPlan;
        }

        public bool TryGet(out MobaGameStartSpec spec)
        {
            spec = _spec;
            return HasSpec;
        }

        public MobaBattleStartPlanValidationResult ValidatePendingPlan()
        {
            if (!HasPlan)
            {
                return MobaBattleStartPlanValidationResult.Fail("pending battle start plan is missing.");
            }

            return ValidatePlan(in _plan);
        }

        public static MobaBattleStartPlanValidationResult ValidatePlan(in MobaBattleStartPlan plan)
        {
            var createWorldValidation = MobaProtocolValidation.ValidateCreateWorldSpecEnvelope(plan.LocalPlayerId, in plan.CreateWorldSpec);
            if (!createWorldValidation.IsValid)
            {
                return MobaBattleStartPlanValidationResult.Fail("create-world spec envelope invalid. " + createWorldValidation);
            }

            var enterReq = plan.ToEnterReq();
            var enterValidation = MobaProtocolValidation.ValidateEnterGameReqEnvelope(in enterReq);
            if (!enterValidation.IsValid)
            {
                return MobaBattleStartPlanValidationResult.Fail("enter-game request envelope invalid. " + enterValidation);
            }

            return MobaBattleStartPlanValidationResult.Success;
        }

        public void Clear()
        {
            _plan = default;
            _spec = default;
            HasPlan = false;
            HasSpec = false;
        }

        public void Dispose()
        {
            Clear();
        }
    }
}

