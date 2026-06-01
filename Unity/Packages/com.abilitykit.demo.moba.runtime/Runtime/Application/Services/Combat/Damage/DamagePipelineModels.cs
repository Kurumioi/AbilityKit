using AbilityKit.Core.Common.Numbers;
using AbilityKit.Demo.Moba.Services;

namespace AbilityKit.Demo.Moba
{
    public sealed class AttackInfo : Services.IMobaActorContextProvider, Services.IMobaOriginContextProvider
    {
        public int AttackerActorId;
        public int TargetActorId;

        public object OriginSource;
        public object OriginTarget;

        public MobaTraceKind OriginKind;
        public int OriginConfigId;
        public long OriginContextId;
        public Services.MobaGameplayOrigin Origin;

        public DamageType DamageType;
        public CritType CritType;

        public DamageReasonKind ReasonKind;
        public int ReasonParam;

        public int FormulaKind;
        public string FormulaId;

        public readonly NumberValue BaseDamage;
        public readonly NumberValue DamageRate;
        public readonly NumberValue FlatBonus;
        public readonly NumberValue FinalDamage;

        public AttackInfo()
        {
            BaseDamage = new NumberValue(NumberValueMode.BaseAddMul);
            DamageRate = new NumberValue(NumberValueMode.BaseAddMul, baseValue: 1f);
            FlatBonus = new NumberValue(NumberValueMode.BaseAddMul);
            FinalDamage = new NumberValue(NumberValueMode.OverrideOnly);
        }

        public bool TryGetSourceActorId(out int actorId)
        {
            actorId = AttackerActorId;
            return actorId > 0;
        }

        public bool TryGetTargetActorId(out int actorId)
        {
            actorId = TargetActorId;
            return actorId > 0;
        }

        public bool TryGetOrigin(out Services.MobaGameplayOrigin origin)
        {
            if (Origin.IsValid)
            {
                origin = Origin;
                return true;
            }

            var sourceActorId = OriginSource is int source ? source : AttackerActorId;
            var targetActorId = OriginTarget is int target ? target : TargetActorId;
            origin = Services.MobaGameplayOrigin.FromLegacy(sourceActorId, targetActorId, OriginKind, OriginConfigId, OriginContextId);
            return origin.IsValid;
        }

        public void SetOrigin(in Services.MobaGameplayOrigin origin)
        {
            Origin = origin;
            OriginSource = origin.SourceActorId;
            OriginTarget = origin.TargetActorId;
            OriginKind = origin.ImmediateKind;
            OriginConfigId = origin.ImmediateConfigId;
            OriginContextId = origin.EffectiveParentContextId;
        }
    }

    public sealed class AttackCalcInfo : Services.IMobaActorContextProvider, Services.IMobaOriginContextProvider
    {
        public AttackInfo Attack;

        public readonly NumberValue RawDamage;
        public readonly NumberValue MitigatedDamage;
        public readonly NumberValue ShieldAbsorb;
        public readonly NumberValue HpDamage;

        public AttackCalcInfo(AttackInfo attack)
        {
            Attack = attack;
            RawDamage = new NumberValue(NumberValueMode.BaseAddMul);
            MitigatedDamage = new NumberValue(NumberValueMode.BaseAddMul);
            ShieldAbsorb = new NumberValue(NumberValueMode.BaseAddMul);
            HpDamage = new NumberValue(NumberValueMode.BaseAddMul);
        }

        public bool TryGetSourceActorId(out int actorId)
        {
            if (Attack != null) return Attack.TryGetSourceActorId(out actorId);
            actorId = 0;
            return false;
        }

        public bool TryGetTargetActorId(out int actorId)
        {
            if (Attack != null) return Attack.TryGetTargetActorId(out actorId);
            actorId = 0;
            return false;
        }

        public bool TryGetOrigin(out Services.MobaGameplayOrigin origin)
        {
            if (Attack != null) return Attack.TryGetOrigin(out origin);
            origin = default;
            return false;
        }
    }

    public sealed class DamageResult : Services.IMobaActorContextProvider, Services.IMobaOriginContextProvider
    {
        public int AttackerActorId;
        public int TargetActorId;

        public object OriginSource;
        public object OriginTarget;

        public MobaTraceKind OriginKind;
        public int OriginConfigId;
        public long OriginContextId;
        public Services.MobaGameplayOrigin Origin;

        public DamageType DamageType;
        public CritType CritType;

        public DamageReasonKind ReasonKind;
        public int ReasonParam;

        public float Value;
        public float TargetHp;
        public float TargetMaxHp;

        public bool TryGetSourceActorId(out int actorId)
        {
            actorId = AttackerActorId;
            return actorId > 0;
        }

        public bool TryGetTargetActorId(out int actorId)
        {
            actorId = TargetActorId;
            return actorId > 0;
        }

        public bool TryGetOrigin(out Services.MobaGameplayOrigin origin)
        {
            if (Origin.IsValid)
            {
                origin = Origin;
                return true;
            }

            var sourceActorId = OriginSource is int source ? source : AttackerActorId;
            var targetActorId = OriginTarget is int target ? target : TargetActorId;
            origin = Services.MobaGameplayOrigin.FromLegacy(sourceActorId, targetActorId, OriginKind, OriginConfigId, OriginContextId);
            return origin.IsValid;
        }

        public void SetOrigin(in Services.MobaGameplayOrigin origin)
        {
            Origin = origin;
            OriginSource = origin.SourceActorId;
            OriginTarget = origin.TargetActorId;
            OriginKind = origin.ImmediateKind;
            OriginConfigId = origin.ImmediateConfigId;
            OriginContextId = origin.EffectiveParentContextId;
        }
    }
}
