using System;
using AbilityKit.Demo.Moba.Services;
using AbilityKit.Demo.Moba.Share.Config;
using AbilityKit.GameplayTags;

namespace AbilityKit.Demo.Moba.Config.BattleDemo.MO
{
    public sealed class TagTemplateMO
    {
        public int Id { get; }
        public string Name { get; }

        public GameplayTagContainer RequiredTags { get; }
        public GameplayTagContainer BlockedTags { get; }
        public GameplayTagContainer GrantTags { get; }
        public GameplayTagContainer RemoveTags { get; }

        public TagTemplateMO(TagTemplateDTO dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            Id = dto.Id;
            Name = dto.Name;

            RequiredTags = MobaGameplayTagCatalog.ToContainer(dto.RequiredTagNames);
            BlockedTags = MobaGameplayTagCatalog.ToContainer(dto.BlockedTagNames);
            GrantTags = MobaGameplayTagCatalog.ToContainer(dto.GrantTagNames);
            RemoveTags = MobaGameplayTagCatalog.ToContainer(dto.RemoveTagNames);
        }
    }
}
