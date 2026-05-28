using AbilityKit.Ability.Host;
using AbilityKit.Demo.Moba;

namespace AbilityKit.Demo.Moba.Events.Unit
{
    /// <summary>
    /// 单位事件负载
    /// </summary>
    public readonly struct UnitEventPayload
    {
        /// <summary>单位 ActorId</summary>
        public readonly int ActorId;

        /// <summary>队伍</summary>
        public readonly Team Team;

        /// <summary>实体主类型</summary>
        public readonly EntityMainType MainType;

        /// <summary>单位子类型</summary>
        public readonly UnitSubType UnitSubType;

        /// <summary>所属玩家</summary>
        public readonly PlayerId OwnerPlayerId;

        /// <summary>模板 ID</summary>
        public readonly int TemplateId;

        public UnitEventPayload(int actorId, Team team, EntityMainType mainType, UnitSubType unitSubType, PlayerId ownerPlayerId, int templateId)
        {
            ActorId = actorId;
            Team = team;
            MainType = mainType;
            UnitSubType = unitSubType;
            OwnerPlayerId = ownerPlayerId;
            TemplateId = templateId;
        }
    }
}
