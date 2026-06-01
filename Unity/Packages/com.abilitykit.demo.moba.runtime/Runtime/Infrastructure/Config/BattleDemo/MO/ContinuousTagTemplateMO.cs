using System;
using System.Collections.Generic;
using AbilityKit.Demo.Moba.Share.Config;

namespace AbilityKit.Demo.Moba.Config.BattleDemo.MO
{
    public sealed class ContinuousTagTemplateMO
    {
        public int Id { get; }
        public string Name { get; }
        public IReadOnlyList<int> ActivationRequiredTags { get; }
        public IReadOnlyList<int> ActivationBlockedTags { get; }
        public IReadOnlyList<int> ApplicationTags { get; }
        public IReadOnlyList<int> RemovalRequiredTags { get; }
        public IReadOnlyList<int> RemovalBlockedTags { get; }
        public IReadOnlyList<int> OngoingRequiredTags { get; }
        public IReadOnlyList<int> OngoingBlockedTags { get; }
        public IReadOnlyList<int> RemovalTags { get; }

        public ContinuousTagTemplateMO(ContinuousTagTemplateDTO dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            Id = dto.Id;
            Name = dto.Name;
            ActivationRequiredTags = dto.ActivationRequiredTags ?? Array.Empty<int>();
            ActivationBlockedTags = dto.ActivationBlockedTags ?? Array.Empty<int>();
            ApplicationTags = dto.ApplicationTags ?? Array.Empty<int>();
            RemovalRequiredTags = dto.RemovalRequiredTags ?? Array.Empty<int>();
            RemovalBlockedTags = dto.RemovalBlockedTags ?? Array.Empty<int>();
            OngoingRequiredTags = dto.OngoingRequiredTags ?? Array.Empty<int>();
            OngoingBlockedTags = dto.OngoingBlockedTags ?? Array.Empty<int>();
            RemovalTags = dto.RemovalTags ?? Array.Empty<int>();
        }
    }
}
