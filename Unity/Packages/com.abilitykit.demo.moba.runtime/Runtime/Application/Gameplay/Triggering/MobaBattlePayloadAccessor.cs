using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Events.Unit;
using AbilityKit.Triggering.Eventing;
using AbilityKit.Triggering.Payload;

namespace AbilityKit.Demo.Moba.Gameplay.Triggering
{
    public static class MobaBattlePayloadFields
    {
        public const string AttackerActorId = "attacker_actor_id";
        public const string TargetActorId = "target_actor_id";
        public const string DamageValue = "damage_value";
        public const string TargetHp = "target_hp";
        public const string TargetMaxHp = "target_max_hp";
        public const string DamageType = "damage_type";
        public const string CritType = "crit_type";
        public const string ReasonKind = "reason_kind";
        public const string ReasonParam = "reason_param";
        public const string UnitActorId = "unit_actor_id";
        public const string KillerActorId = "killer_actor_id";

        private static readonly HashSet<int> KnownFieldIds = new HashSet<int>
        {
            FieldId(AttackerActorId),
            FieldId(TargetActorId),
            FieldId(DamageValue),
            FieldId(TargetHp),
            FieldId(TargetMaxHp),
            FieldId(DamageType),
            FieldId(CritType),
            FieldId(ReasonKind),
            FieldId(ReasonParam),
            FieldId(UnitActorId),
            FieldId(KillerActorId),
        };

        public static int FieldId(string fieldName)
        {
            return StableStringId.Get("payload:" + fieldName);
        }

        public static bool IsKnownFieldId(int fieldId)
        {
            return KnownFieldIds.Contains(fieldId);
        }
    }

    public sealed class MobaBattlePayloadAccessor :
        IPayloadIntAccessor<AttackInfo>,
        IPayloadIntAccessor<DamageResult>,
        IPayloadDoubleAccessor<DamageResult>,
        IPayloadIntAccessor<UnitDieEventPayload>,
        IPayloadDoubleAccessor<UnitDieEventPayload>
    {
        private static readonly int AttackerActorIdId = MobaBattlePayloadFields.FieldId(MobaBattlePayloadFields.AttackerActorId);
        private static readonly int TargetActorIdId = MobaBattlePayloadFields.FieldId(MobaBattlePayloadFields.TargetActorId);
        private static readonly int DamageValueId = MobaBattlePayloadFields.FieldId(MobaBattlePayloadFields.DamageValue);
        private static readonly int TargetHpId = MobaBattlePayloadFields.FieldId(MobaBattlePayloadFields.TargetHp);
        private static readonly int TargetMaxHpId = MobaBattlePayloadFields.FieldId(MobaBattlePayloadFields.TargetMaxHp);
        private static readonly int DamageTypeId = MobaBattlePayloadFields.FieldId(MobaBattlePayloadFields.DamageType);
        private static readonly int CritTypeId = MobaBattlePayloadFields.FieldId(MobaBattlePayloadFields.CritType);
        private static readonly int ReasonKindId = MobaBattlePayloadFields.FieldId(MobaBattlePayloadFields.ReasonKind);
        private static readonly int ReasonParamId = MobaBattlePayloadFields.FieldId(MobaBattlePayloadFields.ReasonParam);
        private static readonly int UnitActorIdId = MobaBattlePayloadFields.FieldId(MobaBattlePayloadFields.UnitActorId);
        private static readonly int KillerActorIdId = MobaBattlePayloadFields.FieldId(MobaBattlePayloadFields.KillerActorId);

        public static bool SupportsAttackInfoField(int fieldId)
        {
            return fieldId == AttackerActorIdId
                || fieldId == TargetActorIdId
                || fieldId == DamageTypeId
                || fieldId == CritTypeId
                || fieldId == ReasonKindId
                || fieldId == ReasonParamId;
        }

        public static bool SupportsDamageResultField(int fieldId)
        {
            return SupportsAttackInfoField(fieldId)
                || fieldId == DamageValueId
                || fieldId == TargetHpId
                || fieldId == TargetMaxHpId;
        }

        public static bool SupportsUnitDieField(int fieldId)
        {
            return fieldId == UnitActorIdId
                || fieldId == TargetActorIdId
                || fieldId == KillerActorIdId
                || fieldId == AttackerActorIdId
                || fieldId == DamageTypeId
                || fieldId == ReasonKindId
                || fieldId == ReasonParamId
                || fieldId == DamageValueId;
        }

        public bool TryGet(in AttackInfo args, int fieldId, out int value)
        {
            value = 0;
            if (args == null) return false;

            if (fieldId == AttackerActorIdId)
            {
                value = args.AttackerActorId;
                return true;
            }

            if (fieldId == TargetActorIdId)
            {
                value = args.TargetActorId;
                return true;
            }

            if (fieldId == DamageTypeId)
            {
                value = (int)args.DamageType;
                return true;
            }

            if (fieldId == CritTypeId)
            {
                value = (int)args.CritType;
                return true;
            }

            if (fieldId == ReasonKindId)
            {
                value = (int)args.ReasonKind;
                return true;
            }

            if (fieldId == ReasonParamId)
            {
                value = args.ReasonParam;
                return true;
            }

            return false;
        }

        public bool TryGet(in DamageResult args, int fieldId, out int value)
        {
            value = 0;
            if (args == null) return false;

            if (fieldId == AttackerActorIdId)
            {
                value = args.AttackerActorId;
                return true;
            }

            if (fieldId == TargetActorIdId)
            {
                value = args.TargetActorId;
                return true;
            }

            if (fieldId == DamageTypeId)
            {
                value = (int)args.DamageType;
                return true;
            }

            if (fieldId == CritTypeId)
            {
                value = (int)args.CritType;
                return true;
            }

            if (fieldId == ReasonKindId)
            {
                value = (int)args.ReasonKind;
                return true;
            }

            if (fieldId == ReasonParamId)
            {
                value = args.ReasonParam;
                return true;
            }

            return false;
        }

        public bool TryGet(in DamageResult args, int fieldId, out double value)
        {
            value = 0d;
            if (args == null) return false;

            if (fieldId == DamageValueId)
            {
                value = args.Value;
                return true;
            }

            if (fieldId == TargetHpId)
            {
                value = args.TargetHp;
                return true;
            }

            if (fieldId == TargetMaxHpId)
            {
                value = args.TargetMaxHp;
                return true;
            }

            if (TryGet(in args, fieldId, out int intValue))
            {
                value = intValue;
                return true;
            }

            return false;
        }

        public bool TryGet(in UnitDieEventPayload args, int fieldId, out int value)
        {
            if (fieldId == UnitActorIdId || fieldId == TargetActorIdId)
            {
                value = args.ActorId;
                return true;
            }

            if (fieldId == KillerActorIdId || fieldId == AttackerActorIdId)
            {
                value = args.KillerActorId;
                return true;
            }

            if (fieldId == DamageTypeId)
            {
                value = args.DamageType;
                return true;
            }

            if (fieldId == ReasonKindId)
            {
                value = args.ReasonKind;
                return true;
            }

            if (fieldId == ReasonParamId)
            {
                value = args.ReasonParam;
                return true;
            }

            value = 0;
            return false;
        }

        public bool TryGet(in UnitDieEventPayload args, int fieldId, out double value)
        {
            if (fieldId == DamageValueId)
            {
                value = args.DamageValue;
                return true;
            }

            if (TryGet(in args, fieldId, out int intValue))
            {
                value = intValue;
                return true;
            }

            value = 0d;
            return false;
        }
    }
}
