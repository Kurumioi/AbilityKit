using AbilityKit.Demo.Moba;

namespace AbilityKit.Demo.Moba.Events.Unit
{
    /// <summary>
    /// 单位死亡事件负载
    /// </summary>
    public readonly struct UnitDieEventPayload
    {
        /// <summary>单位 ActorId</summary>
        public readonly int ActorId;

        /// <summary>击杀者 ActorId</summary>
        public readonly int KillerActorId;

        /// <summary>伤害类型</summary>
        public readonly int DamageType;

        /// <summary>死亡原因类型</summary>
        public readonly int ReasonKind;

        /// <summary>死亡原因参数</summary>
        public readonly int ReasonParam;

        /// <summary>伤害值</summary>
        public readonly float DamageValue;

        public UnitDieEventPayload(int actorId, int killerActorId, int damageType, int reasonKind, int reasonParam, float damageValue)
        {
            ActorId = actorId;
            KillerActorId = killerActorId;
            DamageType = damageType;
            ReasonKind = reasonKind;
            ReasonParam = reasonParam;
            DamageValue = damageValue;
        }
    }
}
