using System;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Share.Config;
using AbilityKit.GameplayTags;

namespace AbilityKit.Demo.Moba.Config.BattleDemo.MO
{
    public sealed class ContinuousTagTemplateMO
    {
        public int Id { get; }
        public string Name { get; }
        public GameplayTagContainer ActivationRequiredTags { get; }
        public GameplayTagContainer ActivationBlockedTags { get; }
        public GameplayTagContainer ApplicationTags { get; }
        public GameplayTagContainer RemovalRequiredTags { get; }
        public GameplayTagContainer RemovalBlockedTags { get; }
        public GameplayTagContainer OngoingRequiredTags { get; }
        public GameplayTagContainer OngoingBlockedTags { get; }
        public GameplayTagContainer RemovalTags { get; }

        public ContinuousTagTemplateMO(ContinuousTagTemplateDTO dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            Id = dto.Id;
            Name = dto.Name;
            ActivationRequiredTags = MobaGameplayTagCatalog.ToContainer(dto.ActivationRequiredTagNames);
            ActivationBlockedTags = MobaGameplayTagCatalog.ToContainer(dto.ActivationBlockedTagNames);
            ApplicationTags = MobaGameplayTagCatalog.ToContainer(dto.ApplicationTagNames);
            RemovalRequiredTags = MobaGameplayTagCatalog.ToContainer(dto.RemovalRequiredTagNames);
            RemovalBlockedTags = MobaGameplayTagCatalog.ToContainer(dto.RemovalBlockedTagNames);
            OngoingRequiredTags = MobaGameplayTagCatalog.ToContainer(dto.OngoingRequiredTagNames);
            OngoingBlockedTags = MobaGameplayTagCatalog.ToContainer(dto.OngoingBlockedTagNames);
            RemovalTags = MobaGameplayTagCatalog.ToContainer(dto.RemovalTagNames);
        }
    }
}
